using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed class DJConnectApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public DJConnectApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void Configure(string homeAssistantUrl, string? token)
    {
        _httpClient.BaseAddress = new Uri(homeAssistantUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<PairingResponse> PairAsync(PairingPayload payload, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/device/pair", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<PairingResponse>(response, cancellationToken);
    }

    public async Task<StatusResponse> GetStatusAsync(ClientIdentity identity, CancellationToken cancellationToken)
    {
        var payload = new
        {
            device_id = identity.DeviceId,
            device_name = identity.DeviceName,
            client_type = identity.ClientType,
            firmware = "windows-app"
        };
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/status", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<StatusResponse>(response, cancellationToken);
    }

    public async Task<AskDJHistoryResponse> GetAskDJHistoryAsync(long sinceRevision, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"api/djconnect/ask_dj/history?since_revision={sinceRevision}", cancellationToken);
        return await ReadJsonAsync<AskDJHistoryResponse>(response, cancellationToken);
    }

    public async Task<AskDJMessageResponse> SendAskDJMessageAsync(AskDJRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/ask_dj/message", request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<AskDJMessageResponse>(response, cancellationToken);
    }

    public async Task<AskDJHistoryResponse> ClearAskDJHistoryAsync(ClientIdentity identity, CancellationToken cancellationToken)
    {
        var payload = new
        {
            device_id = identity.DeviceId,
            device_name = identity.DeviceName,
            client_type = identity.ClientType
        };
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/ask_dj/history/clear", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<AskDJHistoryResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> RunPlaybackActionAsync(ClientIdentity identity, PlaybackAction action, CancellationToken cancellationToken)
    {
        var command = string.IsNullOrWhiteSpace(action.Command)
            ? DefaultCommandFor(action)
            : action.Command;
        var payload = new
        {
            command,
            device_id = identity.DeviceId,
            device_name = identity.DeviceName,
            client_type = identity.ClientType,
            action
        };
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/command", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, null, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, object? args, CancellationToken cancellationToken)
    {
        var payload = BuildCommandPayload(identity, command, args);
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/command", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    public static IReadOnlyDictionary<string, object?> BuildCommandPayload(ClientIdentity identity, string command, object? args = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["command"] = command,
            ["device_id"] = identity.DeviceId,
            ["device_name"] = identity.DeviceName,
            ["client_type"] = identity.ClientType
        };

        if (args is not null)
        {
            payload["args"] = args;
        }

        return payload;
    }

    private static string DefaultCommandFor(PlaybackAction action)
    {
        return string.Equals(action.Kind, "confirmation", StringComparison.OrdinalIgnoreCase)
            ? "ask_dj_followup_response"
            : "ask_dj_play_recommendation";
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("Pairing is stale or this Home Assistant token is no longer accepted.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.UpgradeRequired)
        {
            var mismatch = await response.Content.ReadFromJsonAsync<VersionMismatchError>(JsonOptions, cancellationToken);
            throw new DJConnectVersionMismatchException(mismatch?.Error, mismatch?.Message, mismatch?.HaVersion, mismatch?.HaMajorMinor);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Home Assistant returned an empty DJConnect response.");
    }

    private sealed record VersionMismatchError(
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("ha_version")] string? HaVersion,
        [property: JsonPropertyName("ha_major_minor")] string? HaMajorMinor);
}

public sealed class DJConnectVersionMismatchException : Exception
{
    public DJConnectVersionMismatchException(string? error, string? userMessage, string? haVersion, string? haMajorMinor)
        : base("DJConnect protocol version mismatch.")
    {
        Error = error;
        UserMessage = userMessage;
        HaVersion = haVersion;
        HaMajorMinor = haMajorMinor;
    }

    public string? Error { get; }
    public string? UserMessage { get; }
    public string? HaVersion { get; }
    public string? HaMajorMinor { get; }
}
