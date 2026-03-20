import type { AlertMessage, PatientSnapshot, SystemStatus, TelemetryMessage } from "../types/telemetry";

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

export function getLatestEcg(options?: { count?: number; recordId?: string }): Promise<TelemetryMessage[]> {
    const count = options?.count ?? 500;
    const safeCount = Math.min(1000, Math.max(1, count));
    const query = new URLSearchParams({ count: String(safeCount) });
    if (options?.recordId) {
        query.set("recordId", options.recordId);
    }
    return request<TelemetryMessage[]>(`/api/ecg/latest?${query.toString()}`);
}

export function getSystemStatus(): Promise<SystemStatus> {
    return request<SystemStatus>("/api/system/status");
}

export function getAlerts(options?: { count?: number; recordId?: string }): Promise<AlertMessage[]> {
    const count = options?.count ?? 20;
    const safeCount = Math.min(100, Math.max(1, count));
    const query = new URLSearchParams({ count: String(safeCount) });
    if (options?.recordId) {
        query.set("recordId", options.recordId);
    }
    return request<AlertMessage[]>(`/api/alerts?${query.toString()}`);
}

export function getCurrentPatient(): Promise<PatientSnapshot> {
    return request<PatientSnapshot>("/api/patients/current");
}
