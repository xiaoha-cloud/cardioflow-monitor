import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import type { TelemetryMessage } from "../types/telemetry";

type Props = {
  data: TelemetryMessage[];
};

export default function EcgChart({ data }: Props) {
  if (!data.length) {
    return <section className="panel">No ECG data yet. Start replay to see waveform.</section>;
  }

  const chartData = data
    .slice()
    .sort((a, b) => a.sampleIndex - b.sampleIndex)
    .map((item) => ({
      x: item.sampleIndex,
      lead1: item.lead1
    }));

  return (
    <section className="panel">
      <h3>ECG Waveform (Lead I)</h3>
      <div className="chart-wrap">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="x" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} domain={["auto", "auto"]} />
            <Tooltip />
            <Line type="monotone" dataKey="lead1" dot={false} stroke="#d7263d" strokeWidth={2} />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
