import type { PatientSnapshot } from "../types/telemetry";

type Props = {
  patient: PatientSnapshot | null;
};

type Field = {
  label: string;
  value: string;
};

function qualityClass(quality: string): string {
  const normalized = quality.trim().toLowerCase();
  if (normalized === "good" || normalized === "fair" || normalized === "poor") {
    return normalized;
  }
  return "unknown";
}

export default function PatientCard({ patient }: Props) {
  const batteryValue = patient?.battery ?? null;
  const batteryText = batteryValue === null ? "-" : `${batteryValue}%`;
  const signalQuality = patient?.signalQuality ?? "-";
  const fields: Field[] = [
    { label: "Patient ID", value: patient?.patientId || "-" },
    { label: "Record ID", value: patient?.recordId || "-" },
    { label: "Device ID", value: patient?.deviceId || "-" }
  ];

  return (
    <section className="panel patient-card">
      <h3>Patient & Device</h3>
      {!patient && <p className="subtle patient-empty">No patient snapshot yet</p>}
      <div className="kv-grid">
        {fields.map((field) => (
          <div key={field.label} className="kv-item">
            <p className="kv-label">{field.label}</p>
            <p className="kv-value">{field.value}</p>
          </div>
        ))}
        <div className="kv-item">
          <p className="kv-label">Battery</p>
          <div className="battery-row">
            <div className="battery-track">
              <div
                className={`battery-fill ${batteryValue !== null && batteryValue < 20 ? "battery-low" : ""}`}
                style={{ width: `${Math.max(0, Math.min(100, batteryValue ?? 0))}%` }}
              />
            </div>
            <p className="kv-value battery-value">{batteryText}</p>
          </div>
        </div>
        <div className="kv-item">
          <p className="kv-label">Signal Quality</p>
          <span className={`quality-badge quality-${qualityClass(signalQuality)}`}>{signalQuality}</span>
        </div>
      </div>
    </section>
  );
}
