using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Resources;

namespace DJConnect.Windows.Services;

public sealed class DJConnectApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ApiRoutePrefix = "api/djconnect/v1";
    private readonly HttpClient _httpClient;
    private readonly IDJConnectWebSocketFastPath _webSocketFastPath;
    private readonly DJConnectWebSocketPayloadFactory _webSocketPayloadFactory;
    private string _homeAssistantUrl = "";
    private string? _deviceToken;
    private bool _webSocketFastPathEnabled;

    public DJConnectApiClient(HttpClient httpClient)
        : this(httpClient, new HomeAssistantWebSocketFastPath(), new DJConnectWebSocketPayloadFactory())
    {
    }

    public DJConnectApiClient(HttpClient httpClient, IDJConnectWebSocketFastPath webSocketFastPath)
        : this(httpClient, webSocketFastPath, new DJConnectWebSocketPayloadFactory())
    {
    }

    public DJConnectApiClient(
        HttpClient httpClient,
        IDJConnectWebSocketFastPath webSocketFastPath,
        DJConnectWebSocketPayloadFactory webSocketPayloadFactory)
    {
        _httpClient = httpClient;
        _webSocketFastPath = webSocketFastPath;
        _webSocketPayloadFactory = webSocketPayloadFactory;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public FastPathDiagnostics FastPathDiagnostics => _webSocketFastPath.Diagnostics;

    public void Configure(
        string homeAssistantUrl,
        string? token,
        bool enableLocalWebSocketFastPath = false,
        string? haWebSocketAuthToken = null)
    {
        Configure(new DJConnectClientConfiguration(
            homeAssistantUrl,
            token,
            enableLocalWebSocketFastPath,
            haWebSocketAuthToken));
    }

    public void Configure(DJConnectClientConfiguration configuration)
    {
        _homeAssistantUrl = configuration.HomeAssistantUrl.TrimEnd('/');
        _deviceToken = configuration.DeviceToken;
        _webSocketFastPathEnabled = configuration.EnableLocalWebSocketFastPath
            && IsLocalHttpUrl(_homeAssistantUrl)
            && !string.IsNullOrWhiteSpace(configuration.DeviceToken)
            && !string.IsNullOrWhiteSpace(configuration.HomeAssistantWebSocketAuthToken);
        _httpClient.BaseAddress = new Uri(configuration.HomeAssistantUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(configuration.DeviceToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", configuration.DeviceToken);
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Device-ID");
        if (!string.IsNullOrWhiteSpace(configuration.DeviceToken) && !string.IsNullOrWhiteSpace(configuration.DeviceId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-DJConnect-Device-ID", configuration.DeviceId);
        }

        _webSocketFastPath.Configure(_homeAssistantUrl, configuration.HomeAssistantWebSocketAuthToken, _webSocketFastPathEnabled);
    }

    public async Task<PairingResponse> PairAsync(PairingPayload payload, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Device-ID");
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Client-Type");
        _httpClient.DefaultRequestHeaders.Add("X-DJConnect-Client-Type", DJConnectContract.ClientType);
        var response = await _httpClient.PostAsJsonAsync(ApiRoute("pair"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<PairingResponse>(response, cancellationToken);
    }

    public async Task<StatusResponse> GetStatusAsync(ClientIdentity identity, CancellationToken cancellationToken)
    {
        return await GetStatusAsync(identity, null, cancellationToken);
    }

    public async Task<StatusResponse> GetStatusAsync(ClientIdentity identity, string? language, CancellationToken cancellationToken)
    {
        return await GetStatusAsync(identity, language, null, cancellationToken);
    }

    public async Task<StatusResponse> GetStatusAsync(ClientIdentity identity, string? language, int? mood, CancellationToken cancellationToken)
    {
        ApplyRequestContext(language, mood);
        var payload = BuildStatusPayload(identity, language, mood);
        var response = await _httpClient.PostAsJsonAsync(ApiRoute("status"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<StatusResponse>(response, cancellationToken);
    }

    public async Task<AskDJHistoryResponse> GetAskDJHistoryAsync(long sinceRevision, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"{ApiRoute("ask_dj/history")}?since_revision={sinceRevision}", cancellationToken);
        return await ReadJsonAsync<AskDJHistoryResponse>(response, cancellationToken);
    }

    public async Task<AskDJMessageResponse> SendAskDJMessageAsync(AskDJRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildAskDJ(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<AskDJMessageResponse>("djconnect/ask_dj/message", fastPathPayload, TimeSpan.FromSeconds(15), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("ask_dj/message"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<AskDJMessageResponse>(response, cancellationToken);
    }

    public async Task<TrackInsightResponse> GetTrackInsightAsync(TrackInsightRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildTrackInsight(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<TrackInsightResponse>("djconnect/track_insight", fastPathPayload, TimeSpan.FromSeconds(15), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("track_insight"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<TrackInsightResponse>(response, cancellationToken);
    }

    public async Task<AskDJMessageResponse> SendAskDJVoiceAsync(
        ClientIdentity identity,
        Stream wavAudio,
        AskDJVoiceRequest request,
        CancellationToken cancellationToken)
    {
        var locale = AppStrings.NormalizeApiLocale(request.Language ?? request.Locale);
        ApplyRequestContext(locale, request.Mood);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(request.ClientMessageId), "client_message_id" },
            { new StringContent(identity.DeviceId), "client_id" },
            { new StringContent(identity.DeviceId), "device_id" },
            { new StringContent(identity.DeviceName), "device_name" },
            { new StringContent(identity.ClientType), "client_type" },
            { new StringContent(request.AudioResponse), "audio_response" },
            { new StringContent(locale), "language" },
            { new StringContent(locale), "locale" }
        };
        using var audio = new StreamContent(wavAudio);
        audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audio, "audio", "ask-dj.wav");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoute("voice"))
        {
            Content = content
        };
        httpRequest.Headers.Add("X-DJConnect-Language", locale);
        httpRequest.Headers.Add("X-DJConnect-Locale", locale);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadJsonAsync<AskDJMessageResponse>(response, cancellationToken);
    }

    public async Task<AskDJHistoryResponse> ClearAskDJHistoryAsync(ClientIdentity identity, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["device_id"] = identity.DeviceId,
            ["client_id"] = identity.DeviceId,
            ["device_name"] = identity.DeviceName,
            ["client_type"] = identity.ClientType
        };
        var fastPath = await TryWebSocketAsync<AskDJHistoryResponse>(
            "djconnect/ask_dj/history/clear",
            _webSocketPayloadFactory.BuildAskDJHistoryClear(identity, _deviceToken),
            TimeSpan.FromSeconds(5),
            cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("ask_dj/history/clear"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<AskDJHistoryResponse>(response, cancellationToken);
    }

    public async Task<string> ExportAskDJHistoryAsync(ClientIdentity identity, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["identity"] = new Dictionary<string, object?>
            {
                ["device_id"] = identity.DeviceId,
                ["client_type"] = identity.ClientType,
                ["device_name"] = identity.DeviceName
            },
            ["app_version"] = DJConnectContract.AppVersion
        };
        var response = await _httpClient.PostAsJsonAsync(ApiRoute("ask_dj/history/export"), payload, JsonOptions, cancellationToken);
        return await ReadStringAsync(response, cancellationToken);
    }

    public async Task<CommandResponse> RunPlaybackActionAsync(ClientIdentity identity, PlaybackAction action, CancellationToken cancellationToken)
    {
        return await RunPlaybackActionAsync(identity, action, null, cancellationToken);
    }

    public async Task<CommandResponse> RunPlaybackActionAsync(ClientIdentity identity, PlaybackAction action, string? language, CancellationToken cancellationToken)
    {
        return await RunPlaybackActionAsync(identity, action, language, null, cancellationToken);
    }

    public async Task<CommandResponse> RunPlaybackActionAsync(ClientIdentity identity, PlaybackAction action, string? language, int? mood, CancellationToken cancellationToken)
    {
        return await RunPlaybackActionAsync(identity, action, language, mood, null, cancellationToken);
    }

    public async Task<CommandResponse> RunPlaybackActionAsync(ClientIdentity identity, PlaybackAction action, string? language, int? mood, DJAnnouncementOutput? djAnnouncementOutput, CancellationToken cancellationToken)
    {
        var command = string.IsNullOrWhiteSpace(action.Command)
            ? DefaultCommandFor(action)
            : action.Command;
        ApplyRequestContext(language, mood);
        var payload = BuildActionCommandPayload(identity, command, ActionValueFor(action), language: language, mood: mood, djAnnouncementOutput: djAnnouncementOutput);
        payload["action"] = action;
        var fastPath = await TryWebSocketAsync<CommandResponse>("djconnect/command", _webSocketPayloadFactory.BuildCommand(payload, _deviceToken), TimeSpan.FromSeconds(2), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("command"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> RunAskDJMessageActionAsync(ClientIdentity identity, PlaybackAction action, CancellationToken cancellationToken)
    {
        return await RunAskDJMessageActionAsync(identity, action, null, cancellationToken);
    }

    public async Task<CommandResponse> RunAskDJMessageActionAsync(ClientIdentity identity, PlaybackAction action, string? language, CancellationToken cancellationToken)
    {
        return await RunAskDJMessageActionAsync(identity, action, language, null, cancellationToken);
    }

    public async Task<CommandResponse> RunAskDJMessageActionAsync(ClientIdentity identity, PlaybackAction action, string? language, int? mood, CancellationToken cancellationToken)
    {
        return await RunAskDJMessageActionAsync(identity, action, language, mood, null, cancellationToken);
    }

    public async Task<CommandResponse> RunAskDJMessageActionAsync(ClientIdentity identity, PlaybackAction action, string? language, int? mood, DJAnnouncementOutput? djAnnouncementOutput, CancellationToken cancellationToken)
    {
        var prompt = ActionTextFor(action);
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            var locale = AppStrings.NormalizeApiLocale(language);
            var request = new AskDJRequest(
                Guid.NewGuid().ToString("N"),
                identity.DeviceId,
                identity.DeviceId,
                identity.DeviceName,
                identity.ClientType,
                prompt,
                Mood: mood,
                Language: locale,
                Locale: locale,
                DJAnnouncementOutput: djAnnouncementOutput ?? DJAnnouncementOutput.ClientDevice);
            var askResponse = await SendAskDJMessageAsync(request, cancellationToken);
            return new CommandResponse(askResponse.Success, askResponse.Text ?? askResponse.DjText ?? askResponse.Message, askResponse.Text ?? askResponse.DjText, askResponse.Error);
        }

        ApplyRequestContext(language, mood);
        var payload = BuildActionCommandPayload(identity, "ask_dj_message", ActionValueFor(action), language: language, mood: mood, djAnnouncementOutput: djAnnouncementOutput);
        payload["action"] = action;
        var fastPath = await TryWebSocketAsync<CommandResponse>("djconnect/command", _webSocketPayloadFactory.BuildCommand(payload, _deviceToken), TimeSpan.FromSeconds(2), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("command"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, null, null, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, string? language, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, null, language, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, object? args, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, args, null, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, object? args, string? language, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, args, language, null, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, object? args, string? language, int? mood, CancellationToken cancellationToken)
    {
        return await RunCommandAsync(identity, command, args, language, mood, null, cancellationToken);
    }

    public async Task<CommandResponse> RunCommandAsync(ClientIdentity identity, string command, object? args, string? language, int? mood, DJAnnouncementOutput? djAnnouncementOutput, CancellationToken cancellationToken)
    {
        ApplyRequestContext(language, mood);
        var payload = BuildCommandPayload(identity, command, args, language: language, mood: mood, djAnnouncementOutput: djAnnouncementOutput);
        var fastPath = await TryWebSocketAsync<CommandResponse>("djconnect/command", _webSocketPayloadFactory.BuildCommand(payload, _deviceToken), TimeSpan.FromSeconds(CommandTimeoutSeconds(command)), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("command"), payload, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    private async Task<FastPathResult<T>> TryWebSocketAsync<T>(
        string route,
        Dictionary<string, object?> payload,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!_webSocketFastPathEnabled)
        {
            return FastPathResult<T>.Miss("disabled");
        }

        try
        {
            return await _webSocketFastPath.TrySendAsync<T>(route, payload, timeout, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return FastPathResult<T>.Miss(ex.GetType().Name);
        }
    }

    private static string ApiRoute(string route)
    {
        return $"{ApiRoutePrefix}/{route.TrimStart('/')}";
    }

    public static Dictionary<string, object?> BuildCommandPayload(ClientIdentity identity, string command, object? args = null, string? clientMessageId = null, string? language = null, int? mood = null, DJAnnouncementOutput? djAnnouncementOutput = null)
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

        AddLanguage(payload, language);
        AddMood(payload, mood);
        if (djAnnouncementOutput.HasValue)
        {
            payload["dj_announcement_output"] = djAnnouncementOutput.Value;
        }

        if (args is not null)
        {
            payload["args"] = args;
        }

        return payload;
    }

    private static int CommandTimeoutSeconds(string command)
    {
        return command is "status" or "devices" or "queue" or "playlists" ? 5 : 2;
    }

    private static bool IsLocalHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = uri.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("10.", StringComparison.OrdinalIgnoreCase)
            || IsPrivate172(host);
    }

    private static bool IsPrivate172(string host)
    {
        var parts = host.Split('.');
        return parts.Length == 4
            && parts[0] == "172"
            && int.TryParse(parts[1], out var second)
            && second is >= 16 and <= 31;
    }

    public static Dictionary<string, object?> BuildStatusPayload(ClientIdentity identity, string? language = null, int? mood = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["device_id"] = identity.DeviceId,
            ["device_name"] = identity.DeviceName,
            ["client_type"] = identity.ClientType,
            ["firmware"] = "windows-app",
            ["version"] = DJConnectContract.AppVersion,
            ["app_version"] = DJConnectContract.AppVersion,
            ["protocol_version"] = DJConnectContract.ProtocolLine
        };
        AddLanguage(payload, language);
        AddMood(payload, mood);
        return payload;
    }

    public static Dictionary<string, object?> BuildActionCommandPayload(ClientIdentity identity, string command, object? value = null, string? clientMessageId = null, string? language = null, int? mood = null, DJAnnouncementOutput? djAnnouncementOutput = null)
    {
        var payload = BuildCommandPayload(identity, command, null, clientMessageId, language, mood, djAnnouncementOutput);
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

    public async Task<MusicDnaProfileResponse> GetMusicDnaProfileAsync(MusicDnaProfileRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDnaProfile(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<MusicDnaProfileResponse>("djconnect/music_dna/profile", fastPathPayload, TimeSpan.FromSeconds(5), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("music_dna/profile"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<MusicDnaProfileResponse>(response, cancellationToken);
    }

    public async Task<MusicDnaSettingsResponse> UpdateMusicDnaSettingsAsync(MusicDnaSettingsRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDnaSettings(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<MusicDnaSettingsResponse>("djconnect/music_dna/settings", fastPathPayload, TimeSpan.FromSeconds(5), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("music_dna/settings"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<MusicDnaSettingsResponse>(response, cancellationToken);
    }

    public async Task<MusicDnaClearResponse> ClearMusicDnaAsync(MusicDnaClearRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDnaClear(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<MusicDnaClearResponse>("djconnect/music_dna/clear", fastPathPayload, TimeSpan.FromSeconds(5), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("music_dna/clear"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<MusicDnaClearResponse>(response, cancellationToken);
    }

    public async Task<MusicDiscoveryResponse> GetMusicDiscoveryAsync(MusicDiscoveryRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDiscovery(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<MusicDiscoveryResponse>("djconnect/music_discovery/feed", fastPathPayload, TimeSpan.FromSeconds(5), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.GetAsync(ApiRoute("music_discovery") + MusicDiscoveryQuery(request), cancellationToken);
        return await ReadJsonAsync<MusicDiscoveryResponse>(response, cancellationToken);
    }

    public async Task<MusicDiscoveryResponse> RefreshMusicDiscoveryAsync(MusicDiscoveryRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDiscovery(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<MusicDiscoveryResponse>("djconnect/music_discovery/refresh", fastPathPayload, TimeSpan.FromSeconds(8), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("music_discovery/refresh"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<MusicDiscoveryResponse>(response, cancellationToken);
    }

    public async Task<CommandResponse> PlayMusicDiscoveryAsync(MusicDiscoveryPlayRequest request, CancellationToken cancellationToken)
    {
        ApplyRequestContext(request.Language ?? request.Locale, request.Mood);
        var fastPathPayload = _webSocketPayloadFactory.BuildMusicDiscoveryPlay(request, _deviceToken);
        var fastPath = await TryWebSocketAsync<CommandResponse>("djconnect/music_discovery/play", fastPathPayload, TimeSpan.FromSeconds(5), cancellationToken);
        if (fastPath.Success && fastPath.Value is not null)
        {
            return fastPath.Value;
        }

        var response = await _httpClient.PostAsJsonAsync(ApiRoute("music_discovery/play"), request, JsonOptions, cancellationToken);
        return await ReadJsonAsync<CommandResponse>(response, cancellationToken);
    }

    private static string MusicDiscoveryQuery(MusicDiscoveryRequest request)
    {
        var values = new Dictionary<string, string?>
        {
            ["client_id"] = request.ClientId,
            ["device_id"] = request.DeviceId,
            ["device_name"] = request.DeviceName,
            ["client_type"] = request.ClientType,
            ["language"] = request.Language,
            ["locale"] = request.Locale,
            ["mood"] = request.Mood?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["music_dna_key"] = request.MusicDnaKey
        };
        var query = string.Join("&", values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));
        return string.IsNullOrWhiteSpace(query) ? "" : "?" + query;
    }

    private static void AddLanguage(Dictionary<string, object?> payload, string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        var locale = AppStrings.NormalizeApiLocale(language);
        payload["language"] = locale;
        payload["locale"] = locale;
    }

    private static void AddMood(Dictionary<string, object?> payload, int? mood)
    {
        if (mood is >= 0 and <= 100)
        {
            payload["mood"] = mood.Value;
        }
    }

    private void ApplyRequestContext(string? language, int? mood)
    {
        _httpClient.DefaultRequestHeaders.Remove("Accept-Language");
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Language");
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Locale");
        _httpClient.DefaultRequestHeaders.Remove("X-DJConnect-Mood");

        if (!string.IsNullOrWhiteSpace(language))
        {
            var locale = AppStrings.NormalizeApiLocale(language);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", locale);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-DJConnect-Language", locale);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-DJConnect-Locale", locale);
        }

        if (mood is >= 0 and <= 100)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-DJConnect-Mood", mood.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
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

    private static async Task<string> ReadStringAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Home Assistant returned HTTP {(int)response.StatusCode}.");
        }

        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException("Home Assistant returned an empty DJConnect response.")
            : text;
    }

    private sealed record VersionMismatchError(
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("ha_version")] string? HaVersion,
        [property: JsonPropertyName("ha_major_minor")] string? HaMajorMinor);
}

public sealed record FastPathDiagnostics(
    string FastPathTransport,
    bool WebSocketConnected,
    string LastWebSocketError,
    DateTimeOffset? LastCapabilityRefresh,
    IReadOnlyList<string> WebSocketCommands);

public sealed record FastPathResult<T>(bool Success, T? Value, string? Error)
{
    public static FastPathResult<T> Hit(T value) => new(true, value, null);
    public static FastPathResult<T> Miss(string? error) => new(false, default, error);
}

public interface IDJConnectWebSocketFastPath
{
    FastPathDiagnostics Diagnostics { get; }
    void Configure(string homeAssistantUrl, string? token, bool enabled);
    Task<FastPathResult<T>> TrySendAsync<T>(
        string route,
        Dictionary<string, object?> payload,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed class HomeAssistantWebSocketFastPath : IDJConnectWebSocketFastPath, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private ClientWebSocket? _socket;
    private string _homeAssistantUrl = "";
    private string? _token;
    private bool _enabled;
    private int _nextId = 1;
    private HashSet<string> _commands = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _lastCapabilityRefresh;
    private DateTimeOffset? _unhealthyUntil;
    private string _lastError = "";

    public FastPathDiagnostics Diagnostics => new(
        _lastError.Length == 0 && IsConnected ? "websocket" : "http",
        IsConnected,
        _lastError,
        _lastCapabilityRefresh,
        _commands.Order(StringComparer.OrdinalIgnoreCase).ToArray());

    private bool IsConnected => _socket?.State == WebSocketState.Open;

    public void Configure(string homeAssistantUrl, string? token, bool enabled)
    {
        if (!string.Equals(_homeAssistantUrl, homeAssistantUrl, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_token, token, StringComparison.Ordinal))
        {
            CloseSocket();
            _commands.Clear();
            _lastCapabilityRefresh = null;
            _lastError = "";
        }

        _homeAssistantUrl = homeAssistantUrl;
        _token = token;
        _enabled = enabled;
        if (!enabled)
        {
            CloseSocket();
        }
    }

    public async Task<FastPathResult<T>> TrySendAsync<T>(
        string route,
        Dictionary<string, object?> payload,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(_homeAssistantUrl) || string.IsNullOrWhiteSpace(_token))
        {
            return FastPathResult<T>.Miss("disabled");
        }

        if (_unhealthyUntil is not null && DateTimeOffset.UtcNow < _unhealthyUntil)
        {
            return FastPathResult<T>.Miss("backoff");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            await EnsureConnectedAsync(timeoutCts.Token);
            if (!_commands.Contains(route))
            {
                return FastPathResult<T>.Miss("missing capability");
            }

            payload["id"] = _nextId++;
            payload["type"] = route;
            await SendJsonAsync(payload, timeoutCts.Token);
            using var response = await ReceiveJsonAsync(timeoutCts.Token);
            var value = ReadResult<T>(response.RootElement);
            _lastError = "";
            return value is null ? FastPathResult<T>.Miss("empty response") : FastPathResult<T>.Hit(value);
        }
        catch (Exception ex) when (ex is WebSocketException or IOException or JsonException or InvalidOperationException or TaskCanceledException)
        {
            MarkUnhealthy(ex.GetType().Name);
            return FastPathResult<T>.Miss(_lastError);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (IsConnected && _commands.Count > 0)
        {
            return;
        }

        CloseSocket();
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(WebSocketUri(_homeAssistantUrl), cancellationToken);

        using var authRequired = await ReceiveJsonAsync(cancellationToken);
        var authType = authRequired.RootElement.TryGetProperty("type", out var authTypeElement)
            ? authTypeElement.GetString()
            : "";
        if (!string.Equals(authType, "auth_required", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("unexpected websocket auth state");
        }

        await SendJsonAsync(new Dictionary<string, object?>
        {
            ["type"] = "auth",
            ["access_token"] = _token
        }, cancellationToken);

        using var authResponse = await ReceiveJsonAsync(cancellationToken);
        var responseType = authResponse.RootElement.TryGetProperty("type", out var responseTypeElement)
            ? responseTypeElement.GetString()
            : "";
        if (!string.Equals(responseType, "auth_ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("websocket auth failed");
        }

        await RefreshCapabilitiesAsync(cancellationToken);
    }

    private async Task RefreshCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var id = _nextId++;
        await SendJsonAsync(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["type"] = "djconnect/capabilities"
        }, cancellationToken);

        using var response = await ReceiveJsonAsync(cancellationToken);
        if (!IsSuccessfulResult(response.RootElement))
        {
            throw new InvalidOperationException("capability detection failed");
        }

        if (!CapabilitiesEnableWebSocket(response.RootElement))
        {
            throw new InvalidOperationException("websocket capability disabled");
        }

        var commands = ExtractCommands(response.RootElement);
        if (commands.Count == 0)
        {
            throw new InvalidOperationException("capability detection returned no commands");
        }

        _commands = new HashSet<string>(commands, StringComparer.OrdinalIgnoreCase);
        _lastCapabilityRefresh = DateTimeOffset.UtcNow;
    }

    private static bool IsSuccessfulResult(JsonElement root)
    {
        return root.TryGetProperty("success", out var success)
            && success.ValueKind == JsonValueKind.True;
    }

    private static IReadOnlyList<string> ExtractCommands(JsonElement root)
    {
        var result = root.TryGetProperty("result", out var resultElement) ? resultElement : root;
        if (!result.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return commands
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    private static bool CapabilitiesEnableWebSocket(JsonElement root)
    {
        var result = root.TryGetProperty("result", out var resultElement) ? resultElement : root;
        if (!result.TryGetProperty("websocket_supported", out var supported) || supported.ValueKind != JsonValueKind.True)
        {
            return false;
        }

        if (!result.TryGetProperty("transports", out var transports) || transports.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return transports.TryGetProperty("websocket", out var websocket)
            && websocket.ValueKind == JsonValueKind.True;
    }

    private static T? ReadResult<T>(JsonElement root)
    {
        if (!IsSuccessfulResult(root))
        {
            return default;
        }

        var result = root.TryGetProperty("result", out var resultElement) ? resultElement : root;
        return result.Deserialize<T>(JsonOptions);
    }

    private async Task SendJsonAsync(object payload, CancellationToken cancellationToken)
    {
        if (_socket is null)
        {
            throw new InvalidOperationException("websocket is not connected");
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<JsonDocument> ReceiveJsonAsync(CancellationToken cancellationToken)
    {
        if (_socket is null)
        {
            throw new InvalidOperationException("websocket is not connected");
        }

        using var stream = new MemoryStream();
        var buffer = new byte[8192];
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("websocket closed");
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private void MarkUnhealthy(string error)
    {
        _lastError = error;
        _unhealthyUntil = DateTimeOffset.UtcNow.AddSeconds(15);
        CloseSocket();
    }

    private void CloseSocket()
    {
        try
        {
            _socket?.Dispose();
        }
        catch
        {
            // Best effort: websocket failures must never affect HTTP fallback.
        }
        finally
        {
            _socket = null;
        }
    }

    private static Uri WebSocketUri(string homeAssistantUrl)
    {
        var builder = new UriBuilder(homeAssistantUrl.TrimEnd('/'))
        {
            Scheme = homeAssistantUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws",
            Path = "api/websocket"
        };
        return builder.Uri;
    }

    public void Dispose()
    {
        CloseSocket();
        _gate.Dispose();
    }
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
