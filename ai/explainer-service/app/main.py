import logging

from fastapi import FastAPI

from app.explainer import generate_explanation
from app.models import AlertInput, ExplanationOutput

logging.basicConfig(level=logging.INFO)

app = FastAPI(
    title="CardioFlow Explainer Service",
    description=(
        "Alert explanations for the CardioFlow dashboard: OpenAI (LangChain) when "
        "OPENAI_API_KEY is set; otherwise rule-based fallback."
    ),
    version="0.2.0",
)


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/explain", response_model=ExplanationOutput, response_model_by_alias=True)
def explain(alert: AlertInput) -> ExplanationOutput:
    return generate_explanation(alert)
