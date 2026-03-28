from fastapi import FastAPI

from app.explainer import generate_explanation
from app.models import AlertInput, ExplanationOutput

app = FastAPI(
    title="CardioFlow Explainer Service",
    description="Rule-based alert explanations for the CardioFlow monitoring dashboard.",
    version="0.1.0",
)


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/explain", response_model=ExplanationOutput, response_model_by_alias=True)
def explain(alert: AlertInput) -> ExplanationOutput:
    return generate_explanation(alert)
