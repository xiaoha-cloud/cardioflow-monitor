export interface DerivedMetrics {
    rrIntervalMs?: number | null;
    hrvRmssd?: number | null;
    qrsWidthMs?: number | null;
}

export interface TelemetryMessage {
    patientId: string;
    recordId: string;
    deviceId: string;
    timestamp: string;
    receivedAt?: string | null;
    sampleIndex: number;
    lead1: number;
    annotation: string;
    heartRate: number | null;
    status: string;
    signalQuality: string | null;
    battery: number | null;
    rrIntervalMs?: number | null;
    isDerived?: boolean | null;
    derivedMetrics?: DerivedMetrics | null;
}

export type SignalQuality = "good" | "fair" | "poor" | string;

export interface ChartPoint {
    x: number;
    lead1: number;
    timestamp: string;
}

export interface CurrentPatient {
    patientId: string;
    recordId: string;
    deviceId: string;
    battery: number | null;
    signalQuality: SignalQuality;
    rrIntervalMs?: number | null;
    streamStatus: string;
}

// Backward-compatible alias for existing component/service imports.
export type PatientSnapshot = CurrentPatient;

export interface AlertMessage {
    patientId?: string;
    recordId?: string;
    deviceId?: string;
    timestamp: string;
    receivedAt?: string | null;
    sampleIndex: number;
    annotation?: string | null;
    severity: string;
    message: string;
    sourceRule?: string;
    heartRate?: number | null;
    rrIntervalMs?: number | null;
    metadata?: Record<string, string> | null;
    /** Short line from explainer service (optional). */
    explanationSummary?: string | null;
    /** Longer non-diagnostic text from explainer (optional). */
    explanationDetails?: string | null;
    /** Monitoring-oriented suggestion from explainer (optional). */
    recommendedAction?: string | null;
}

export interface SystemStatus {
    streamHealth?: "healthy" | "degraded" | "stale" | "down" | string;
    streamStatus: string;
    samplingRate: number;
    topic: string;
    activePatient: string | null;
    currentRecord?: string | null;
    activeRecord?: string | null;
    activeRecordId?: string | null;
    deviceId?: string | null;
    lastAlert: string | null;
    bufferCount: number;
    telemetryCount?: number;
    alertCount?: number;
    lastMessageTimestamp?: string | null;
    lastMessageAt: string | null;
    lastAlertAt?: string | null;
    consumerLagApprox?: number | null;
    uptimeSeconds?: number;
}
