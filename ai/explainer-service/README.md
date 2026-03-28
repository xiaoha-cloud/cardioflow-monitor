# CardioFlow explainer service

FastAPI service with optional **LangChain + OpenAI** explanations and **rule-based fallback**. Specification: [docs/architecture/explainer-service-design.md](../../docs/architecture/explainer-service-design.md)

## Environment variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENAI_API_KEY` | No | If set, `/explain` uses the configured OpenAI model via LangChain structured output. If unset or empty, responses are rule-based only. |
| `OPENAI_MODEL` | No | Defaults to `gpt-4o-mini`. |

Do not commit API keys. Export in your shell or inject via your host / Kubernetes Secret.

## Local run

```bash
cd ai/explainer-service
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
# optional: export OPENAI_API_KEY=sk-...
uvicorn app.main:app --reload --port 8000
```

- Health: `GET http://localhost:8000/health`
- Explain: `POST http://localhost:8000/explain` with JSON body (see design doc)

## Docker

Build from the **repository root** (same pattern as the backend image):

```bash
docker build -t cardioflow-explainer -f ai/explainer-service/Dockerfile .
docker run --rm -p 8000:8000 cardioflow-explainer
```

Optional LLM inside the container:

```bash
docker run --rm -p 8000:8000 -e OPENAI_API_KEY -e OPENAI_MODEL cardioflow-explainer
```

Omit `OPENAI_API_KEY` to run in rule-based-only mode.
