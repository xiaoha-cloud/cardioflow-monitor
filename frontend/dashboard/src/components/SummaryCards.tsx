import type { SystemStatus } from "../types/telemetry";

type Props = {
  status: SystemStatus | null;
  selectedRecordId: string;
};

export default function SummaryCards({ status, selectedRecordId }: Props) {
  const streamStatus = status?.streamStatus ?? "stopped";
  const streamHealth = (status?.streamHealth ?? "down").toLowerCase();
  const normalizeHealth = streamHealth === "healthy" || streamHealth === "degraded" || streamHealth === "stale" || streamHealth === "down"
    ? streamHealth
    : "down";
  const lastMessageValue = (() => {
    const value = status?.lastMessageTimestamp ?? status?.lastMessageAt;
    if (!value) {
      return "-";
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "-" : date.toLocaleTimeString();
  })();
  const lastAlertValue = (() => {
    const value = status?.lastAlert;
    if (!value) {
      return "-";
    }

    if (typeof value === "string") {
      return value;
    }

    if (typeof value === "object" && "message" in value) {
      return String((value as { message?: unknown }).message ?? "-");
    }

    return "-";
  })();

  const cards = [
    { label: "Active Patient", value: status?.activePatient ?? "-" },
    { label: "Selected Record", value: selectedRecordId },
    { label: "Stream Health", value: normalizeHealth, isHealth: true },
    { label: "Stream Status", value: streamStatus, isStatus: true },
    { label: "Current Record", value: status?.currentRecord ?? status?.activeRecordId ?? status?.activeRecord ?? "-" },
    { label: "Telemetry Count", value: String(status?.telemetryCount ?? status?.bufferCount ?? 0) },
    { label: "Alert Count", value: String(status?.alertCount ?? 0) },
    { label: "Last Message", value: lastMessageValue },
    { label: "Sampling Rate", value: `${status?.samplingRate ?? 0} Hz` },
    { label: "Consumer Lag", value: status?.consumerLagApprox == null ? "-" : String(status.consumerLagApprox) },
    { label: "Uptime", value: status?.uptimeSeconds == null ? "-" : `${status.uptimeSeconds}s` },
    { label: "Last Alert", value: lastAlertValue }
  ];

  return (
    <section className="cards-grid">
      {cards.map((card) => (
        <article key={card.label} className="card">
          <p className="card-label">{card.label}</p>
          {card.isStatus ? (
            <span className={`status-badge status-${String(card.value)}`}>{card.value}</span>
          ) : card.isHealth ? (
            <span className={`status-badge health-${String(card.value)}`}>{card.value}</span>
          ) : (
            <p className="card-value">{card.value}</p>
          )}
        </article>
      ))}
    </section>
  );
}
