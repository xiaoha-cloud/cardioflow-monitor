import type { AlertMessage, CurrentPatient, SystemStatus, TelemetryMessage } from "../types/telemetry";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5050";

async function request<T>(path: string): Promise<T> {
    try {
        const response = await fetch(`${API_BASE_URL}${path}`);
        if (!response.ok) {
            throw new Error(`Request failed (${response.status} ${response.statusText}): ${path}`);
        }

        return (await response.json()) as T;
    } catch (error) {
        if (error instanceof Error) {
            throw new Error(`API request error: ${error.message}`);
        }
        throw new Error("API request error: Unknown failure");
    }
}

function warnContract(endpoint: string, detail: string): void {
    if (import.meta.env.DEV) {
        console.warn(`[contract warning] ${endpoint}: ${detail}`);
    }
}

function parseTelemetry(endpoint: string, raw: unknown): TelemetryMessage {
    const item = (raw ?? {}) as Partial<TelemetryMessage>;
    if (!item.patientId || !item.recordId || !item.deviceId) {
        warnContract(endpoint, "missing identity fields in telemetry payload");
    }
    return {
        patientId: item.patientId ?? "-",
        recordId: item.recordId ?? "-",
        deviceId: item.deviceId ?? "-",
        timestamp: item.timestamp ?? "",
        receivedAt: item.receivedAt ?? null,
        sampleIndex: Number(item.sampleIndex ?? 0),
        lead1: Number(item.lead1 ?? 0),
        annotation: item.annotation ?? "-",
        heartRate: item.heartRate ?? null,
        status: item.status ?? "unknown",
        signalQuality: item.signalQuality ?? "unknown",
        battery: item.battery ?? null,
        rrIntervalMs: item.rrIntervalMs ?? item.derivedMetrics?.rrIntervalMs ?? null,
        isDerived: item.isDerived ?? null,
        derivedMetrics: item.derivedMetrics ?? null
    };
}

function parseAlert(endpoint: string, raw: unknown): AlertMessage {
    const item = (raw ?? {}) as Partial<AlertMessage>;
    if (!item.severity || !item.message) {
        warnContract(endpoint, "missing severity/message in alert payload");
    }
    return {
        patientId: item.patientId ?? "-",
        recordId: item.recordId ?? "-",
        deviceId: item.deviceId ?? "-",
        timestamp: item.timestamp ?? "",
        receivedAt: item.receivedAt ?? null,
        sampleIndex: Number(item.sampleIndex ?? 0),
        annotation: item.annotation ?? null,
        severity: item.severity ?? "warning",
        message: item.message ?? "Abnormal ECG event",
        sourceRule: item.sourceRule ?? "unknown",
        heartRate: item.heartRate ?? null,
        rrIntervalMs: item.rrIntervalMs ?? null,
        metadata: item.metadata ?? null
    };
}

function parseStatus(raw: unknown): SystemStatus {
    const item = (raw ?? {}) as Partial<SystemStatus>;
    return {
        streamHealth: item.streamHealth ?? "down",
        streamStatus: item.streamStatus ?? "stopped",
        samplingRate: Number(item.samplingRate ?? 360),
        topic: item.topic ?? "ecg.telemetry",
        activePatient: item.activePatient ?? null,
        currentRecord: item.currentRecord ?? null,
        activeRecord: item.activeRecord ?? null,
        activeRecordId: item.activeRecordId ?? null,
        deviceId: item.deviceId ?? null,
        lastAlert: item.lastAlert ?? null,
        bufferCount: Number(item.bufferCount ?? 0),
        telemetryCount: Number(item.telemetryCount ?? item.bufferCount ?? 0),
        alertCount: item.alertCount ?? 0,
        lastMessageTimestamp: item.lastMessageTimestamp ?? item.lastMessageAt ?? null,
        lastMessageAt: item.lastMessageAt ?? null,
        lastAlertAt: item.lastAlertAt ?? null,
        consumerLagApprox: item.consumerLagApprox ?? null,
        uptimeSeconds: item.uptimeSeconds ?? 0
    };
}

function parseCurrentPatient(raw: unknown): CurrentPatient {
    const item = (raw ?? {}) as Partial<CurrentPatient>;
    return {
        patientId: item.patientId ?? "-",
        recordId: item.recordId ?? "-",
        deviceId: item.deviceId ?? "-",
        battery: item.battery ?? null,
        signalQuality: item.signalQuality ?? "unknown",
        rrIntervalMs: item.rrIntervalMs ?? null,
        streamStatus: item.streamStatus ?? "stopped"
    };
}

export function getLatestEcg(options?: { count?: number; recordId?: string }): Promise<TelemetryMessage[]> {
    const count = options?.count ?? 500;
    const safeCount = Math.min(1000, Math.max(1, count));
    const query = new URLSearchParams({ count: String(safeCount) });
    if (options?.recordId) {
        query.set("recordId", options.recordId);
    }
    return request<unknown[]>(`/api/ecg/latest?${query.toString()}`)
        .then((items) => Array.isArray(items) ? items.map((item) => parseTelemetry("/api/ecg/latest", item)) : []);
}

export function getSystemStatus(): Promise<SystemStatus> {
    return request<unknown>("/api/system/status").then(parseStatus);
}

export function getAlerts(options?: { count?: number; recordId?: string }): Promise<AlertMessage[]> {
    const count = options?.count ?? 20;
    const safeCount = Math.min(100, Math.max(1, count));
    const query = new URLSearchParams({ count: String(safeCount) });
    if (options?.recordId) {
        query.set("recordId", options.recordId);
    }
    return request<unknown[]>(`/api/alerts?${query.toString()}`)
        .then((items) => Array.isArray(items) ? items.map((item) => parseAlert("/api/alerts", item)) : []);
}

export function getCurrentPatient(): Promise<CurrentPatient> {
    return request<unknown>("/api/patients/current").then(parseCurrentPatient);
}
