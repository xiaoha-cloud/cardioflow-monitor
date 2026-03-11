#!/usr/bin/env bash
# Ensures Kafka is up and topics ecg.telemetry, ecg.alerts exist.
# Run from repo root: scripts/kafka/ensure-topics.sh
# Or from scripts/kafka: ./ensure-topics.sh (uses compose in current dir)

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Starting Kafka and Zookeeper if not already running..."
docker compose up -d

echo "Waiting for Kafka to be ready..."
for i in {1..30}; do
  if docker compose exec -T kafka kafka-broker-api-versions --bootstrap-server localhost:9092 >/dev/null 2>&1; then
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Kafka did not become ready in time."
    exit 1
  fi
  sleep 2
done

echo "Creating topics..."
docker compose exec -T kafka kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic ecg.telemetry --partitions 3 --replication-factor 1

docker compose exec -T kafka kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --topic ecg.alerts --partitions 3 --replication-factor 1

echo "Topics:"
docker compose exec -T kafka kafka-topics --list --bootstrap-server localhost:9092
echo "Done. Kafka is ready at localhost:9092."
