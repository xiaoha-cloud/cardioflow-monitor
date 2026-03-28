# CardioFlow Explainer Service (Design)

This document defines the **AI explanation layer** for CardioFlow Monitor. It is a contract-first specification: implementation (FastAPI, LangChain, Kubernetes) follows later phases.

## Purpose

The explainer service turns **structured anomaly alerts** from the backend into **short, human-readable text** for the dashboard. It does **not** replace clinical judgment or the rule-based detector.

## Responsibilities

The service **shall**:

1. **Alert summary** — One or two sentences describing what the alert context suggests in plain English.
2. **Short explanation** — Brief elaboration tying together annotation (if any), severity, and the alert message from upstream rules.
3. **Recommended monitoring action** — Non-clinical follow-up guidance (for example, continued observation, review of repeated events). Must not prescribe treatment or emergency instructions.

The service **shall not**:

- Assert a definitive medical diagnosis.
- Provide drug dosing, procedures, or emergency triage instructions.
- Interpret raw ECG waveforms or replace MIT-BIH annotation semantics with independent beat labeling (that belongs to a separate ML pipeline, if added later).

## Placement in the architecture

Target data flow (post-integration):

```text
MIT-BIH simulator -> Kafka -> ASP.NET Core backend (rules + AlertMessage)
  -> HTTP POST explainer `/explain` -> explanation fields -> SignalR / REST -> React dashboard
```

The backend remains the **source of truth** for when an alert exists and what `severity` / `message` / `sourceRule` are. The explainer only **enriches** the alert for display.

## API surface (planned)

- `GET /health` — Liveness for orchestration and load balancers.
- `POST /explain` — Request body below; response body below.

Authentication between backend and explainer is **out of scope** for the minimal MVP; restrict via network policy or private URL in production.

## Request body: `POST /explain`

JSON object. All string fields use UTF-8.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `patientId` | string | yes | Patient identifier (e.g. MIT-BIH style id). |
| `annotation` | string or null | no | Beat annotation symbol from telemetry, if present (e.g. `V`, `A`, `N`). |
| `heartRate` | number or null | no | Heart rate in bpm when the alert was raised. |
| `rrInterval` | number or null | no | RR interval in **seconds** (e.g. `0.42`). Omit or null if unknown. |
| `severity` | string | yes | One of: `normal`, `warning`, `critical` (aligned with backend). |
| `message` | string | yes | Human-readable alert text from the rule engine (e.g. `PVC detected`). |

### Example request

```json
{
  "patientId": "mitdb-100",
  "annotation": "V",
  "heartRate": 112,
  "rrInterval": 0.42,
  "severity": "warning",
  "message": "PVC detected"
}
```

### Adapter note (backend integration)

The ASP.NET Core `AlertMessage` model uses `rrIntervalMs` (milliseconds). The backend **adapter** that calls this service must convert: `rrInterval = rrIntervalMs / 1000.0` when present, and must document units in logs if debugging.

## Response body: `POST /explain`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `summary` | string | yes | Short dashboard-friendly line. |
| `explanation` | string | yes | Slightly longer clinical-style wording; still non-diagnostic. |
| `recommendedAction` | string | yes | Monitoring-oriented next step only. |

### Example response

```json
{
  "summary": "Possible premature ventricular contraction detected.",
  "explanation": "The alert suggests a PVC-related abnormal beat with elevated heart rate.",
  "recommendedAction": "Continue monitoring and review repeated abnormal events."
}
```

## Implementation phases (reference)

1. **Rule-based fallback** — Deterministic templates from `annotation` / `severity` / `message` so the stack runs without an LLM API key.
2. **LLM path** — Optional: LangChain or OpenAI SDK behind the same `/explain` contract; timeouts and graceful degradation on failure.
3. **Container + Kubernetes** — Dockerfile for this service; Deployment + Service manifests (see repository `k8s/` in a later change set).

## Acceptance criteria (design phase)

- Stakeholders can state clearly: **what** the service accepts, **what** it returns, and **what** it refuses to do (no diagnosis, no treatment orders).
- Backend and frontend teams can implement against this JSON contract without ambiguity on field names and RR **seconds** vs **milliseconds** at the explainer boundary.
