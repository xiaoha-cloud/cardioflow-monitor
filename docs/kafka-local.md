# Kafka Local Setup

Kafka is used for local and development only. Production deployment is out of scope for Day 1.

## Prerequisites

- Docker and Docker Compose

## One-command prepare (start + create topics)

From the repository root, with Docker available:

```bash
chmod +x scripts/kafka/ensure-topics.sh && scripts/kafka/ensure-topics.sh
```

This starts Kafka and Zookeeper if needed, waits for the broker, creates `ecg.telemetry` and `ecg.alerts`, and lists topics.

## Start Kafka (manual)

From the repository root:

```bash
cd scripts/kafka && docker compose up -d
```

Or from anywhere:

```bash
docker compose -f /path/to/cardioflow-monitor/scripts/kafka/docker-compose.yml up -d
```

Check that containers are running:

```bash
docker compose -f scripts/kafka/docker-compose.yml ps
```

## Bootstrap address

- From host (backend, simulator, CLI tools): `localhost:9092`
- From another container on the same Docker network: `kafka:29092`

For local development, set (do not commit `.env`):

- `KAFKA_BOOTSTRAP_SERVERS=localhost:9092`

## Create topics

After Kafka is up, create the required topics:

```bash
docker compose -f scripts/kafka/docker-compose.yml exec kafka \
  kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic ecg.telemetry --partitions 3 --replication-factor 1

docker compose -f scripts/kafka/docker-compose.yml exec kafka \
  kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic ecg.alerts --partitions 3 --replication-factor 1
```

List topics:

```bash
docker compose -f scripts/kafka/docker-compose.yml exec kafka \
  kafka-topics --list --bootstrap-server localhost:9092
```

## Verify Kafka is usable

Console consumer (reads from `ecg.telemetry`; leave running to see messages):

```bash
docker compose -f scripts/kafka/docker-compose.yml exec kafka \
  kafka-console-consumer --bootstrap-server localhost:9092 \
  --topic ecg.telemetry --from-beginning
```

In another terminal, produce a test message:

```bash
docker compose -f scripts/kafka/docker-compose.yml exec kafka \
  kafka-console-producer --bootstrap-server localhost:9092 --topic ecg.telemetry
# Type a line and press Enter; it should appear in the consumer.
```

Stop Kafka:

```bash
cd scripts/kafka && docker compose down
```

## CI

CI does not start Kafka. It only builds code (e.g. `backend/*`, `frontend/*`). Kafka is for local and future integration tests when added.
