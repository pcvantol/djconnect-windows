using System.Text.Json.Serialization;

namespace DJConnect.Windows.Models;

public sealed record LocalPairingInfo(
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("firmware")] string Firmware,
    [property: JsonPropertyName("app_version")] string AppVersion,
    [property: JsonPropertyName("platform")] string Platform,
    [property: JsonPropertyName("paired")] bool Paired,
    [property: JsonPropertyName("local_url")] string LocalUrl,
    [property: JsonPropertyName("pair_code")] string? PairCode);

public sealed record LocalPairRequest(
    [property: JsonPropertyName("pair_code")] string? PairCode,
    [property: JsonPropertyName("pairing_code")] string? PairingCode,
    [property: JsonPropertyName("pairing_token")] string? PairingToken,
    [property: JsonPropertyName("device_id")] string? DeviceId,
    [property: JsonPropertyName("client_type")] string? ClientType,
    [property: JsonPropertyName("device_token")] string? DeviceToken,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("bearer_token")] string? BearerToken,
    [property: JsonPropertyName("ha_local_url")] string? HomeAssistantLocalUrl,
    [property: JsonPropertyName("device_language")] string? DeviceLanguage,
    [property: JsonPropertyName("language")] string? Language)
{
    public string? ResolvedPairCode => PairCode ?? PairingCode ?? PairingToken;
    public string? ResolvedDeviceToken => DeviceToken ?? BearerToken ?? Token;
}

public sealed record LocalPairResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("device_id")] string? DeviceId = null,
    [property: JsonPropertyName("client_type")] string? ClientType = null,
    [property: JsonPropertyName("paired")] bool? Paired = null);

public sealed record LocalPairingSnapshot(
    ClientIdentity Identity,
    string PairCode,
    string PairingStatus,
    string LocalUrl,
    bool Pairable,
    string Firmware,
    string AppVersion)
{
    public bool Paired => PairingStatus == "paired";
}
