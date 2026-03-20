import type { AlertMessage, SystemStatus, TelemetryMessage } from "../types/telemetry";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5050";

async function request<T>(path: string): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${path}`);
    if (!response.ok) {
        throw new Error(`Request failed (${response.status} ${response.statusText}): ${path}`);
    }

    return (await response.json()) as T;
}

export function getLatestEcg(count = 500): Promise<TelemetryMessage[]> {
    const safeCount = Math.min(1000, Math.max(1, count));
    return request<TelemetryMessage[]>(`/api/ecg/latest?count=${safeCount}`);
}

export function getSystemStatus(): Promise<SystemStatus> {
    return request<SystemStatus>("/api/system/status");
}

export function getAlerts(count = 20): Promise<AlertMessage[]> {
    const safeCount = Math.min(100, Math.max(1, count));
    return request<AlertMessage[]>(`/api/alerts?count=${safeCount}`);
}
