using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Models;

public sealed record VersionCompatibilityResult(bool IsCompatible, string RequiredMajorMinor, string? HomeAssistantMajorMinor);

public static class VersionCompatibility
{
    public static VersionCompatibilityResult Evaluate(
        string appProtocolLine,
        string? haVersion,
        string? haMajorMinor,
        bool updateRequired,
        string? error)
    {
        var required = ParseMajorMinor(appProtocolLine) ?? DJConnectContract.ProtocolLine;
        var actual = ParseMajorMinor(haMajorMinor) ?? ParseMajorMinor(haVersion);

        if (string.Equals(actual, "0.0", StringComparison.OrdinalIgnoreCase))
        {
            return new VersionCompatibilityResult(true, required, actual);
        }

        if (updateRequired || string.Equals(error, "version_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            return new VersionCompatibilityResult(false, required, actual);
        }

        if (!string.IsNullOrWhiteSpace(actual)
            && !string.Equals(actual, required, StringComparison.OrdinalIgnoreCase))
        {
            return new VersionCompatibilityResult(false, required, actual);
        }

        return new VersionCompatibilityResult(true, required, actual);
    }

    public static string? ParseMajorMinor(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var parts = version.Trim().Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        return int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor)
            ? $"{major}.{minor}"
            : null;
    }
}
