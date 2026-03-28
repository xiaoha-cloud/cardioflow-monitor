# CardioFlow explainer service

Rule-based FastAPI service. Specification: [docs/architecture/explainer-service-design.md](../../docs/architecture/explainer-service-design.md)

## Local run

```bash
cd ai/explainer-service
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

- Health: `GET http://localhost:8000/health`
- Explain: `POST http://localhost:8000/explain` with JSON body (see design doc)

## Docker

Build from this directory (context = `ai/explainer-service`):

```bash
docker build -t cardioflow-explainer:local .
docker run --rm -p 8000:8000 cardioflow-explainer:local
```
