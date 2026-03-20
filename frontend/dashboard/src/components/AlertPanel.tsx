import type { AlertMessage } from "../types/telemetry";

type Props = {
  alerts: AlertMessage[];
};

export default function AlertPanel({ alerts }: Props) {
  const visibleAlerts = alerts.slice(0, 10);

  return (
    <section className="panel">
      <h3>Recent Alerts</h3>
      {visibleAlerts.length === 0 ? (
        <p>No alerts</p>
      ) : (
        <ul className="alerts-list">
          {visibleAlerts.map((alert) => (
            <li key={`${alert.timestamp}-${alert.sampleIndex}`} className="alert-item">
              <span>{new Date(alert.timestamp).toLocaleTimeString()}</span>
              <span>{alert.annotation}</span>
              <span className="severity">{alert.severity}</span>
              <span>{alert.message}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
