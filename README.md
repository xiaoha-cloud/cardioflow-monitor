# CardioFlow Monitor

CardioFlow Monitor is a real-time ECG monitoring demo stack:

- **Simulator** replays MIT-BIH records (`100`, `101`, `103`) to Kafka
- **Backend** consumes telemetry, performs anomaly detection, and exposes REST + SignalR
- **Frontend** renders ECG chart, alerts, patient/device info, and event log

## Dashboard Screenshot

![CardioFlow Dashboard Overview](docs/screenshots/dashboard-overview.png)


## Architecture

```mermaid
flowchart LR
  sim[MIT-BIH Replay Simulator] --> k[(Kafka ecg.telemetry)]
  k --> c[ASP.NET Core KafkaConsumerService]
  c --> b[TelemetryBufferService]
  c --> a[AlertService]
  c --> d[AnomalyDetectionService]
  b --> rest1[/GET /api/ecg/latest<br/>GET /api/ecg/events/]
  a --> rest2[/GET /api/alerts/]
  b --> rest3[/GET /api/system/status/]
  c --> hub[SignalR /hubs/telemetry]
  rest1 --> fe[React Dashboard]
  rest2 --> fe
  rest3 --> fe
  hub --> fe
```

## Repository Structure

| Path | Purpose |
|------|---------|
| `backend/CardioFlow.Api/` | ASP.NET Core API, Kafka consumer, REST, SignalR |
| `frontend/dashboard/` | React + TypeScript + Vite dashboard |
| `simulator/mitbih-replay/` | Python MIT-BIH replay producer |
| `scripts/kafka/` | Docker Compose + topic bootstrap scripts |
| `docs/` | Architecture notes and screenshots |

## Prerequisites

- Docker + Docker Compose
- .NET SDK
- Node.js + npm
- Python 3.8+

## Quick Start

### 1) Start Kafka and create topics

```bash
cd scripts/kafka
docker compose up -d
cd ../..
scripts/kafka/ensure-topics.sh
```

### 2) Start backend

```bash
cd backend/CardioFlow.Api
dotnet restore
dotnet run --urls http://localhost:5050
```

### 3) Start simulator replay

```bash
cd simulator/mitbih-replay
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
python replay.py --record 100
```

To test other records:

```bash
python replay.py --record 101
python replay.py --record 103
```

### 4) Start frontend

```bash
cd frontend/dashboard
npm install
npm run dev
```

Frontend default URL: [http://localhost:5173](http://localhost:5173)

## Frontend Environment Variables

- `VITE_API_BASE_URL` (default: `http://localhost:5050`)
- `VITE_SIGNALR_HUB_URL` (default: `http://localhost:5050/hubs/telemetry`)

## Backend API Overview

### System status

- `GET /api/system/status`
- Includes: `streamStatus`, `activePatient`, `activeRecordId`, `bufferCount`, `lastMessageAt`

### ECG data

- `GET /api/ecg/latest?count=500&recordId=100`
- `GET /api/ecg/window?count=800&windowSeconds=5&recordId=101&downsample=2`
- `GET /api/ecg/events?count=30&recordId=103` (latest first, event-log friendly)

### Alerts

- `GET /api/alerts?count=20&recordId=100`
- Returns timestamp-desc alerts with `severity`, `message`, and `heartRate`

### Patient snapshot

- `GET /api/patients/current`

## Record Switching Behavior

- Supported records: `100`, `101`, `103`
- Dashboard record selector reloads record-scoped REST data
- SignalR updates are filtered on the client by selected `recordId`
- Invalid `recordId` on backend APIs returns `400`

## Health Checks

Use these commands to verify data flow:

```bash
curl -sS "http://localhost:5050/api/system/status"; echo
curl -sS "http://localhost:5050/api/ecg/latest?recordId=100&count=5"; echo
curl -sS "http://localhost:5050/api/ecg/events?recordId=100&count=5"; echo
curl -sS "http://localhost:5050/api/alerts?recordId=100&count=5"; echo
```

If all arrays are empty and `streamStatus=stopped`, simulator is likely not publishing or Kafka/consumer is disconnected.



## Troubleshooting

- **Port 5050 already used**: `lsof -i :5050`
- **No telemetry received**:
  - verify simulator terminal is actively sending samples
  - verify Kafka topic exists (`ecg.telemetry`)
  - verify backend logs show consumer activity
- **Frontend connected but no updates**:
  - verify `VITE_SIGNALR_HUB_URL`
  - check browser console for SignalR reconnect/disconnect logs
