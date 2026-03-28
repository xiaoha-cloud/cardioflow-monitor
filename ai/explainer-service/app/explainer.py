import logging
import os
from typing import List, Optional

from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from pydantic import BaseModel, Field

from app.models import AlertInput, ExplanationOutput

logger = logging.getLogger(__name__)

SYSTEM_PROMPT = """You assist a real-time ECG monitoring dashboard.
Given a structured cardiac alert, produce text for clinicians and operators.

Rules:
- Do not state a definitive medical diagnosis.
- Do not give emergency instructions, treatments, or drug dosing.
- Keep language concise and suitable for a live monitoring UI.
- Fill every field in the structured response with substantive text."""

USER_TEMPLATE = """Structured alert:
- patientId: {patient_id}
- annotation (MIT-BIH style beat label, if any): {annotation}
- heartRate (bpm, if known): {heart_rate}
- rrInterval (seconds, if known): {rr_interval}
- severity: {severity}
- upstream rule message: {message}

Produce: a short summary, a slightly longer explanation, and a monitoring-only recommended action."""


class LlmExplanationPayload(BaseModel):
    """Schema for LangChain / OpenAI structured output (matches dashboard JSON contract)."""

    summary: str = Field(description="One or two sentences for the dashboard headline.")
    explanation: str = Field(
        description="Brief clinical-style context; explicitly non-diagnostic."
    )
    recommendedAction: str = Field(
        description="Monitoring-oriented next step only (e.g. continued observation)."
    )


def _format_optional_float(value: Optional[float]) -> str:
    if value is None:
        return "unknown"
    return f"{value:.4g}"


def _annotation_for_prompt(alert: AlertInput) -> str:
    raw = (alert.annotation or "").strip()
    return raw if raw else "none"


def _invoke_llm(alert: AlertInput) -> ExplanationOutput:
    api_key = os.environ["OPENAI_API_KEY"].strip()
    model_name = (os.environ.get("OPENAI_MODEL") or "gpt-4o-mini").strip() or "gpt-4o-mini"

    llm = ChatOpenAI(
        model=model_name,
        temperature=0.2,
        api_key=api_key,
        timeout=30,
    )
    structured_llm = llm.with_structured_output(LlmExplanationPayload)

    prompt = ChatPromptTemplate.from_messages(
        [
            ("system", SYSTEM_PROMPT),
            ("human", USER_TEMPLATE),
        ]
    )
    chain = prompt | structured_llm
    payload = chain.invoke(
        {
            "patient_id": alert.patient_id,
            "annotation": _annotation_for_prompt(alert),
            "heart_rate": str(int(round(alert.heart_rate))) if alert.heart_rate is not None else "unknown",
            "rr_interval": _format_optional_float(alert.rr_interval),
            "severity": alert.severity,
            "message": alert.message,
        }
    )
    return ExplanationOutput(
        summary=payload.summary.strip(),
        explanation=payload.explanation.strip(),
        recommended_action=payload.recommendedAction.strip(),
    )


def _context_suffix(alert: AlertInput) -> str:
    parts: List[str] = []
    if alert.heart_rate is not None:
        parts.append(f"heart rate {alert.heart_rate:.0f} bpm")
    if alert.rr_interval is not None:
        parts.append(f"RR interval {alert.rr_interval:.3f} s")
    if not parts:
        return ""
    return " Context: " + ", ".join(parts) + "."


def generate_rule_based_explanation(alert: AlertInput) -> ExplanationOutput:
    """
    Deterministic explanations (no LLM). Maps MIT-BIH-style annotations to
    non-diagnostic monitoring language.
    """
    raw = (alert.annotation or "").strip()
    ann = raw.upper() if raw else None
    ctx = _context_suffix(alert)
    sev = alert.severity.lower()

    if ann == "V":
        return ExplanationOutput(
            summary="Possible premature ventricular contraction (PVC) pattern suggested by the beat label.",
            explanation=(
                "The telemetry annotation 'V' often corresponds to a ventricular ectopic beat in MIT-BIH style records. "
                "This is contextual monitoring information, not a confirmed diagnosis."
                + ctx
            ),
            recommended_action=(
                "Continue monitoring; review repeated ventricular-labeled events and correlate with clinical context."
            ),
        )

    if ann == "A":
        return ExplanationOutput(
            summary="Possible atrial premature beat pattern suggested by the beat label.",
            explanation=(
                "The telemetry annotation 'A' often corresponds to an atrial premature beat in MIT-BIH style records. "
                "This is contextual monitoring information, not a confirmed diagnosis."
                + ctx
            ),
            recommended_action=(
                "Continue monitoring; review repeated atrial-labeled events if they cluster."
            ),
        )

    if ann == "N":
        return ExplanationOutput(
            summary="Beat labeled as normal sinus context for this sample.",
            explanation=(
                "The annotation 'N' typically indicates a normal beat label in MIT-BIH style data. "
                "If an alert still fired, it may be driven by rate, rhythm, or another rule."
                + ctx
            ),
            recommended_action=(
                "Continue routine monitoring; verify which rule produced the alert if severity seems unexpected."
            ),
        )

    return ExplanationOutput(
        summary=f"Alert ({sev}): {alert.message}",
        explanation=(
            f"The monitoring system reported: {alert.message!r} with severity {sev!r}. "
            "No specific beat annotation mapping was applied."
            + ctx
        ),
        recommended_action="Continue monitoring and review repeated alerts for the same patient or record.",
    )


def generate_explanation(alert: AlertInput) -> ExplanationOutput:
    """
    Uses OpenAI via LangChain when OPENAI_API_KEY is set; otherwise rule-based fallback.
    On LLM errors, falls back to rule-based output so the service stays available.
    """
    if os.environ.get("OPENAI_API_KEY", "").strip():
        try:
            return _invoke_llm(alert)
        except Exception as exc:
            logger.warning(
                "LLM explanation failed, using rule-based fallback: %s",
                exc,
                exc_info=True,
            )
    return generate_rule_based_explanation(alert)
