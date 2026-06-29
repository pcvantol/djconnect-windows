namespace DJConnect.Windows.Services;

public static class PairingStatePolicy
{
    public static bool RequiresLocalPairingCleanup(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("stale", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not_configured", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not configured", StringComparison.OrdinalIgnoreCase)
            || error.Contains("401", StringComparison.OrdinalIgnoreCase)
            || error.Contains("403", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
            || error.Contains("token is no longer accepted", StringComparison.OrdinalIgnoreCase);
    }
}
