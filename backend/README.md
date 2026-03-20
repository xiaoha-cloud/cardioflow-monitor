# Backend

CardioFlow backend is an ASP.NET Core service responsible for telemetry ingestion, in-memory buffering, anomaly detection, alerting, and real-time/dashboard APIs.

## Backend Architecture Responsibilities

- **Kafka consumer**: `KafkaConsumerService` continuously consumes telemetry from `ecg.telemetry`.
- **Telemetry buffer**: `TelemetryBufferService` stores recent telemetry in a thread-safe FIFO buffer.
- **Anomaly detection**: `AnomalyDetectionService` maps annotation + HR thresholds into alert severity and message.
- **Alert buffer**: `AlertService` keeps recent alerts (deduped, bounded queue).
- **REST APIs**: serve initialization and filtered queries for ECG, alerts, patient snapshot, and system status.
- **SignalR**: pushes `ReceiveTelemetry`, `ReceiveAlert`, `ReceiveSystemStatus` to dashboard clients.

## Run Locally (Backend)

```bash
cd backend/CardioFlow.Api
dotnet restore
dotnet run --urls http://localhost:5050
```

### Common Issues

- **Port already in use**
  - macOS: `lsof -i :5050`
  - stop existing process or change `--urls`.
- **Kafka connection failure**
  - ensure Kafka is running and `Kafka:BootstrapServers` matches local setup.
  - check topic name: `Kafka:TelemetryTopic` (default `ecg.telemetry`).
- **No telemetry in APIs**
  - simulator/replay may not be publishing yet, or consumer may be disconnected.

## API Examples

### System status

`GET /api/system/status`

```json
{
  "streamStatus": "running",
  "samplingRate": 360,
  "topic": "ecg.telemetry",
  "activePatient": "mitdb-101",
  "activeRecord": "101",
  "activeRecordId": "101",
  "deviceId": "ecg-sim-01",
  "lastAlert": "PVC detected",
  "bufferCount": 742,
  "lastMessageAt": "2026-03-20T20:00:10.345Z"
}
```

### ECG latest with record filter

`GET /api/ecg/latest?recordId=100&count=500`

### Event log endpoint (latest first)

`GET /api/ecg/events?recordId=103&count=30`

### Alerts with record filter

`GET /api/alerts?recordId=101&count=20`

## Multi-Record Usage (100/101/103)

- Supported record filters: `100`, `101`, `103`.
- Invalid record filter returns `400` with a clear message.
- Endpoints supporting record filtering:
  - `/api/ecg/latest`
  - `/api/ecg/window`
  - `/api/ecg/events`
  - `/api/alerts`
- If `recordId` is omitted, APIs default to the latest active record in buffer.

## Architecture Diagram

Mermaid draft is available in `docs/architecture/week2-backend-flow.md`.
