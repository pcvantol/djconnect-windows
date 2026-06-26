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
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
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
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/pair", payload, JsonOptions, cancellationToken);
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

    public async Task<AskDJMessageResponse> SendAskDJVoiceAsync(
        ClientIdentity identity,
        Stream wavAudio,
        AskDJVoiceRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(request.ClientMessageId), "client_message_id" },
            { new StringContent(identity.DeviceId), "client_id" },
            { new StringContent(identity.DeviceId), "device_id" },
            { new StringContent(identity.DeviceName), "device_name" },
            { new StringContent(identity.ClientType), "client_type" },
            { new StringContent(request.AudioResponse), "audio_response" }
        };
        using var audio = new StreamContent(wavAudio);
        audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audio, "audio", "ask-dj.wav");

        var response = await _httpClient.PostAsync("api/djconnect/voice", content, cancellationToken);
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
        var payload = BuildActionCommandPayload(identity, command, ActionValueFor(action));
        payload["action"] = action;
        var response = await _httpClient.PostAsJsonAsync("api/djconnect/command", payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> RunAskDJMessageActionAsync(ClientIdentity identity, PlaybackAction action, CancellationToken cancellationToken)
    {
        var prompt = ActionTextFor(action);
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            var request = new AskDJRequest(
                Guid.NewGuid().ToString("N"),
                identity.DeviceId,
                identity.DeviceId,
                identity.DeviceName,
                identity.ClientType,
                prompt,
                prompt);
            var askResponse = await SendAskDJMessageAsync(request, cancellationToken);
            return new CommandResponse(askResponse.Success, askResponse.Text ?? askResponse.DjText ?? askResponse.Message, askResponse.Text ?? askResponse.DjText, askResponse.Error);
        }

        var payload = BuildActionCommandPayload(identity, "ask_dj_message", ActionValueFor(action));
        payload["action"] = action;
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

    public static Dictionary<string, object?> BuildCommandPayload(ClientIdentity identity, string command, object? args = null, string? clientMessageId = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["command"] = command,
            ["client_message_id"] = string.IsNullOrWhiteSpace(clientMessageId) ? Guid.NewGuid().ToString("N") : clientMessageId,
            ["client_id"] = identity.DeviceId,
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

    public static Dictionary<string, object?> BuildActionCommandPayload(ClientIdentity identity, string command, object? value = null, string? clientMessageId = null)
    {
        var payload = BuildCommandPayload(identity, command, null, clientMessageId);
        if (value is not null)
        {
            payload["value"] = value;
        }

        if (value is PlaybackAction action && action.MusicBackendRevision.HasValue)
        {
            payload["music_backend_revision"] = action.MusicBackendRevision.Value;
        }

        return payload;
    }

    private static string DefaultCommandFor(PlaybackAction action)
    {
        if (action.IsConfirmation)
        {
            return "ask_dj_followup_response";
        }

        if (action.IsPlayNowRecommendation)
        {
            return "ask_dj_play_recommendation";
        }

        return string.IsNullOrWhiteSpace(action.Command) ? "ask_dj_play_recommendation" : action.Command;
    }

    private static object? ActionValueFor(PlaybackAction action)
    {
        return action.IsConfirmation || action.IsPlayNowRecommendation
            ? action
            : action.Value ?? action;
    }

    private static string? ActionTextFor(PlaybackAction action)
    {
        if (action.Value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }

                if (element.TryGetProperty("prompt", out var prompt) && prompt.ValueKind == JsonValueKind.String)
                {
                    return prompt.GetString();
                }
            }

            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }

        return action.Value?.ToString();
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
