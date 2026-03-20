import type { TelemetryMessage } from "../types/telemetry";

type Props = {
  events: TelemetryMessage[];
};

const MAX_EVENTS = 50;
const DEFAULT_VISIBLE_LOGS = 30;

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

export default function EventLogViewer({ events }: Props) {
  const latestEvents = events.slice(-Math.min(MAX_EVENTS, DEFAULT_VISIBLE_LOGS)).reverse();

  return (
    <section className="panel event-log-panel">
      <h3>Event Log</h3>
      {latestEvents.length === 0 ? (
        <p className="subtle">No telemetry events yet</p>
      ) : (
        <div className="event-log-scroll">
          <table className="event-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Sample</th>
                <th>Annotation</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {latestEvents.map((item) => (
                <tr key={`${item.timestamp}-${item.sampleIndex}-${item.annotation}`}>
                  <td>{formatTime(item.timestamp)}</td>
                  <td>{item.sampleIndex}</td>
                  <td>{item.annotation || "-"}</td>
                  <td>{item.status || "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
