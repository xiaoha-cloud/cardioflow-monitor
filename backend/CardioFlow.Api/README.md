# CardioFlow API

ASP.NET Core Web API for consuming ECG telemetry data from Kafka and providing REST endpoints for accessing the data.

## Live Demo

⚙️ **[https://cardioflow-monitor-1.onrender.com](https://cardioflow-monitor-1.onrender.com)**

Quick health check:
```bash
curl https://cardioflow-monitor-1.onrender.com/api/system/status
```

> Deployed via Docker on Render's free tier. May take ~50 seconds to wake up after inactivity.

## Overview

This API service:
- Consumes ECG telemetry messages from Kafka topic `ecg.telemetry`
- Stores messages in an in-memory buffer (max 1000 messages)
- Provides REST endpoints to query telemetry data
- Runs as a background service for continuous message consumption

## Prerequisites

- .NET 10.0 SDK or later
- Kafka running locally (see `../../scripts/kafka/` for setup)
- Kafka topic `ecg.telemetry` created

## Configuration

### appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroupId": "cardioflow-api-consumer",
    "TelemetryTopic": "ecg.telemetry"
  },
  "TelemetryBuffer": {
    "MaxBufferSize": 1000
  }
}
```

### Environment Variables

You can override Kafka configuration using environment variables:

```bash
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
```

## Running the API

1. **Ensure Kafka is running:**
   ```bash
   # From project root
   scripts/kafka/ensure-topics.sh
   ```

2. **Restore dependencies:**
   ```bash
   cd backend/CardioFlow.Api
   dotnet restore
   ```

3. **Run the API:**
   ```bash
   dotnet run
   ```

   Or for development:
   ```bash
   dotnet watch run
   ```

4. **The API will be available at:**
   - HTTP: http://localhost:5050
   - HTTPS: https://localhost:5001
   - Swagger UI: http://localhost:5050/swagger

## Frontend Integration Contract

This section defines the frozen REST contract for frontend integration during Day6.

### GET `/api/system/status`

**Success response**
```json
{
  "streamStatus": "running",
  "samplingRate": 360,
  "topic": "ecg.telemetry",
  "activePatient": "mitdb-100",
  "lastAlert": "PVC detected",
  "bufferCount": 1000,
  "lastMessageAt": "2026-03-20T18:55:10.1234567Z"
}
```

**Empty-data response**
```json
{
  "streamStatus": "stopped",
  "samplingRate": 360,
  "topic": "ecg.telemetry",
  "activePatient": null,
  "lastAlert": null,
  "bufferCount": 0,
  "lastMessageAt": null
}
```

### GET `/api/ecg/latest?count=500`

- Default `count=500`
- Valid range `1..1000`
- Returns messages sorted by `sampleIndex` ascending

**Success response**
```json
[
  {
    "patientId": "mitdb-100",
    "recordId": "100",
    "deviceId": "ecg-sim-01",
    "timestamp": "2026-03-20T18:55:10.1234567Z",
    "sampleIndex": 12340,
    "lead1": -0.245,
    "annotation": "N",
    "heartRate": 74,
    "status": "normal",
    "signalQuality": "good",
    "battery": 87
  }
]
```

**Empty-data response**
```json
[]
```

### GET `/api/alerts?count=20`

- Default `count=20`
- Valid range `1..100`
- Returns latest alerts first (`timestamp` descending)

**Success response**
```json
[
  {
    "patientId": "mitdb-100",
    "deviceId": "ecg-sim-01",
    "timestamp": "2026-03-20T18:55:08.2345678Z",
    "sampleIndex": 12220,
    "annotation": "V",
    "severity": "warning",
    "message": "PVC detected",
    "heartRate": 78
  }
]
```

**Empty-data response**
```json
[]
```

### GET `/health`

**Success response**
```json
{
  "status": "ok",
  "kafkaConfigured": true,
  "bufferCount": 1000,
  "alertsCount": 35
}
```

### Error Codes

- `400 Bad Request`: query validation failure (for invalid `count` range)
- `500 Internal Server Error`: unexpected server-side failure

## Architecture

### Components

1. **KafkaConsumerService** (`BackgroundServices/KafkaConsumerService.cs`)
   - Background service that continuously consumes messages from Kafka
   - Handles connection errors and reconnection logic
   - Deserializes JSON messages to `TelemetryMessage` objects

2. **TelemetryBufferService** (`Services/TelemetryBufferService.cs`)
   - Thread-safe in-memory buffer for storing telemetry messages
   - Uses FIFO strategy when buffer is full
   - Maximum capacity: 1000 messages (configurable)

3. **TelemetryController** (`Controllers/TelemetryController.cs`)
   - REST API endpoints for querying telemetry data
   - Provides access to buffer contents

### Message Flow

```
Simulator → Kafka (ecg.telemetry) → KafkaConsumerService → TelemetryBufferService → TelemetryController → Client
```

## Testing

1. **Start Kafka:**
   ```bash
   scripts/kafka/ensure-topics.sh
   ```

2. **Start the API:**
   ```bash
   cd backend/CardioFlow.Api
   dotnet run
   ```

3. **Send test data (in another terminal):**
   ```bash
   cd simulator/mitbih-replay
   source venv/bin/activate
   python replay.py --record 100 --limit 100
   ```

4. **Query the API:**
   ```bash
   curl "http://localhost:5050/api/ecg/latest?count=10"
   ```

## Logging

The API logs important events:
- Kafka consumer startup/shutdown
- Message consumption (every 100 messages)
- Buffer status updates
- Errors and exceptions

Log levels:
- Development: Debug
- Production: Information

## Error Handling

- **Kafka Connection Errors**: Automatic reconnection with exponential backoff
- **Message Deserialization Errors**: Logged but processing continues
- **Buffer Full**: Oldest messages are automatically removed (FIFO)

## Performance Considerations

- Sampling rate: 360 Hz = 360 messages/second
- Buffer capacity: 1000 messages ≈ 2.78 seconds of data
- The buffer uses `ConcurrentQueue` for thread-safe operations
- Background service runs independently of HTTP requests
