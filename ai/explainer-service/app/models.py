from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class AlertInput(BaseModel):
    """Structured alert payload from the CardioFlow backend (camelCase JSON)."""

    model_config = ConfigDict(populate_by_name=True)

    patient_id: str = Field(alias="patientId")
    annotation: Optional[str] = None
    heart_rate: Optional[float] = Field(default=None, alias="heartRate")
    rr_interval: Optional[float] = Field(default=None, alias="rrInterval")
    severity: str
    message: str


class ExplanationOutput(BaseModel):
    """Human-readable explanation fields for the dashboard (camelCase JSON)."""

    model_config = ConfigDict(populate_by_name=True)

    summary: str
    explanation: str
    recommended_action: str = Field(serialization_alias="recommendedAction")
