import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import type { ChartPoint, TelemetryMessage } from "../types/telemetry";

type Props = {
  data: TelemetryMessage[];
  samplingRate: number;
  windowSeconds: 5 | 10;
  maxRenderPoints: number;
  onWindowChange: (seconds: 5 | 10) => void;
};

function toChartPoint(item: TelemetryMessage): ChartPoint {
  return {
    x: item.sampleIndex,
    lead1: item.lead1,
    timestamp: item.timestamp
  };
}

export default function EcgChart({
  data,
  samplingRate,
  windowSeconds,
  maxRenderPoints,
  onWindowChange
}: Props) {
  const windowPointLimit = Math.max(1, Math.floor(samplingRate * windowSeconds));

  if (!data.length) {
    return (
      <section className="panel ecg-panel">
        <div className="ecg-header">
          <h3>ECG Waveform (Lead I)</h3>
          <div className="ecg-window-toggle">
            <button
              type="button"
              className={`window-btn ${windowSeconds === 5 ? "window-btn-active" : ""}`}
              onClick={() => onWindowChange(5)}
            >
              5s
            </button>
            <button
              type="button"
              className={`window-btn ${windowSeconds === 10 ? "window-btn-active" : ""}`}
              onClick={() => onWindowChange(10)}
            >
              10s
            </button>
          </div>
        </div>
        <div className="chart-empty">No ECG data yet. Start replay to see waveform.</div>
      </section>
    );
  }

  const sortedData = data.slice().sort((a, b) => a.sampleIndex - b.sampleIndex);
  const latestX = sortedData[sortedData.length - 1]?.sampleIndex ?? 0;
  const minX = Math.max(0, latestX - windowPointLimit);
  const windowedData = sortedData.filter((item) => item.sampleIndex >= minX);
  const stride = Math.max(1, Math.ceil(windowedData.length / maxRenderPoints));
  const chartData = windowedData.filter((_, index) => index % stride === 0).map(toChartPoint);

  return (
    <section className="panel ecg-panel">
      <div className="ecg-header">
        <h3>ECG Waveform (Lead I)</h3>
        <div className="ecg-window-toggle">
          <button
            type="button"
            className={`window-btn ${windowSeconds === 5 ? "window-btn-active" : ""}`}
            onClick={() => onWindowChange(5)}
          >
            5s
          </button>
          <button
            type="button"
            className={`window-btn ${windowSeconds === 10 ? "window-btn-active" : ""}`}
            onClick={() => onWindowChange(10)}
          >
            10s
          </button>
        </div>
      </div>
      <div className="chart-wrap">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="x" tick={{ fontSize: 12 }} domain={[minX, "dataMax"]} type="number" />
            <YAxis tick={{ fontSize: 12 }} domain={["auto", "auto"]} />
            <Tooltip
              labelFormatter={(value) => `Sample: ${value}`}
              formatter={(value: number) => [value.toFixed(4), "Lead1"]}
            />
            <Line type="monotone" dataKey="lead1" dot={false} stroke="#d7263d" strokeWidth={2} />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
