# CardioFlow Monitor

ECG monitoring stack: backend (ASP.NET Core + Kafka), React frontend, and a simulator for data replay. Day 1 covers repository layout and local Kafka only; no app code or CI yet.

## Tech stack

- **Backend:** ASP.NET Core, Kafka (telemetry and alerts)
- **Frontend:** React (to be initialized later)
- **Simulator:** Data replay (e.g. future MIT-BIH replay); language TBD
- **Local Kafka:** Docker Compose (development only)

## Directory structure

| Path | Purpose | CI build |
|------|---------|----------|
| `backend/` | ASP.NET Core API and Kafka consumers | `backend/*` |
| `frontend/` | React app (not yet initialized) | `frontend/*` |
| `simulator/` | Replay/simulation (e.g. `simulator/mitbih-replay`) | `simulator/*` |
| `docs/` | Documentation | - |
| `docs/architecture/` | Architecture notes and diagrams | - |
| `docs/screenshots/` | UI or runbook screenshots | - |
| `scripts/kafka/` | Docker Compose and helpers for local Kafka | - |

Empty directories are kept in git via `.gitkeep` or a short `README.md` so CI can rely on path-based builds.

## Prerequisites

- .NET SDK (for backend)
- Node.js and npm/yarn (for frontend, when added)
- Python 3 (optional, for simulator tooling if needed)
- Docker and Docker Compose (for local Kafka)

## Environment variables

For local runs you may need:

- `KAFKA_BOOTSTRAP_SERVERS` – e.g. `localhost:9092` when using the Compose stack

Do not commit `.env` or any file containing secrets. Prefer `.env.local` or OS/user-specific config and add only the variable names and examples in this README or in `docs/`.

## Kafka (local development)

Kafka is for local and development use only. Production deployment is not in scope for Day 1.

1. **Prepare Kafka (one command)**

   From repo root, with Docker installed:

   ```bash
   chmod +x scripts/kafka/ensure-topics.sh && scripts/kafka/ensure-topics.sh
   ```

   This starts Kafka, creates `ecg.telemetry` and `ecg.alerts`, and lists topics.

2. **Start Kafka manually, or create topics and verify**

   See [docs/kafka-local.md](docs/kafka-local.md) for:
   - Creating `ecg.telemetry` and `ecg.alerts`
   - Listing topics
   - Using `kafka-console-consumer` / `kafka-console-producer` to verify

CI does not start Kafka; it only builds the code (e.g. `backend/*`, `frontend/*`).

## Later

- **CI/CD:** GitHub Actions (e.g. build, test, optional lint) will be added in a later phase (Phase 4.5).
- **Deployment:** Target (e.g. Azure) and deployment method to be documented when introduced. Badges and deployment instructions will be added then.
