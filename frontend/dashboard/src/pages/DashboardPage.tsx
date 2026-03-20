import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import AlertPanel from "../components/AlertPanel";
import EcgChart from "../components/EcgChart";
import EventLogViewer from "../components/EventLogViewer";
import PatientCard from "../components/PatientCard";
import SummaryCards from "../components/SummaryCards";
import { getAlerts, getCurrentPatient, getLatestEcg, getSystemStatus } from "../services/api";
import { telemetrySignalRClient } from "../services/signalr";
import type { AlertMessage, CurrentPatient, SystemStatus, TelemetryMessage } from "../types/telemetry";

const CHART_RETENTION_POINTS = 3600;
const MAX_ECG_POINTS = 800;
const MAX_ALERTS = 50;
const MAX_EVENT_LOGS = 30;
const FLUSH_INTERVAL_MS = 100;
const RECORD_OPTIONS = ["100", "101", "103"] as const;

function toPatientSnapshot(
  telemetry: TelemetryMessage | null | undefined,
  status: SystemStatus | null
): CurrentPatient | null {
  if (!telemetry && !status?.activePatient) {
    return null;
  }

  return {
    patientId: telemetry?.patientId ?? status?.activePatient ?? "-",
    recordId: telemetry?.recordId ?? status?.activeRecordId ?? status?.activeRecord ?? "-",
    deviceId: telemetry?.deviceId ?? status?.deviceId ?? "-",
    battery: telemetry?.battery ?? null,
    signalQuality: telemetry?.signalQuality ?? "unknown",
    rrIntervalMs: telemetry?.rrIntervalMs ?? telemetry?.derivedMetrics?.rrIntervalMs ?? null,
    streamStatus: status?.streamStatus ?? "stopped"
  };
}

function samePatientSnapshot(a: CurrentPatient | null, b: CurrentPatient | null): boolean {
  if (!a || !b) {
    return a === b;
  }

  return (
    a.patientId === b.patientId &&
    a.recordId === b.recordId &&
    a.deviceId === b.deviceId &&
    a.battery === b.battery &&
    a.signalQuality === b.signalQuality &&
    a.rrIntervalMs === b.rrIntervalMs &&
    a.streamStatus === b.streamStatus
  );
}

function normalizeAlert(alert: AlertMessage): AlertMessage {
  const severity = (alert.severity || "warning").toLowerCase();
  const supportedSeverity =
    severity === "critical" || severity === "warning" || severity === "normal" || severity === "info"
      ? severity
      : "warning";
  return {
    ...alert,
    severity: supportedSeverity,
    message: alert.message || "Abnormal ECG event",
    sourceRule: alert.sourceRule || "unknown",
    recordId: alert.recordId || "-",
    annotation: alert.annotation ?? null
  };
}

function alertKey(alert: AlertMessage): string {
  return `${alert.timestamp}-${alert.sampleIndex}-${alert.message || ""}-${alert.sourceRule || "unknown"}`;
}

function mergeAlerts(prev: AlertMessage[], incoming: AlertMessage, maxCount: number): AlertMessage[] {
  const nextAlert = normalizeAlert(incoming);
  const seen = new Set<string>([alertKey(nextAlert)]);
  const merged = [nextAlert, ...prev.filter((item) => {
    const key = alertKey(item);
    if (seen.has(key)) {
      return false;
    }
    seen.add(key);
    return true;
  })];

  return merged
    .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
    .slice(0, maxCount);
}

