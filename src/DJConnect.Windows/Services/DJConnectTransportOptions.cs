using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed record DJConnectTransportOptions(
    bool EnableLocalWebSocketFastPath,
    string? HomeAssistantWebSocketAuthToken)
{
    public static DJConnectTransportOptions Disabled { get; } = new(false, null);

    public static DJConnectTransportOptions FromEnvironment()
    {
        return new DJConnectTransportOptions(
            IsEnabled(Environment.GetEnvironmentVariable("DJCONNECT_ENABLE_HA_WEBSOCKET_FAST_PATH")),
            Environment.GetEnvironmentVariable("DJCONNECT_HA_WEBSOCKET_TOKEN"));
    }

    public bool AllowsWebSocketFastPath(HomeAssistantConnectionMode mode)
    {
        return mode == HomeAssistantConnectionMode.Local
            && EnableLocalWebSocketFastPath
            && !string.IsNullOrWhiteSpace(HomeAssistantWebSocketAuthToken);
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record DJConnectClientConfiguration(
    string HomeAssistantUrl,
    string? DeviceToken,
    bool EnableLocalWebSocketFastPath,
    string? HomeAssistantWebSocketAuthToken);
