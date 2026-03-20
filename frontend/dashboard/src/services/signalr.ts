import * as signalR from "@microsoft/signalr";
import type { AlertMessage, SystemStatus, TelemetryMessage } from "../types/telemetry";

export type HubConnectionState =
    | "connecting"
    | "connected"
    | "reconnecting"
    | "disconnected";

const HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL ?? "http://localhost:5050/hubs/telemetry";

class TelemetrySignalRClient {
    private connection: signalR.HubConnection | null = null;
    private startPromise: Promise<void> | null = null;
    private disconnectRequested = false;

    async connectTelemetryHub(onConnectionStateChange?: (state: HubConnectionState) => void): Promise<void> {
        if (this.startPromise) {
            await this.startPromise;
            return;
        }
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            onConnectionStateChange?.("connected");
            return;
        }
        if (this.connection?.state === signalR.HubConnectionState.Connecting) {
            onConnectionStateChange?.("connecting");
            return;
        }

        this.disconnectRequested = false;
        onConnectionStateChange?.("connecting");

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                // Avoid credentialed CORS mode for local dev (no auth cookie needed).
                withCredentials: false
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.onreconnecting(() => {
            onConnectionStateChange?.("reconnecting");
        });

        this.connection.onreconnected(() => {
            onConnectionStateChange?.("connected");
        });

        this.connection.onclose(() => {
            onConnectionStateChange?.("disconnected");
        });

        this.startPromise = this.connection
            .start()
            .then(() => {
                if (!this.disconnectRequested) {
                    onConnectionStateChange?.("connected");
                }
            })
            .catch((err) => {
                const message = String(err ?? "");
                // Ignore expected dev-time race when effect cleanup stops a connecting hub.
                if (this.disconnectRequested || message.includes("stopped during negotiation")) {
                    return;
                }
                throw err;
            })
            .finally(() => {
                this.startPromise = null;
            });

        await this.startPromise;
    }

    onTelemetry(callback: (message: TelemetryMessage) => void): void {
        this.connection?.on("ReceiveTelemetry", callback);
    }

    onAlert(callback: (message: AlertMessage) => void): void {
        this.connection?.on("ReceiveAlert", callback);
    }

    onSystemStatus(callback: (message: SystemStatus) => void): void {
        this.connection?.on("ReceiveSystemStatus", callback);
    }

    offTelemetry(callback: (message: TelemetryMessage) => void): void {
        this.connection?.off("ReceiveTelemetry", callback);
    }

    offAlert(callback: (message: AlertMessage) => void): void {
        this.connection?.off("ReceiveAlert", callback);
    }

    offSystemStatus(callback: (message: SystemStatus) => void): void {
        this.connection?.off("ReceiveSystemStatus", callback);
    }

    async disconnect(): Promise<void> {
        if (!this.connection) {
            return;
        }

        this.disconnectRequested = true;

        if (this.startPromise) {
            try {
                await this.startPromise;
            }
            catch
            {
                // Ignore start failure while disconnecting.
            }
        }

        if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
            await this.connection.stop();
        }
        this.connection = null;
    }
}

export const telemetrySignalRClient = new TelemetrySignalRClient();
