using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed class DJConnectWebSocketPayloadFactory
{
    public Dictionary<string, object?> BuildAskDJ(AskDJRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(
            request.DeviceId,
            request.ClientId,
            request.DeviceName,
            request.ClientType,
            deviceToken);

        payload["client_message_id"] = request.ClientMessageId;
        payload["text"] = request.Text;
        payload["mood"] = request.Mood;
        payload["audio_response"] = request.AudioResponse;
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["music_dna_key"] = request.MusicDnaKey;

        if (request.Metadata?.TryGetValue("music_dna_key", out var musicDnaKey) == true && musicDnaKey is not null)
        {
            payload["music_dna_key"] = musicDnaKey;
        }

        return payload;
    }

    public Dictionary<string, object?> BuildTrackInsight(TrackInsightRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(
            request.DeviceId,
            request.ClientId,
            request.DeviceName,
            request.ClientType,
            deviceToken);

        payload["track"] = request.Track;
        payload["entity_id"] = request.EntityId;
        payload["player_id"] = request.PlayerId;
        payload["music_backend"] = request.MusicBackend;
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        payload["force_refresh"] = request.ForceRefresh;
        payload["include_visual_profile"] = request.IncludeVisualProfile;
        return payload;
    }

    public Dictionary<string, object?> BuildMusicDnaProfile(MusicDnaProfileRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(request.DeviceId, request.ClientId, request.DeviceName, request.ClientType, deviceToken);
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        return payload;
    }

    public Dictionary<string, object?> BuildMusicDnaSettings(MusicDnaSettingsRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(request.DeviceId, request.ClientId, request.DeviceName, request.ClientType, deviceToken);
        payload["enabled"] = request.Enabled;
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        return payload;
    }

    public Dictionary<string, object?> BuildMusicDnaClear(MusicDnaClearRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(request.DeviceId, request.ClientId, request.DeviceName, request.ClientType, deviceToken);
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        return payload;
    }

    public Dictionary<string, object?> BuildMusicDiscovery(MusicDiscoveryRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(request.DeviceId, request.ClientId, request.DeviceName, request.ClientType, deviceToken);
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        return payload;
    }

    public Dictionary<string, object?> BuildMusicDiscoveryPlay(MusicDiscoveryPlayRequest request, string? deviceToken)
    {
        var payload = BuildIdentityEnvelope(request.DeviceId, request.ClientId, request.DeviceName, request.ClientType, deviceToken);
        payload["recommendation_id"] = request.RecommendationId;
        payload["item_id"] = request.ItemId;
        payload["kind"] = request.Kind;
        payload["uri"] = request.Uri;
        payload["spotify_uri"] = request.SpotifyUri;
        payload["source"] = request.Source;
        payload["language"] = request.Language;
        payload["locale"] = request.Locale;
        payload["mood"] = request.Mood;
        payload["music_dna_key"] = request.MusicDnaKey;
        return payload;
    }

    public Dictionary<string, object?> BuildAskDJHistoryClear(ClientIdentity identity, string? deviceToken)
    {
        return BuildIdentityEnvelope(
            identity.DeviceId,
            identity.DeviceId,
            identity.DeviceName,
            identity.ClientType,
            deviceToken);
    }

    public Dictionary<string, object?> BuildCommand(Dictionary<string, object?> httpPayload, string? deviceToken)
    {
        var payload = new Dictionary<string, object?>(httpPayload)
        {
            ["device_token"] = deviceToken
        };

        if (payload.TryGetValue("args", out var args) && args is not null && !payload.ContainsKey("value"))
        {
            payload["value"] = args;
        }

        return payload;
    }

    private static Dictionary<string, object?> BuildIdentityEnvelope(
        string deviceId,
        string? clientId,
        string deviceName,
        string clientType,
        string? deviceToken)
    {
        return new Dictionary<string, object?>
        {
            ["device_id"] = deviceId,
            ["client_id"] = string.IsNullOrWhiteSpace(clientId) ? deviceId : clientId,
            ["device_name"] = deviceName,
            ["client_type"] = clientType,
            ["device_token"] = deviceToken
        };
    }
}
