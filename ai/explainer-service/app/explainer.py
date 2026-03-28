from typing import List

from app.models import AlertInput, ExplanationOutput


def _context_suffix(alert: AlertInput) -> str:
    parts: List[str] = []
    if alert.heart_rate is not None:
        parts.append(f"heart rate {alert.heart_rate:.0f} bpm")
    if alert.rr_interval is not None:
        parts.append(f"RR interval {alert.rr_interval:.3f} s")
    if not parts:
        return ""
    return " Context: " + ", ".join(parts) + "."


def generate_explanation(alert: AlertInput) -> ExplanationOutput:
    """
    Rule-based explanations only (no LLM). Maps MIT-BIH-style annotations to
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

    # Unknown or missing annotation: use upstream message + severity
    return ExplanationOutput(
        summary=f"Alert ({sev}): {alert.message}",
        explanation=(
            f"The monitoring system reported: {alert.message!r} with severity {sev!r}. "
            "No specific beat annotation mapping was applied."
            + ctx
        ),
        recommended_action="Continue monitoring and review repeated alerts for the same patient or record.",
    )