export default function DashboardPage() {
  const [selectedRecordId, setSelectedRecordId] = useState<string>("100");
  const [ecg, setEcg] = useState<TelemetryMessage[]>([]);
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [alerts, setAlerts] = useState<AlertMessage[]>([]);
  const [patient, setPatient] = useState<CurrentPatient | null>(null);
  const [windowSeconds, setWindowSeconds] = useState<5 | 10>(5);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionState, setConnectionState] = useState<
    "connecting" | "connected" | "reconnecting" | "disconnected"
  >("disconnected");
  const pendingTelemetryRef = useRef<TelemetryMessage[]>([]);
  const lastSampleRef = useRef<number | null>(null);
  const statusRef = useRef<SystemStatus | null>(null);
  const selectedRecordRef = useRef<string>("100");

  const loadData = useCallback(async (recordId: string): Promise<void> => {
    setLoading(true);

    try {
      const [ecgResult, statusResult, alertResult, patientResult] = await Promise.allSettled([
        getLatestEcg({ count: CHART_RETENTION_POINTS, recordId }),
        getSystemStatus(),
        getAlerts({ count: MAX_ALERTS, recordId }),
        getCurrentPatient()
      ]);

      const ecgData = ecgResult.status === "fulfilled" ? ecgResult.value : [];
      const statusData = statusResult.status === "fulfilled" ? statusResult.value : null;
      const alertData = alertResult.status === "fulfilled" ? alertResult.value : [];
      const patientData = patientResult.status === "fulfilled" ? patientResult.value : null;

      const sortedEcg = ecgData
        .slice()
        .sort((a, b) => a.sampleIndex - b.sampleIndex)
        .slice(-CHART_RETENTION_POINTS);

      setEcg(sortedEcg);
      setStatus(statusData);
      statusRef.current = statusData;
      setAlerts(
        alertData
          .map(normalizeAlert)
          .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
          .slice(0, MAX_ALERTS)
      );
      const derivedPatient = patientData ?? toPatientSnapshot(sortedEcg[sortedEcg.length - 1], statusData);
      setPatient(
        derivedPatient && derivedPatient.recordId !== "-" && derivedPatient.recordId !== recordId
          ? { ...derivedPatient, recordId }
          : derivedPatient
      );
      lastSampleRef.current = sortedEcg[sortedEcg.length - 1]?.sampleIndex ?? null;
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load dashboard data.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // Record switch policy: reset local buffers first, then reload REST baseline for the selected record.
    selectedRecordRef.current = selectedRecordId;
    pendingTelemetryRef.current = [];
    lastSampleRef.current = null;
    setEcg([]);
    setAlerts([]);
    setPatient(null);
    void loadData(selectedRecordId);
  }, [loadData, selectedRecordId]);

  useEffect(() => {
    // Dashboard runtime flow:
    // - keep telemetry batching on interval to avoid per-message chart re-render
    // - subscribe SignalR handlers once on mount
    // - unsubscribe + disconnect on unmount to prevent duplicate listeners
    let mounted = true;
    let flushTimer: number | null = null;

    const flushPendingTelemetry = (): void => {
      if (!mounted || pendingTelemetryRef.current.length === 0) {
        return;
      }

      const batch = pendingTelemetryRef.current.splice(0, pendingTelemetryRef.current.length);
      setEcg((prev) => {
        const combined = [...prev, ...batch].sort((a, b) => a.sampleIndex - b.sampleIndex);
        if (combined.length <= CHART_RETENTION_POINTS) {
          return combined;
        }
        return combined.slice(-CHART_RETENTION_POINTS);
      });
    };

    flushTimer = window.setInterval(flushPendingTelemetry, FLUSH_INTERVAL_MS);

    const handleTelemetry = (message: TelemetryMessage): void => {
      if (message.recordId !== selectedRecordRef.current) {
        return;
      }
      if (lastSampleRef.current === message.sampleIndex) {
        return;
      }
      lastSampleRef.current = message.sampleIndex;
      pendingTelemetryRef.current.push(message);
      const nextSnapshot = toPatientSnapshot(message, statusRef.current);
      setPatient((prev) => (samePatientSnapshot(prev, nextSnapshot) ? prev : nextSnapshot));
    };

    const handleAlert = (message: AlertMessage): void => {
      const messageRecordId = message.recordId;
      if (messageRecordId && messageRecordId !== selectedRecordRef.current) {
        return;
      }
      setAlerts((prev) => mergeAlerts(prev, message, MAX_ALERTS));
    };

    const handleSystemStatus = (message: SystemStatus): void => {
      setStatus(message);
      statusRef.current = message;
      setPatient((prev) => {
        const fallback = prev
          ? { ...prev, streamStatus: message.streamStatus }
          : toPatientSnapshot(null, message);
        return samePatientSnapshot(prev, fallback) ? prev : fallback;
      });
    };

    async function connectHub(): Promise<void> {
      try {
        await telemetrySignalRClient.connectTelemetryHub(setConnectionState);
        telemetrySignalRClient.onTelemetry(handleTelemetry);
        telemetrySignalRClient.onAlert(handleAlert);
        telemetrySignalRClient.onSystemStatus(handleSystemStatus);
      } catch (err) {
        if (mounted) {
          setConnectionState("disconnected");
          setError(err instanceof Error ? err.message : "SignalR connection failed.");
        }
      }
    }

    void connectHub();

    return () => {
      mounted = false;
      if (flushTimer) {
        window.clearInterval(flushTimer);
      }
      pendingTelemetryRef.current = [];
      telemetrySignalRClient.offTelemetry(handleTelemetry);
      telemetrySignalRClient.offAlert(handleAlert);
      telemetrySignalRClient.offSystemStatus(handleSystemStatus);
      void telemetrySignalRClient.disconnect();
    };
  }, []);

  const latestEcg = useMemo(() => ecg.slice(-CHART_RETENTION_POINTS), [ecg]);
  const eventLogData = useMemo(() => latestEcg.slice(-MAX_EVENT_LOGS), [latestEcg]);
  const samplingRate = status?.samplingRate ?? 360;

  return (
    <main className="page">
      <header className="app-header">
        <h1>CardioFlow Monitor</h1>
        <div className="header-actions">
          <label className="record-selector">
            <span>Record</span>
            <select
              value={selectedRecordId}
              onChange={(event) => setSelectedRecordId(event.target.value)}
              disabled={loading}
            >
              {RECORD_OPTIONS.map((recordId) => (
                <option key={recordId} value={recordId}>
                  {recordId}
                </option>
              ))}
            </select>
          </label>
          <span className={`connection-badge connection-${connectionState}`}>{connectionState}</span>
          <button className="refresh-btn" onClick={() => void loadData(selectedRecordId)} disabled={loading}>
            {loading ? "Loading..." : "Refresh"}
          </button>
        </div>
      </header>

      {error && <div className="notice">Failed to load data: {error}</div>}
      {loading ? (
        <div className="panel">Loading dashboard...</div>
      ) : (
        <>
          <SummaryCards status={status} selectedRecordId={selectedRecordId} />
          <section className="dashboard-main">
            <EcgChart
              data={latestEcg}
              samplingRate={samplingRate}
              windowSeconds={windowSeconds}
              maxRenderPoints={MAX_ECG_POINTS}
              onWindowChange={setWindowSeconds}
            />
            <AlertPanel alerts={alerts} />
          </section>
          <section className="dashboard-bottom">
            <PatientCard patient={patient} />
            <EventLogViewer events={eventLogData} />
          </section>
        </>
      )}
    </main>
  );
}
