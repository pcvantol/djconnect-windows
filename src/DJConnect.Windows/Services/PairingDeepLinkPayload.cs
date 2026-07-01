using System.Text.Json;
using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Services;

public sealed record PairingDeepLinkPayload(
    string HomeAssistantUrl,
    string PairCode,
    string ClientType,
    string PairPath)
{
    public const string PairEndpoint = "/api/djconnect/pair";

    public static bool TryParse(string? payload, out PairingDeepLinkPayload result, out string failureReason)
    {
        result = new PairingDeepLinkPayload("", "", "", "");
        failureReason = "";

        if (string.IsNullOrWhiteSpace(payload))
        {
            failureReason = "empty";
            return false;
        }

        var values = payload.TrimStart().StartsWith("{", StringComparison.Ordinal)
            ? ParseJson(payload)
            : ParseQueryPayload(payload);

        var candidate = new PairingDeepLinkPayload(
            values.GetValueOrDefault("ha_url") ?? "",
            values.GetValueOrDefault("pair_code") ?? "",
            values.GetValueOrDefault("client_type") ?? "",
            values.GetValueOrDefault("pair_path") ?? "");

        if (!string.Equals(candidate.ClientType, DJConnectContract.ClientType, StringComparison.Ordinal))
        {
            failureReason = "client_type";
            return false;
        }

        if (!string.Equals(candidate.PairPath, PairEndpoint, StringComparison.Ordinal))
        {
            failureReason = "pair_path";
            return false;
        }

        if (!IsValidPairCode(candidate.PairCode))
        {
            failureReason = "pair_code";
            return false;
        }

        if (!IsValidHomeAssistantUrl(candidate.HomeAssistantUrl))
        {
            failureReason = "ha_url";
            return false;
        }

        result = candidate;
        return true;
    }

    public static bool IsValidHomeAssistantUrl(string? url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https" && !string.IsNullOrWhiteSpace(uri.Host);
    }

    public static bool IsValidPairCode(string? pairCode)
    {
        return pairCode?.Trim() is { Length: 6 } trimmed && trimmed.All(char.IsDigit);
    }

    private static Dictionary<string, string?> ParseJson(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
        }

        return values;
    }

    private static Dictionary<string, string?> ParseQueryPayload(string payload)
    {
        var query = payload;
        if (Uri.TryCreate(payload, UriKind.Absolute, out var uri))
        {
            query = uri.Query;
        }

        query = query.TrimStart('?');
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pieces[0]);
            var value = pieces.Length == 2 ? Uri.UnescapeDataString(pieces[1].Replace("+", "%20", StringComparison.Ordinal)) : "";
            values[key] = value;
        }

        return values;
    }
}
