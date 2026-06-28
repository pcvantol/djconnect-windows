using System.Text;

namespace DJConnect.Windows.Services;

public static class FastPathDiagnosticsFormatter
{
    public static string AboutText(FastPathDiagnostics diagnostics)
    {
        if (diagnostics.WebSocketConnected)
        {
            return "websocket fast path";
        }

        return diagnostics.FastPathTransport == "websocket" ? "websocket" : "http fallback";
    }

    public static void AppendTo(StringBuilder body, FastPathDiagnostics diagnostics)
    {
        body.AppendLine($"- Fast path transport: {diagnostics.FastPathTransport}");
        body.AppendLine($"- WebSocket connected: {diagnostics.WebSocketConnected.ToString().ToLowerInvariant()}");
        body.AppendLine($"- Last WebSocket error: {diagnostics.LastWebSocketError}");
        body.AppendLine($"- Last capability refresh: {diagnostics.LastCapabilityRefresh?.ToString("u") ?? "never"}");
        body.AppendLine($"- WebSocket commands: {string.Join(", ", diagnostics.WebSocketCommands)}");
    }
}

