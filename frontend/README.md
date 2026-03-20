# Frontend

CardioFlow dashboard is a React + TypeScript + Vite single-page monitor UI.

## Dashboard Features

- Summary cards for stream and status overview
- Real-time ECG chart (windowed rendering, rolling update)
- Alert panel with severity badges and HR info
- Patient/device card (patient, record, device, battery, signal quality)
- Event log viewer (latest telemetry summary rows)
- Record selector (`100`, `101`, `103`) for multi-record demo

## Local Run (Frontend)

```bash
cd frontend/dashboard
npm install
npm run dev
```

### Environment Variables

- `VITE_API_BASE_URL` (default: `http://localhost:5050`)
- `VITE_SIGNALR_HUB_URL` (default: `http://localhost:5050/hubs/telemetry`)

## Demo Startup Order

1. Start Kafka and topic setup
2. Start backend API
3. Start simulator replay (choose record 100/101/103)
4. Start frontend (`npm run dev`)

## Multi-Record Usage

- Use the top record selector to switch `100`, `101`, or `103`.
- On switch, frontend clears old chart/event/alert buffers and reloads from record-specific REST endpoints.
- Incoming SignalR telemetry and alerts are filtered by currently selected record.

## Screenshot Checklist

Store screenshots in `docs/screenshots/`:

- `dashboard-overview.png` (record=100, active waveform)
- `dashboard-alerts.png` (warning/critical alerts visible)
- `dashboard-record-101.png` (after switching to record 101)
