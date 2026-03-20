import type { AlertMessage } from "../types/telemetry";

type Props = {
  alerts: AlertMessage[];
};

function formatTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }
  const hh = String(date.getHours()).padStart(2, "0");
  const mm = String(date.getMinutes()).padStart(2, "0");
  const ss = String(date.getSeconds()).padStart(2, "0");
  const mmm = String(date.getMilliseconds()).padStart(3, "0");
  return `${hh}:${mm}:${ss}.${mmm}`;
}

function normalizeSeverity(value: string | null | undefined): "normal" | "warning" | "critical" | "info" {
  const normalized = (value ?? "").toLowerCase();
  if (normalized === "normal" || normalized === "warning" || normalized === "critical" || normalized === "info") {
    return normalized;
  }
  return "warning";
}

export default function AlertPanel({ alerts }: Props) {
  // UI safety cap: always render at most 50 newest alerts in panel.
  const visibleAlerts = alerts.slice(0, 50);

  return (
    <section className="panel alert-panel">
      <h3>Recent Alerts</h3>
      {visibleAlerts.length === 0 ? (
        <p className="subtle">No alerts</p>
      ) : (
        <div className="alerts-scroll">
          <ul className="alerts-list">
            {visibleAlerts.map((alert) => (
              <li key={`${alert.timestamp}-${alert.sampleIndex}-${alert.message}-${alert.sourceRule || "unknown"}`} className="alert-item">
                <span className="alert-time">{formatTime(alert.timestamp)}</span>
                <span className="alert-annotation">{alert.annotation || "-"}</span>
                <span className={`severity-tag severity-${normalizeSeverity(alert.severity)}`}>
                  {normalizeSeverity(alert.severity)}
                </span>
                <span className="alert-hr">
                  HR: {alert.heartRate ?? "--"}
                </span>
                <span className="alert-rr">
                  RR: {alert.rrIntervalMs == null ? "-- ms" : `${Math.round(alert.rrIntervalMs)} ms`}
                </span>
                <span className="alert-message">{alert.message || "Abnormal ECG event"}</span>
                <span className="alert-rule">{alert.sourceRule || "unknown"}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
