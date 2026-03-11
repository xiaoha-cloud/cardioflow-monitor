# CardioFlow API

ASP.NET Core Web API for consuming ECG telemetry data from Kafka and providing REST endpoints for accessing the data.

## Overview

This API service:
- Consumes ECG telemetry messages from Kafka topic `ecg.telemetry`
- Stores messages in an in-memory buffer (max 1000 messages)
- Provides REST endpoints to query telemetry data
- Runs as a background service for continuous message consumption

## Prerequisites

- .NET 8.0 SDK or later
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
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001
   - Swagger UI: http://localhost:5000/swagger

## API Endpoints

### GET /api/telemetry/latest?count=100
Get the latest N telemetry messages from the buffer.

**Query Parameters:**
- `count` (optional): Number of messages to retrieve (default: 100, max: 1000)

**Response:**
```json
[
  {
    "patientId": "mitdb-100",
    "recordId": "100",
    "deviceId": "ecg-sim-01",
    "timestamp": "2026-03-11T22:15:10.234Z",
    "sampleIndex": 12345,
    "lead1": 0.82,
    "annotation": "N",
    "heartRate": 72,
    "status": "normal",
    "signalQuality": "good",
    "battery": 87
  }
]
```

### GET /api/telemetry/all
Get all messages currently in the buffer (up to MaxBufferSize).

### GET /api/telemetry/status
Get buffer status information.

**Response:**
```json
{
  "count": 100,
  "hasMessages": true,
  "latestMessage": { ... }
}
```

### POST /api/telemetry/clear
Clear all messages from the buffer.

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
   curl http://localhost:5000/api/telemetry/latest?count=10
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
