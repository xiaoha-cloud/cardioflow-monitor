# Backend

CardioFlow backend is an ASP.NET Core service that consumes ECG telemetry from Kafka, applies alert rules, stores recent data in memory, and serves REST + SignalR for the dashboard.

## API Overview

- Base URL: `http://localhost:5050`
- REST: `/api/system/status`, `/api/ecg/*`, `/api/alerts`, `/api/patients/current`
- SignalR hub: `/hubs/telemetry`
- Topic: `ecg.telemetry`

## Quick Start (Backend First)

1) Start Kafka:

```bash
cd scripts/kafka
docker compose up -d
cd ../..
scripts/kafka/ensure-topics.sh
```

2) Start backend API:

```bash
cd backend/CardioFlow.Api
dotnet restore
dotnet run --urls http://localhost:5050
```

3) Start simulator (new terminal):

```bash
cd simulator/mitbih-replay
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
python replay.py --record 100
```

4) Start frontend dashboard (optional, new terminal):

```bash
cd frontend/dashboard
npm install
npm run dev
```

Dependency order: **Kafka -> backend -> simulator -> frontend**.

## REST Endpoints

### `GET /api/system/status`

Returns a system health snapshot for stream readiness and runtime state.

| Query | Type | Default | Range |
|---|---|---|---|
| none | - | - | - |

Example request:

```bash
curl -sS "http://localhost:5050/api/system/status"
```

Response (with data):

```json
{
  "streamHealth": "healthy",
  "streamStatus": "running",
  "lastMessageTimestamp": "2026-03-21T10:12:14.123Z",
  "currentRecord": "100",
  "telemetryCount": 1000,
  "alertCount": 42,
  "samplingRate": 360,
  "topic": "ecg.telemetry",
  "activePatient": "mitdb-100",
  "lastAlert": "Short RR interval detected",
  "uptimeSeconds": 356,
  "consumerState": "connected"
}
```

Response (empty):

```json
{
  "streamHealth": "down",
  "streamStatus": "stopped",
  "lastMessageTimestamp": null,
  "currentRecord": null,
  "telemetryCount": 0,
  "alertCount": 0,
  "samplingRate": 360,
  "topic": "ecg.telemetry",
  "activePatient": null,
  "lastAlert": null,
  "uptimeSeconds": 18,
  "consumerState": "reconnecting"
}
```

Error (400):

```json
{
  "error": "Invalid request"
}
```

### `GET /api/ecg/latest`

Returns latest telemetry array ordered by `sampleIndex` asc.

| Query | Type | Default | Range |
|---|---|---|---|
| `count` | int | 500 | 1-1000 |
| `recordId` | string | current record | `100`,`101`,`103` |

Example request:

```bash
curl -sS "http://localhost:5050/api/ecg/latest?recordId=100&count=5"
```

Response (with data):

```json
[
  {
    "patientId": "mitdb-100",
    "recordId": "100",
    "deviceId": "ecg-sim-01",
    "timestamp": "2026-03-21T10:12:14.123Z",
    "sampleIndex": 314748,
    "lead1": -0.42,
    "annotation": "N",
    "heartRate": 78,
    "status": "normal",
    "rrIntervalMs": 820.1
  }
]
```

Response (empty):

```json
[]
```

Error (400):

```json
{
  "error": "recordId must be one of: 100, 101, 103"
}
```

### `GET /api/alerts`

Returns timestamp-desc alerts with normalized severity and source rule.

| Query | Type | Default | Range |
|---|---|---|---|
| `count` | int | 20 | 1-100 |
| `recordId` | string | current record | `100`,`101`,`103` |

Example request:

```bash
curl -sS "http://localhost:5050/api/alerts?recordId=100&count=5"
```

Response (with data):

```json
[
  {
    "patientId": "mitdb-100",
    "recordId": "100",
    "deviceId": "ecg-sim-01",
    "timestamp": "2026-03-21T10:12:14.123Z",
    "sampleIndex": 314748,
    "annotation": "V",
    "heartRate": 132,
    "rrIntervalMs": 380.5,
    "severity": "critical",
    "message": "Tachycardia threshold exceeded",
    "sourceRule": "hr_threshold_rule",
    "metadata": {
      "matchedRules": "annotation_rule,hr_threshold_rule,rr_interval_rule",
      "matchedCount": "3"
    }
  }
]
```

Response (empty):

```json
[]
```

Error (400):

```json
{
  "error": "recordId must be one of: 100, 101, 103"
}
```

### `GET /api/patients/current`

Returns current patient/device snapshot inferred from latest telemetry.

| Query | Type | Default | Range |
|---|---|---|---|
| none | - | - | - |

Example request:

```bash
curl -sS "http://localhost:5050/api/patients/current"
```

Response (with data):

```json
{
  "patientId": "mitdb-100",
  "recordId": "100",
  "deviceId": "ecg-sim-01",
  "battery": 82,
  "signalQuality": "good",
  "streamStatus": "running"
}
```

Response (empty-ish):

```json
{
  "patientId": "-",
  "recordId": "-",
  "deviceId": "-",
  "battery": null,
  "signalQuality": "unknown",
  "streamStatus": "stopped"
}
```

