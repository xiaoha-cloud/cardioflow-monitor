import type { SystemStatus } from "../types/telemetry";

type Props = {
  status: SystemStatus | null;
};

export default function SummaryCards({ status }: Props) {
  const streamStatus = status?.streamStatus ?? "stopped";
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
    { label: "Stream Status", value: streamStatus, isStatus: true },
    { label: "Sampling Rate", value: `${status?.samplingRate ?? 0} Hz` },
    { label: "Topic", value: status?.topic ?? "-" },
    { label: "Last Alert", value: lastAlertValue }
  ];

  return (
    <section className="cards-grid">
      {cards.map((card) => (
        <article key={card.label} className="card">
          <p className="card-label">{card.label}</p>
          {card.isStatus ? (
            <span className={`status-badge status-${String(card.value)}`}>{card.value}</span>
          ) : (
            <p className="card-value">{card.value}</p>
          )}
        </article>
      ))}
    </section>
  );
}
