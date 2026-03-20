import { useCallback, useEffect, useMemo, useState } from "react";
import AlertPanel from "../components/AlertPanel";
import EcgChart from "../components/EcgChart";
import SummaryCards from "../components/SummaryCards";
import { getAlerts, getLatestEcg, getSystemStatus } from "../services/api";
import { telemetrySignalRClient } from "../services/signalr";
import type { AlertMessage, SystemStatus, TelemetryMessage } from "../types/telemetry";

const MAX_ECG_POINTS = 500;
const MAX_ALERTS = 20;

export default function DashboardPage() {
  const [ecg, setEcg] = useState<TelemetryMessage[]>([]);
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [alerts, setAlerts] = useState<AlertMessage[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionState, setConnectionState] = useState<
    "connecting" | "connected" | "reconnecting" | "disconnected"
  >("disconnected");

  const loadData = useCallback(async (): Promise<void> => {
    setLoading(true);

    try {
      const [ecgData, statusData, alertData] = await Promise.all([
        getLatestEcg(MAX_ECG_POINTS),
        getSystemStatus(),
        getAlerts(MAX_ALERTS)
      ]);

      setEcg(ecgData.slice(-MAX_ECG_POINTS));
      setStatus(statusData);
      setAlerts(alertData.slice(0, MAX_ALERTS));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load dashboard data.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useEffect(() => {
    let mounted = true;

    const handleTelemetry = (message: TelemetryMessage): void => {
      setEcg((prev) => {
        if (prev.some((item) => item.sampleIndex === message.sampleIndex)) {
          return prev;
        }

        const next = [...prev, message]
          .sort((a, b) => a.sampleIndex - b.sampleIndex)
          .slice(-MAX_ECG_POINTS);
        return next;
      });
    };

    const handleAlert = (message: AlertMessage): void => {
      setAlerts((prev) => [message, ...prev].slice(0, MAX_ALERTS));
    };

    const handleSystemStatus = (message: SystemStatus): void => {
      setStatus(message);
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
      telemetrySignalRClient.offTelemetry(handleTelemetry);
      telemetrySignalRClient.offAlert(handleAlert);
      telemetrySignalRClient.offSystemStatus(handleSystemStatus);
      void telemetrySignalRClient.disconnect();
    };
  }, []);

  const latestEcg = useMemo(() => ecg.slice(-MAX_ECG_POINTS), [ecg]);

  return (
    <main className="page">
      <header>
        <h1>CardioFlow Monitor</h1>
        <div className="header-actions">
          <span className={`connection-badge connection-${connectionState}`}>{connectionState}</span>
          <button className="refresh-btn" onClick={() => void loadData()} disabled={loading}>
            {loading ? "Loading..." : "Refresh"}
          </button>
        </div>
      </header>

      {error && <div className="notice">Failed to load data: {error}</div>}
      {loading ? (
        <div className="panel">Loading dashboard...</div>
      ) : (
        <>
          <SummaryCards status={status} />
          <EcgChart data={latestEcg} />
          <AlertPanel alerts={alerts} />
        </>
      )}
    </main>
  );
}