### `GET /api/ecg/events`

Returns event-log-friendly telemetry (latest first).

| Query | Type | Default | Range |
|---|---|---|---|
| `count` | int | 30 | 1-200 |
| `recordId` | string | current record | `100`,`101`,`103` |

Example request:

```bash
curl -sS "http://localhost:5050/api/ecg/events?recordId=100&count=5"
```

## SignalR Events

Hub: `ws://localhost:5050/hubs/telemetry`

### `ReceiveTelemetry`

```json
{
  "timestamp": "2026-03-21T10:12:14.123Z",
  "sampleIndex": 314748,
  "recordId": "100",
  "lead1": -0.42,
  "annotation": "N",
  "rrIntervalMs": 820.1
}
```

### `ReceiveAlert`

```json
{
  "timestamp": "2026-03-21T10:12:14.123Z",
  "sampleIndex": 314748,
  "severity": "critical",
  "message": "Tachycardia threshold exceeded",
  "sourceRule": "hr_threshold_rule",
  "rrIntervalMs": 380.5
}
```

### `ReceiveSystemStatus`

```json
{
  "streamHealth": "healthy",
  "streamStatus": "running",
  "currentRecord": "100",
  "telemetryCount": 1000,
  "alertCount": 42,
  "lastMessageTimestamp": "2026-03-21T10:12:14.123Z"
}
```

## Alert Rules

### Rule Matrix

| Rule | Trigger | Severity | Message | SourceRule |
|---|---|---|---|---|
| Annotation | `annotation=V` | warning | PVC detected | `annotation_rule` |
| Annotation | `annotation=A` | warning | Atrial premature beat detected | `annotation_rule` |
| Annotation | `annotation=E/F/Q` | warning | specific annotation message | `annotation_rule` |
| HR threshold | `heartRate > HrHighThreshold` | critical | Tachycardia threshold exceeded | `hr_threshold_rule` |
| HR threshold | `heartRate < HrLowThreshold` | critical | Bradycardia threshold exceeded | `hr_threshold_rule` |
| RR threshold | `rrIntervalMs < RrLowMs` | warning | Short RR interval detected | `rr_interval_rule` |
| RR threshold | `rrIntervalMs > RrHighMs` | warning | Long RR interval detected | `rr_interval_rule` |

### Severity Policy

- Unified severity values: `normal`, `warning`, `critical`
- Priority: `critical > warning > normal`
- Merge strategy: one telemetry emits **one final alert** (highest severity winner)
- `metadata.matchedRules` records all hit rules for transparency

### Detection Threshold Config

In `CardioFlow.Api/appsettings.json`:

```json
"DetectionRules": {
  "HrHighThreshold": 120,
  "HrLowThreshold": 45,
  "RrLowMs": 400,
  "RrHighMs": 1200,
  "EnableRrRule": true
}
```

Threshold meanings:

- `HrHighThreshold`: tachycardia threshold in bpm
- `HrLowThreshold`: bradycardia threshold in bpm
- `RrLowMs`: short RR threshold in milliseconds
- `RrHighMs`: long RR threshold in milliseconds
- `EnableRrRule`: enable/disable RR rule evaluation

## Data Contracts

### `AlertMessage` (normalized)

- Required: `patientId`, `recordId`, `deviceId`, `timestamp`, `sampleIndex`, `severity`, `message`, `sourceRule`
- Optional: `annotation`, `heartRate`, `rrIntervalMs`, `metadata`, `receivedAt`, `explanationSummary`, `explanationDetails`, `recommendedAction` (filled when `Explainer:BaseUrl` is set and the explainer service succeeds)

### `SystemStatusDto` (core)

- Health: `streamHealth`, `consumerState`
- Status: `streamStatus`, `currentRecord`, `activePatient`
- Counts: `telemetryCount`, `alertCount`
- Timing: `lastMessageTimestamp`, `uptimeSeconds`

## Troubleshooting

- **Port conflict (`5050`)**
  - `lsof -i :5050`
- **Port conflict (`5000`)**
  - `lsof -i :5000`
- **Kafka not reachable**
  - verify Docker Kafka containers and topic `ecg.telemetry`
  - check `Kafka:BootstrapServers`
- **Alerts without explanation fields**
  - set `Explainer:BaseUrl` (e.g. `http://localhost:8000`) and run `ai/explainer-service`
  - if `appsettings.Development.json` is gitignored locally, copy `appsettings.Development.sample.json` to `appsettings.Development.json` or merge the `Explainer` section
  - if the explainer is down, alerts are still stored; explanation fields stay null
- **No data / empty arrays**
  - replay may not be publishing
  - consumer may be reconnecting
- **SignalR connected but no events**
  - verify backend running latest build and hub route `/hubs/telemetry`

## Verification Commands

```bash
curl -sS "http://localhost:5050/api/health"; echo
curl -sS "http://localhost:5050/api/system/status"; echo
curl -sS "http://localhost:5050/api/ecg/latest?count=5"; echo
curl -sS "http://localhost:5050/api/alerts?count=5"; echo
```
