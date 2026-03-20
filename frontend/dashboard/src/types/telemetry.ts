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

export interface AlertMessage {
    patientId: string;
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
    lastAlert: string | null;
    bufferCount: number;
    lastMessageAt: string | null;
}
