export interface TelemetryMessage {
    patientId: string;
    recordId: string;
    deviceId: string;
    timestamp: string;
    sampleIndex: number;
    lead1: number;
    annotation: string;
    heartRate: number | null;
    status: string;
    signalQuality: string;
    battery: number;
}

export type SignalQuality = "good" | "fair" | "poor" | string;

export interface ChartPoint {
    x: number;
    lead1: number;
    timestamp: string;
}

export interface PatientSnapshot {
    patientId: string;
    recordId: string;
    deviceId: string;
    battery: number | null;
    signalQuality: SignalQuality;
    streamStatus: string;
}

export interface AlertMessage {
    patientId: string;
    recordId?: string;
    deviceId: string;
    timestamp: string;
    sampleIndex: number;
    annotation: string;
    severity: string;
    message: string;
    heartRate: number | null;
}

export interface SystemStatus {
    streamStatus: string;
    samplingRate: number;
    topic: string;
    activePatient: string | null;
    activeRecord?: string | null;
    activeRecordId?: string | null;
    deviceId?: string | null;
    lastAlert: string | null;
    bufferCount: number;
    lastMessageAt: string | null;
}
