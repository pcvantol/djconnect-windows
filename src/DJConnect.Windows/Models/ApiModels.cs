using System.Text.Json;
using System.Text.Json.Serialization;
using DJConnect.Windows.Services;

namespace DJConnect.Windows.Models;

public sealed record PairingPayload(
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("pair_code")] string PairCode,
    [property: JsonPropertyName("app_version")] string AppVersion,
    [property: JsonPropertyName("platform")] string Platform = "windows",
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("build")] string? Build = null);

public sealed record PairingResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("client_type")] string? ClientType,
    [property: JsonPropertyName("device_token")] string? DeviceToken,
    [property: JsonPropertyName("device_id")] string? DeviceId,
    [property: JsonPropertyName("ha_pairing_status")] string? PairingStatus,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("ha_local_url")] string? HomeAssistantLocalUrl = null,
    [property: JsonPropertyName("ha_remote_url")] string? HomeAssistantRemoteUrl = null,
    [property: JsonPropertyName("api_base")] string? ApiBase = null,
    [property: JsonPropertyName("voice_path")] string? VoicePath = null,
    [property: JsonPropertyName("status_path")] string? StatusPath = null,
    [property: JsonPropertyName("event_path")] string? EventPath = null,
    [property: JsonPropertyName("ask_dj_supported")] bool? AskDJSupported = null,
    [property: JsonPropertyName("ask_dj_voice_supported")] bool? AskDJVoiceSupported = null,
    [property: JsonPropertyName("ask_dj_audio_response_supported")] bool? AskDJAudioResponseSupported = null,
    [property: JsonPropertyName("remote_supported")] bool? RemoteSupported = null,
    [property: JsonPropertyName("music_backend")] string? MusicBackend = null,
    [property: JsonPropertyName("music_backend_name")] string? MusicBackendName = null,
    [property: JsonPropertyName("music_backend_available")] bool? MusicBackendAvailable = null,
    [property: JsonPropertyName("music_backend_revision")] int? MusicBackendRevision = null,
    [property: JsonPropertyName("music_backend_capabilities")] MusicBackendCapabilities? MusicBackendCapabilities = null,
    [property: JsonPropertyName("music_target_player")] MusicTargetPlayer? MusicTargetPlayer = null,
    [property: JsonPropertyName("music_backend_error")] MusicBackendError? MusicBackendError = null);

public sealed record MusicBackendCapabilities(
    [property: JsonPropertyName("supports_search")] bool? SupportsSearch,
    [property: JsonPropertyName("supports_queue")] bool? SupportsQueue,
    [property: JsonPropertyName("supports_outputs")] bool? SupportsOutputs,
    [property: JsonPropertyName("supports_favorites")] bool? SupportsFavorites,
    [property: JsonPropertyName("supports_recently_played")] bool? SupportsRecentlyPlayed,
    [property: JsonPropertyName("supports_top_items")] bool? SupportsTopItems)
{
    public string CompactSummary => string.Join(", ", new[]
    {
        Capability("search", SupportsSearch),
        Capability("queue", SupportsQueue),
        Capability("outputs", SupportsOutputs),
        Capability("favorites", SupportsFavorites),
        Capability("recent", SupportsRecentlyPlayed),
        Capability("top", SupportsTopItems)
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string Capability(string label, bool? supported) => supported == true ? label : "";
}

public sealed record MusicTargetPlayer(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name);

[JsonConverter(typeof(MusicBackendErrorConverter))]
public sealed record MusicBackendError(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message)
{
    public string DisplayText => string.IsNullOrWhiteSpace(Message) ? Code ?? "" : Message;
}

public sealed class MusicBackendErrorConverter : JsonConverter<MusicBackendError>
{
    public override MusicBackendError? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringMessage = reader.GetString();
            return string.IsNullOrWhiteSpace(stringMessage) ? null : new MusicBackendError(null, stringMessage);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new MusicBackendError(null, root.GetRawText());
        }

        var code = root.TryGetProperty("code", out var codeElement) && codeElement.ValueKind == JsonValueKind.String
            ? codeElement.GetString()
            : null;
        var objectMessage = root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String
            ? messageElement.GetString()
            : null;
        return new MusicBackendError(code, objectMessage);
    }

    public override void Write(Utf8JsonWriter writer, MusicBackendError value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(value.Code))
        {
            writer.WriteString("code", value.Code);
        }

        if (!string.IsNullOrWhiteSpace(value.Message))
        {
            writer.WriteString("message", value.Message);
        }

        writer.WriteEndObject();
    }
}

public sealed record MusicBackendSummary(
    string? Backend,
    string? Name,
    bool? Available,
    int? Revision,
    MusicBackendCapabilities? Capabilities,
    MusicTargetPlayer? TargetPlayer,
    MusicBackendError? Error)
{
    public static readonly MusicBackendSummary Empty = new(null, null, null, null, null, null, null);
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Backend ?? "unknown" : Name;
    public string AvailabilityText => Available == false ? "unavailable" : Available == true ? "available" : "unknown";
    public string ErrorText => Error?.DisplayText ?? "";
    public bool IsUnavailable => Available == false || !string.IsNullOrWhiteSpace(ErrorText);
}

public sealed record AskDJRequest(
    [property: JsonPropertyName("client_message_id")] string ClientMessageId,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, object?>? Metadata = null,
    [property: JsonPropertyName("audio_response")] string AudioResponse = "auto",
    [property: JsonPropertyName("mood")] int? Mood = null,
    [property: JsonPropertyName("app_version")] string? AppVersion = null,
    [property: JsonPropertyName("protocol_version")] string? ProtocolVersion = null,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("locale")] string? Locale = null);

public sealed record AskDJHistoryResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("history_revision")] long HistoryRevision,
    [property: JsonPropertyName("clear_revision")] long ClearRevision,
    [property: JsonPropertyName("cleared")] bool? Cleared,
    [property: JsonPropertyName("ask_dj_clear_required")] bool? AskDJClearRequired,
    [property: JsonPropertyName("history_limit")] int? HistoryLimit,
    [property: JsonPropertyName("history_trimmed_before")] DateTimeOffset? HistoryTrimmedBefore,
    [property: JsonPropertyName("history_trimmed_count")] int? HistoryTrimmedCount,
    [property: JsonPropertyName("messages")] IReadOnlyList<AskDJMessage> Messages,
    [property: JsonPropertyName("error")] string? Error = null)
{
    public bool RequiresLocalClearAfterClearResponse(long localClearRevision)
    {
        return Success
            || Cleared == true
            || AskDJClearRequired == true
            || ClearRevision > localClearRevision;
    }

    public bool RequiresLocalClearBeforeHistoryMerge(long localClearRevision)
    {
        return Cleared == true
            || AskDJClearRequired == true
            || ClearRevision > localClearRevision;
    }
}

public sealed record AskDJMessage(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt,
    [property: JsonPropertyName("message_kind")] string? MessageKind,
    [property: JsonPropertyName("playback_actions")] IReadOnlyList<PlaybackAction>? PlaybackActions,
    [property: JsonPropertyName("confirmation_actions")] IReadOnlyList<PlaybackAction>? ConfirmationActions,
    [property: JsonPropertyName("items")] IReadOnlyList<RecentItem>? Items,
    [property: JsonPropertyName("images")] IReadOnlyList<AskDJImage>? Images,
    [property: JsonPropertyName("sources")] IReadOnlyList<AskDJSource>? Sources,
    [property: JsonPropertyName("audio_url")] string? AudioUrl = null,
    [property: JsonPropertyName("origin")] string? Origin = null,
    [property: JsonPropertyName("client_message_id")] string? ClientMessageId = null,
    [property: JsonPropertyName("exchange_id")] string? ExchangeId = null,
    [property: JsonPropertyName("exchange_order")] int? ExchangeOrder = null,
    [property: JsonPropertyName("history_revision")] long? HistoryRevision = null,
    [property: JsonIgnore] int? ServerOrder = null,
    [property: JsonIgnore] bool IsPending = false,
    [property: JsonIgnore] bool IsFailed = false,
    [property: JsonPropertyName("intent")] AskDJIntent? Intent = null,
    [property: JsonPropertyName("action")] string? Action = null,
    [property: JsonPropertyName("type")] string? Type = null,
    [property: JsonPropertyName("open_screen")] string? OpenScreen = null,
    [property: JsonPropertyName("track_insight")] TrackInsightResult? TrackInsightData = null,
    [property: JsonPropertyName("links")] IReadOnlyList<AskDJSource>? Links = null)
{
    public string DisplayText => Text ?? Message ?? "";
    public bool IsUser => string.Equals(Role, "user", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => !IsUser && !IsSystem;
    public bool IsSystem => string.Equals(MessageKind, "system", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "system", StringComparison.OrdinalIgnoreCase);
    public bool HasAudio => !string.IsNullOrWhiteSpace(AudioUrl);
    public bool HasPlaybackActions => (PlaybackActions?.Count ?? 0) > 0 || (ConfirmationActions?.Count ?? 0) > 0;
    public bool HasItems => (Items?.Count ?? 0) > 0;
    public bool HasImages => (Images?.Count ?? 0) > 0;
    public bool HasSources => (Sources?.Count ?? 0) > 0 || (Links?.Count ?? 0) > 0;
    public IReadOnlyList<AskDJSource> DisplaySources => AskDJSourceCollection.SourcesAndLinks(Sources, Links);
    public bool IsTrackInsight => string.Equals(Intent?.Intent, "track_insight", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Action, "track_insight", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Type, "track_insight", StringComparison.OrdinalIgnoreCase)
        || string.Equals(OpenScreen, "track_insight", StringComparison.OrdinalIgnoreCase);
    public TrackInsightPresentation? TrackInsight => TrackInsightPresentation.From(TrackInsightData, this);
    public bool HasTrackInsight => TrackInsight?.HasContent == true;
    public string BubbleAlignment => IsUser ? "End" : "Start";
    public string BubbleBackground => IsSystem ? "#24304D" : IsUser ? "#5539D7" : "#222852";
    public string RoleLabel => IsSystem ? (Origin ?? "system") : IsUser ? "Jij" : "Ask DJ";
    public string DeliveryLabel => IsFailed ? "Niet verzonden" : IsPending ? "Verzenden..." : "";
    public string StableKey => StableMessageKey(this);

    public static string StableMessageKey(AskDJMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Id))
        {
            return message.Id;
        }

        if (!string.IsNullOrWhiteSpace(message.ClientMessageId))
        {
            return $"{message.ClientMessageId}|{NormalizeRole(message.Role)}";
        }

        return "";
    }

    public static string NormalizeRole(string? role)
    {
        return string.Equals(role, "user", StringComparison.OrdinalIgnoreCase) ? "user"
            : string.Equals(role, "system", StringComparison.OrdinalIgnoreCase) ? "system"
            : "assistant";
    }
}

public sealed record AskDJMessageResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("user_message")] AskDJMessage? UserMessage,
    [property: JsonPropertyName("assistant_message")] AskDJMessage? AssistantMessage,
    [property: JsonPropertyName("messages")] IReadOnlyList<AskDJMessage>? Messages,
    [property: JsonPropertyName("history_revision")] long? HistoryRevision,
    [property: JsonPropertyName("clear_revision")] long? ClearRevision,
    [property: JsonPropertyName("playback_actions")] IReadOnlyList<PlaybackAction>? PlaybackActions,
    [property: JsonPropertyName("confirmation_actions")] IReadOnlyList<PlaybackAction>? ConfirmationActions,
    [property: JsonPropertyName("items")] IReadOnlyList<RecentItem>? Items,
    [property: JsonPropertyName("images")] IReadOnlyList<AskDJImage>? Images,
    [property: JsonPropertyName("sources")] IReadOnlyList<AskDJSource>? Sources,
    [property: JsonPropertyName("intent")] AskDJIntent? Intent,
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("open_screen")] string? OpenScreen,
    [property: JsonPropertyName("track_insight")] TrackInsightResult? TrackInsightData,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("dj_text")] string? DjText,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("audio_url")] string? AudioUrl,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("ha_local_url")] string? HomeAssistantLocalUrl = null,
    [property: JsonPropertyName("ha_remote_url")] string? HomeAssistantRemoteUrl = null,
    [property: JsonPropertyName("remote_supported")] bool? RemoteSupported = null,
    [property: JsonPropertyName("music_backend")] string? MusicBackend = null,
    [property: JsonPropertyName("music_backend_name")] string? MusicBackendName = null,
    [property: JsonPropertyName("music_backend_available")] bool? MusicBackendAvailable = null,
    [property: JsonPropertyName("music_backend_revision")] int? MusicBackendRevision = null,
    [property: JsonPropertyName("music_backend_capabilities")] MusicBackendCapabilities? MusicBackendCapabilities = null,
    [property: JsonPropertyName("music_target_player")] MusicTargetPlayer? MusicTargetPlayer = null,
    [property: JsonPropertyName("music_backend_error")] MusicBackendError? MusicBackendError = null,
    [property: JsonPropertyName("links")] IReadOnlyList<AskDJSource>? Links = null);

public sealed record TrackInsightRequest(
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("track")] TrackInsightRequestTrack? Track,
    [property: JsonPropertyName("entity_id")] string? EntityId = null,
    [property: JsonPropertyName("player_id")] string? PlayerId = null,
    [property: JsonPropertyName("music_backend")] string? MusicBackend = null,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("mood")] int? Mood = null,
    [property: JsonPropertyName("force_refresh")] bool ForceRefresh = false,
    [property: JsonPropertyName("include_visual_profile")] bool IncludeVisualProfile = true,
    [property: JsonPropertyName("client_id")] string? ClientId = null);

public sealed record TrackInsightResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("track_insight")] TrackInsightResult? TrackInsight,
    [property: JsonPropertyName("track")] TrackInsightTrack? Track = null,
    [property: JsonPropertyName("analysis")] TrackInsightAnalysis? Analysis = null,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("message")] string? Message = null)
{
    public TrackInsightResult? ResolvedTrackInsight => TrackInsight ?? (Track is not null || Analysis is not null
        ? new TrackInsightResult(Track, null, null, null, null, Analysis, null, null, null, null, null, null, null, null, null)
        : null);
}

public sealed record TrackInsightRequestTrack(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("artwork_url")] string? ArtworkUrl = null,
    [property: JsonPropertyName("uri")] string? Uri = null,
    [property: JsonPropertyName("genres")] IReadOnlyList<string>? Genres = null);

public sealed record AskDJIntent(
    [property: JsonPropertyName("intent")] string? Intent,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("confidence")] string? Confidence = null,
    [property: JsonPropertyName("source")] string? Source = null);

public sealed record TrackInsightResult(
    [property: JsonPropertyName("track")] TrackInsightTrack? Track,
    [property: JsonPropertyName("contract_version")] int? ContractVersion,
    [property: JsonPropertyName("mode")] string? Mode,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence,
    [property: JsonPropertyName("analysis")] TrackInsightAnalysis? Analysis,
    [property: JsonPropertyName("sections")] IReadOnlyList<TrackInsightSection>? Sections,
    [property: JsonPropertyName("timeline")] IReadOnlyList<TrackInsightTimelineEntry>? Timeline,
    [property: JsonPropertyName("dj_tips")] IReadOnlyList<TrackInsightTip>? DjTips,
    [property: JsonPropertyName("providers")] IReadOnlyList<TrackInsightProviderStatus>? Providers,
    [property: JsonPropertyName("metadata")] TrackInsightMetadata? Metadata,
    [property: JsonPropertyName("music_dna")] TrackInsightMusicDna? MusicDna,
    [property: JsonPropertyName("visual_profile")] TrackInsightVisualProfile? VisualProfile,
    [property: JsonPropertyName("cache")] TrackInsightCache? Cache,
    [property: JsonPropertyName("limitations")] IReadOnlyList<TrackInsightLimitation>? Limitations);

public sealed record TrackInsightTrack(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("artwork_url")] string? ArtworkUrl = null,
    [property: JsonPropertyName("uri")] string? Uri = null,
    [property: JsonPropertyName("genres")] IReadOnlyList<string>? Genres = null);

public sealed record TrackInsightAnalysis(
    [property: JsonPropertyName("sections")] IReadOnlyList<TrackInsightSection>? Sections,
    [property: JsonPropertyName("timeline")] IReadOnlyList<TrackInsightTimelineEntry>? Timeline,
    [property: JsonPropertyName("dj_tips")] IReadOnlyList<TrackInsightTip>? DjTips,
    [property: JsonPropertyName("limitations")] IReadOnlyList<TrackInsightLimitation>? Limitations,
    [property: JsonPropertyName("providers")] IReadOnlyList<TrackInsightProviderStatus>? Providers,
    [property: JsonPropertyName("summary")] string? Summary = null,
    [property: JsonPropertyName("full_text")] string? FullText = null,
    [property: JsonPropertyName("genre")] string? Genre = null,
    [property: JsonPropertyName("subgenre")] string? Subgenre = null,
    [property: JsonPropertyName("mood")] string? Mood = null,
    [property: JsonPropertyName("vibe")] string? Vibe = null,
    [property: JsonPropertyName("texture")] string? Texture = null,
    [property: JsonPropertyName("emotional_tone")] string? EmotionalTone = null,
    [property: JsonPropertyName("energy")] JsonElement? Energy = null,
    [property: JsonPropertyName("danceability")] JsonElement? Danceability = null,
    [property: JsonPropertyName("intensity")] JsonElement? Intensity = null,
    [property: JsonPropertyName("confidence")] string? Confidence = null,
    [property: JsonPropertyName("production_notes")] IReadOnlyList<string>? ProductionNotes = null,
    [property: JsonPropertyName("instrumentation")] IReadOnlyList<string>? Instrumentation = null,
    [property: JsonPropertyName("arrangement_notes")] IReadOnlyList<string>? ArrangementNotes = null,
    [property: JsonPropertyName("listening_cues")] IReadOnlyList<string>? ListeningCues = null,
    [property: JsonPropertyName("similar_tracks")] IReadOnlyList<TrackInsightSimilarTrack>? SimilarTracks = null);

public sealed record TrackInsightSimilarTrack(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("album")] string? Album = null);

public sealed record MusicDnaProfileRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("mood")] int? Mood = null,
    [property: JsonPropertyName("music_dna_key")] string? MusicDnaKey = null);

public sealed record MusicDnaSettingsRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("mood")] int? Mood = null);

public sealed record MusicDnaClearRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("language")] string? Language = null,
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("mood")] int? Mood = null);

public sealed record MusicDnaProfileResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("enabled")] bool? Enabled,
    [property: JsonPropertyName("profile")] MusicDnaProfile? Profile,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("message")] string? Message = null);

public sealed record MusicDnaSettingsResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("enabled")] bool? Enabled,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("message")] string? Message = null);

public sealed record MusicDnaClearResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("enabled")] bool? Enabled,
    [property: JsonPropertyName("error")] string? Error = null,
    [property: JsonPropertyName("message")] string? Message = null);

public sealed record MusicDnaProfile(
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("favorite_genres")] IReadOnlyList<MusicDnaProfileItem>? FavoriteGenres,
    [property: JsonPropertyName("favorite_artists")] IReadOnlyList<MusicDnaProfileItem>? FavoriteArtists,
    [property: JsonPropertyName("recent_tracks")] IReadOnlyList<MusicDnaProfileItem>? RecentTracks,
    [property: JsonPropertyName("energy_profile")] MusicDnaProfileItem? EnergyProfile,
    [property: JsonPropertyName("mood_profile")] MusicDnaProfileItem? MoodProfile,
    [property: JsonPropertyName("taste_direction")] MusicDnaProfileItem? TasteDirection,
    [property: JsonPropertyName("based_on")] string? BasedOn,
    [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt);

[JsonConverter(typeof(MusicDnaProfileItemJsonConverter))]
public sealed record MusicDnaProfileItem(
    string? Name,
    string? Title,
    string? Artist,
    int? Count,
    double? Score,
    IReadOnlyList<string>? Genres)
{
    public string DisplayTitle => FirstNonEmpty(Name, Title, Artist);
    public string DisplaySubtitle => FirstNonEmpty(Artist, Count.HasValue ? Count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
    public bool HasContent => !string.IsNullOrWhiteSpace(DisplayTitle) || (Genres?.Count ?? 0) > 0;

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public sealed class MusicDnaProfileItemJsonConverter : JsonConverter<MusicDnaProfileItem>
{
    public override MusicDnaProfileItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new MusicDnaProfileItem(reader.GetString(), null, null, null, null, null);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return new MusicDnaProfileItem(
            ReadString(root, "name"),
            ReadString(root, "title"),
            ReadString(root, "artist"),
            ReadInt(root, "count"),
            ReadDouble(root, "score"),
            ReadStringArray(root, "genres"));
    }

    public override void Write(Utf8JsonWriter writer, MusicDnaProfileItem value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new
        {
            value.Name,
            value.Title,
            value.Artist,
            value.Count,
            value.Score,
            value.Genres
        }, options);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : null;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result) ? result : null;
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }
}

public sealed record TrackInsightMusicDna(
    [property: JsonPropertyName("match_percent")] int? MatchPercent,
    [property: JsonPropertyName("why_it_fits")] string? WhyItFits,
    [property: JsonPropertyName("summary")] string? Summary);

public sealed record TrackInsightVisualProfile(
    [property: JsonPropertyName("vibe")] string? Vibe,
    [property: JsonPropertyName("palette")] IReadOnlyList<string>? Palette,
    [property: JsonPropertyName("motion")] string? Motion);

public sealed record TrackInsightCache(
    [property: JsonPropertyName("hit")] bool? Hit,
    [property: JsonPropertyName("generated_at")] DateTimeOffset? GeneratedAt);

public sealed record TrackInsightMetadata(
    [property: JsonPropertyName("musicbrainz_recording_id")] string? MusicBrainzRecordingId,
    [property: JsonPropertyName("match_score")] int? MatchScore,
    [property: JsonPropertyName("recording_title")] string? RecordingTitle,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("first_release_date")] string? FirstReleaseDate,
    [property: JsonPropertyName("release")] TrackInsightMetadataRelease? Release,
    [property: JsonPropertyName("genres")] IReadOnlyList<string>? Genres,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("listenbrainz_listen_count")] int? ListenBrainzListenCount);

public sealed record TrackInsightMetadataRelease(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("status")] string? Status);

public sealed record TrackInsightProviderStatus(
    [property: JsonPropertyName("provider_id")] string? ProviderId,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("requires_config")] bool? RequiresConfig,
    [property: JsonPropertyName("reason")] string? Reason);

public sealed record TrackInsightSection(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("value")] JsonElement? Value,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence,
    [property: JsonPropertyName("items")] IReadOnlyList<TrackInsightMetric>? Items);

public sealed record TrackInsightMetric(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("value")] JsonElement? Value,
    [property: JsonPropertyName("unit")] string? Unit,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence);

public sealed record TrackInsightTimelineEntry(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("start")] string? Start,
    [property: JsonPropertyName("end")] string? End,
    [property: JsonPropertyName("start_time")] string? StartTime,
    [property: JsonPropertyName("end_time")] string? EndTime,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence);

public sealed record TrackInsightTip(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence);

public sealed record TrackInsightLimitation(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("confidence")] string? Confidence);

public sealed record TrackInsightPresentation(
    string Header,
    string MetaLabel,
    IReadOnlyList<TrackInsightRow> Sections,
    IReadOnlyList<TrackInsightRow> Context,
    IReadOnlyList<TrackInsightRow> Timeline,
    IReadOnlyList<TrackInsightRow> Tips,
    IReadOnlyList<TrackInsightRow> Limitations,
    IReadOnlyList<TrackInsightRow> ProviderDiagnostics)
{
    public bool HasSections => Sections.Count > 0;
    public bool HasContext => Context.Count > 0;
    public bool HasTimeline => Timeline.Count > 0;
    public bool HasTips => Tips.Count > 0;
    public bool HasLimitations => Limitations.Count > 0;
    public bool HasProviderDiagnostics => ProviderDiagnostics.Count > 0;
    public bool HasContent => HasSections || HasContext || HasTimeline || HasTips || HasLimitations || HasProviderDiagnostics;
    public bool IsUnavailable => Header.Contains("niet beschikbaar", StringComparison.OrdinalIgnoreCase);

    public static TrackInsightPresentation? From(TrackInsightResult? insight, AskDJMessage? message = null)
    {
        if (insight is null || (message is not null && !message.IsTrackInsight))
        {
            return null;
        }

        var analysis = insight;
        var detail = insight.Analysis;
        var meta = JoinNonEmpty(
            analysis.ContractVersion.HasValue ? $"contract v{analysis.ContractVersion}" : null,
            LabelWithPrefix("bron", analysis.Source),
            LabelWithPrefix("confidence", analysis.Confidence));

        var sections = new List<TrackInsightRow>();
        var context = new List<TrackInsightRow>();
        var timeline = new List<TrackInsightRow>();
        var tips = new List<TrackInsightRow>();
        var limitations = new List<TrackInsightRow>();
        var providerDiagnostics = new List<TrackInsightRow>();

        var analysisSections = detail?.Sections ?? analysis.Sections;
        var analysisTimeline = detail?.Timeline ?? analysis.Timeline;
        var analysisTips = detail?.DjTips ?? analysis.DjTips;
        var analysisLimitations = detail?.Limitations ?? analysis.Limitations;
        var analysisProviders = detail?.Providers ?? analysis.Providers;

        var trackTitle = JoinNonEmpty(analysis.Track?.Title, analysis.Track?.Artist);
        if (!string.IsNullOrWhiteSpace(trackTitle))
        {
            sections.Add(new TrackInsightRow("Track", analysis.Track?.Album ?? "", trackTitle, ""));
        }

        if (analysis.MusicDna is not null)
        {
            var match = analysis.MusicDna.MatchPercent.HasValue ? $"{analysis.MusicDna.MatchPercent.Value}%" : "";
            context.Add(new TrackInsightRow("Music DNA Match", "Music DNA", match, ""));
            if (!string.IsNullOrWhiteSpace(analysis.MusicDna.WhyItFits ?? analysis.MusicDna.Summary))
            {
                context.Add(new TrackInsightRow("Why it fits you", "Music DNA", analysis.MusicDna.WhyItFits ?? analysis.MusicDna.Summary ?? "", ""));
            }
        }

        if (analysis.VisualProfile is not null)
        {
            var visual = JoinNonEmpty(
                LabelWithPrefix("Vibe", analysis.VisualProfile.Vibe),
                LabelWithPrefix("Motion", analysis.VisualProfile.Motion),
                LabelWithPrefix("Palette", JoinNonEmpty(analysis.VisualProfile.Palette?.ToArray() ?? [])));
            if (!string.IsNullOrWhiteSpace(visual))
            {
                context.Add(new TrackInsightRow("Vibe", "Rendering hints", visual, ""));
            }
        }

        if (detail is not null)
        {
            AddIfPresent(sections, "Summary", "Analysis", FirstNonEmpty(detail.Summary, detail.FullText), SourceConfidenceLabel(null, detail.Confidence));
            AddIfPresent(sections, "Genre", FirstNonEmpty(detail.Subgenre, "Genre"), FirstNonEmpty(detail.Genre, JoinNonEmpty(analysis.Track?.Genres?.ToArray() ?? [])), "");
            AddIfPresent(sections, "Mood", "Vibe", JoinNonEmpty(
                LabelWithPrefix("Mood", detail.Mood),
                LabelWithPrefix("Vibe", detail.Vibe),
                LabelWithPrefix("Texture", detail.Texture),
                LabelWithPrefix("Tone", detail.EmotionalTone)), "");
            AddIfPresent(sections, "Energy", "Feel", JoinNonEmpty(
                LabelWithPrefix("Energy", DisplayJson(detail.Energy)),
                LabelWithPrefix("Danceability", DisplayJson(detail.Danceability)),
                LabelWithPrefix("Intensity", DisplayJson(detail.Intensity))), "");
            AddListRow(sections, "Production notes", "Production", detail.ProductionNotes);
            AddListRow(sections, "Instrumentation", "Instrumentation", detail.Instrumentation);
            AddListRow(sections, "Arrangement notes", "Arrangement", detail.ArrangementNotes);
            AddListRow(sections, "Listening cues", "Cues", detail.ListeningCues);
            AddIfPresent(sections, "Similar tracks", "References", SimilarTracksLabel(detail.SimilarTracks), "");
        }

        if (analysis.Cache is not null)
        {
            context.Add(new TrackInsightRow("Cache", analysis.Cache.Hit == true ? "hit" : "fresh", analysis.Cache.GeneratedAt?.ToString("u") ?? "", ""));
        }

        sections.AddRange((analysisSections ?? []).Where(section => !IsMetadataContextSection(section) && !IsForbiddenMusicalMeasurement(section)).Select(SectionRow));
        context.AddRange((analysisSections ?? []).Where(IsMetadataContextSection).Select(MetadataContextSectionRow));
        timeline.AddRange((analysisTimeline ?? []).Select(TimelineRow));
        tips.AddRange((analysisTips ?? []).Select(TipRow));
        limitations.AddRange((analysisLimitations ?? []).Select(LimitationRow));

        if (analysis.Metadata is not null)
        {
            context.AddRange(MetadataRows(analysis.Metadata, analysis.Source, analysis.Confidence));
        }

        var header = string.Equals(analysis.Mode, "unavailable", StringComparison.OrdinalIgnoreCase)
            ? "Track Insight niet beschikbaar"
            : "Track Insight";

        providerDiagnostics.AddRange((analysisProviders ?? []).Select(ProviderRow));

        return new TrackInsightPresentation(header, meta, sections, context, timeline, tips, limitations, providerDiagnostics);
    }

    private static TrackInsightRow SectionRow(TrackInsightSection section)
    {
        var detail = FirstNonEmpty(section.Summary, section.Text, DisplayJson(section.Value));
        if (string.IsNullOrWhiteSpace(detail) && (section.Items?.Count ?? 0) > 0)
        {
            detail = string.Join(" · ", section.Items!
                .Where(item => !IsForbiddenMusicalMeasurement(item))
                .Select(item => $"{FirstNonEmpty(item.Label, item.Title, item.Id, item.Kind)} {DisplayJson(item.Value)} {item.Unit}".Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return new TrackInsightRow(
            FirstNonEmpty(section.Title, section.Label, Humanize(section.Id), Humanize(section.Kind), "Analyse"),
            FirstNonEmpty(Humanize(section.Kind), section.Id),
            detail,
            SourceConfidenceLabel(section.Source, section.Confidence));
    }

    private static bool IsForbiddenMusicalMeasurement(TrackInsightSection section)
    {
        return IsForbiddenMusicalMeasurement(section.Id)
            || IsForbiddenMusicalMeasurement(section.Title)
            || IsForbiddenMusicalMeasurement(section.Label)
            || IsForbiddenMusicalMeasurement(section.Kind);
    }

    private static bool IsForbiddenMusicalMeasurement(TrackInsightMetric metric)
    {
        return IsForbiddenMusicalMeasurement(metric.Id)
            || IsForbiddenMusicalMeasurement(metric.Title)
            || IsForbiddenMusicalMeasurement(metric.Label)
            || IsForbiddenMusicalMeasurement(metric.Kind);
    }

    private static bool IsForbiddenMusicalMeasurement(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Replace("-", "_", StringComparison.Ordinal).Replace(" ", "_", StringComparison.Ordinal).ToLowerInvariant();
        return normalized.Contains("bpm", StringComparison.Ordinal)
            || normalized.Contains("tempo", StringComparison.Ordinal)
            || normalized is "key" or "musical_key" or "key_signature"
            || normalized.Contains("_key", StringComparison.Ordinal)
            || normalized.Contains("key_", StringComparison.Ordinal);
    }

    private static void AddIfPresent(List<TrackInsightRow> rows, string title, string category, string detail, string meta)
    {
        if (!string.IsNullOrWhiteSpace(detail))
        {
            rows.Add(new TrackInsightRow(title, category, detail, meta));
        }
    }

    private static void AddListRow(List<TrackInsightRow> rows, string title, string category, IReadOnlyList<string>? values)
    {
        var detail = JoinNonEmpty(values?.ToArray() ?? []);
        AddIfPresent(rows, title, category, detail, "");
    }

    private static string SimilarTracksLabel(IReadOnlyList<TrackInsightSimilarTrack>? tracks)
    {
        return string.Join(" · ", (tracks ?? [])
            .Select(track => JoinNonEmpty(track.Title, track.Artist))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static bool IsMetadataContextSection(TrackInsightSection section)
    {
        return string.Equals(section.Id, "metadata_context", StringComparison.OrdinalIgnoreCase)
            || string.Equals(section.Source, "metabrainz_metadata", StringComparison.OrdinalIgnoreCase);
    }

    private static TrackInsightRow MetadataContextSectionRow(TrackInsightSection section)
    {
        var detail = FirstNonEmpty(section.Summary, section.Text, DisplayJson(section.Value));
        if (string.IsNullOrWhiteSpace(detail) && (section.Items?.Count ?? 0) > 0)
        {
            detail = string.Join(" · ", section.Items!.Select(item => $"{FirstNonEmpty(item.Label, item.Title, Humanize(item.Id), Humanize(item.Kind))}: {DisplayJson(item.Value)} {item.Unit}".Trim()).Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        return new TrackInsightRow(
            FirstNonEmpty(section.Title, section.Label, "MusicBrainz / ListenBrainz"),
            "Open metadata",
            detail,
            SourceConfidenceLabel(section.Source, section.Confidence));
    }

    private static IEnumerable<TrackInsightRow> MetadataRows(TrackInsightMetadata metadata, string? fallbackSource, string? fallbackConfidence)
    {
        var meta = SourceConfidenceLabel("metabrainz_metadata", fallbackConfidence);
        var identity = JoinNonEmpty(metadata.RecordingTitle, metadata.Artist);
        if (!string.IsNullOrWhiteSpace(identity))
        {
            yield return new TrackInsightRow("MusicBrainz / ListenBrainz", "Context", identity, meta);
        }

        if (!string.IsNullOrWhiteSpace(metadata.MusicBrainzRecordingId))
        {
            yield return new TrackInsightRow("Recording ID", "MusicBrainz", metadata.MusicBrainzRecordingId, meta);
        }

        var releaseDetail = JoinNonEmpty(metadata.Release?.Title, metadata.Release?.Date, metadata.Release?.Country, metadata.Release?.Status);
        if (!string.IsNullOrWhiteSpace(releaseDetail))
        {
            yield return new TrackInsightRow("Release", "Open metadata", releaseDetail, meta);
        }

        if (!string.IsNullOrWhiteSpace(metadata.FirstReleaseDate))
        {
            yield return new TrackInsightRow("First release", "Open metadata", metadata.FirstReleaseDate, meta);
        }

        var genres = JoinNonEmpty(metadata.Genres?.ToArray() ?? []);
        if (!string.IsNullOrWhiteSpace(genres))
        {
            yield return new TrackInsightRow("Genres", "Open metadata", genres, meta);
        }

        var tags = JoinNonEmpty(metadata.Tags?.ToArray() ?? []);
        if (!string.IsNullOrWhiteSpace(tags))
        {
            yield return new TrackInsightRow("Tags", "Open metadata", tags, meta);
        }

        if (metadata.ListenBrainzListenCount.HasValue)
        {
            yield return new TrackInsightRow("ListenBrainz", "Public listens", metadata.ListenBrainzListenCount.Value.ToString(), meta);
        }

        if (metadata.MatchScore.HasValue)
        {
            yield return new TrackInsightRow("Match score", "Open metadata", metadata.MatchScore.Value.ToString(), meta);
        }
    }

    private static TrackInsightRow MetricRow(string group, TrackInsightMetric metric, string? fallbackSource, string? fallbackConfidence)
    {
        return new TrackInsightRow(
            group,
            FirstNonEmpty(metric.Label, metric.Title, Humanize(metric.Id), Humanize(metric.Kind), "Metric"),
            $"{DisplayJson(metric.Value)} {metric.Unit}".Trim(),
            SourceConfidenceLabel(metric.Source ?? fallbackSource, metric.Confidence ?? fallbackConfidence));
    }

    private static TrackInsightRow TimelineRow(TrackInsightTimelineEntry entry)
    {
        return new TrackInsightRow(
            FirstNonEmpty(entry.Label, entry.Title, Humanize(entry.Kind), Humanize(entry.Id), "Segment"),
            JoinNonEmpty(entry.Start ?? entry.StartTime, entry.End ?? entry.EndTime),
            "",
            SourceConfidenceLabel(entry.Source, entry.Confidence));
    }

    private static TrackInsightRow TipRow(TrackInsightTip tip)
    {
        return new TrackInsightRow(
            FirstNonEmpty(tip.Title, tip.Label, Humanize(tip.Kind), "DJ tip"),
            tip.Text ?? "",
            "",
            SourceConfidenceLabel(tip.Source, tip.Confidence));
    }

    private static TrackInsightRow LimitationRow(TrackInsightLimitation limitation)
    {
        return new TrackInsightRow(
            FirstNonEmpty(Humanize(limitation.Kind), limitation.Id, "Beperking"),
            limitation.Text ?? limitation.Message ?? "",
            "",
            SourceConfidenceLabel(limitation.Source, limitation.Confidence));
    }

    private static TrackInsightRow ProviderRow(TrackInsightProviderStatus provider)
    {
        var requiresConfig = provider.RequiresConfig.HasValue
            ? $"config: {(provider.RequiresConfig.Value ? "required" : "not required")}"
            : null;

        return new TrackInsightRow(
            FirstNonEmpty(provider.DisplayName, Humanize(provider.ProviderId), "Unknown"),
            FirstNonEmpty(provider.Status, "Unknown"),
            "",
            JoinNonEmpty(requiresConfig, LabelWithPrefix("reason", RedactProviderMetadata(provider.Reason))));
    }

    private static string? RedactProviderMetadata(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : DiagnosticRedactor.Redact(value);
    }

    private static string SourceConfidenceLabel(string? source, string? confidence)
    {
        return JoinNonEmpty(LabelWithPrefix("bron", source), LabelWithPrefix("confidence", confidence));
    }

    private static string LabelWithPrefix(string prefix, string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : $"{prefix}: {value}";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string JoinNonEmpty(params string?[] values)
    {
        return string.Join(" · ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string Humanize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Replace('_', ' ');
    }

    private static string DisplayJson(JsonElement? value)
    {
        if (value is null)
        {
            return "";
        }

        return value.Value.ValueKind switch
        {
            JsonValueKind.String => value.Value.GetString() ?? "",
            JsonValueKind.Number => value.Value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Undefined => "",
            _ => value.Value.GetRawText()
        };
    }
}

public sealed record TrackInsightRow(string Title, string Subtitle, string Detail, string Meta)
{
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    public bool HasMeta => !string.IsNullOrWhiteSpace(Meta);
}

public sealed record AskDJVoiceRequest(
    string ClientMessageId,
    string AudioResponse = "auto",
    string? Language = null,
    string? Locale = null,
    int? Mood = null);

public sealed record AskDJImage(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("alt")] string? Alt,
    [property: JsonPropertyName("title")] string? Title)
{
    public string DisplayUrl => FirstNonEmpty(Url, ImageUrl);
    public string DisplayLabel => FirstNonEmpty(Title, Alt);
    public bool HasImage => !string.IsNullOrWhiteSpace(DisplayUrl);

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public sealed record AskDJSource(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("url")] string? Url = null,
    [property: JsonPropertyName("href")] string? Href = null)
{
    public string DisplayLabel => FirstNonEmpty(Label, Name, Source, Id, Kind);
    public string DisplayUrl => FirstNonEmpty(Url, Href);
    public bool HasUrl => !string.IsNullOrWhiteSpace(DisplayUrl);

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public static class AskDJSourceCollection
{
    public static IReadOnlyList<AskDJSource> SourcesAndLinks(IReadOnlyList<AskDJSource>? sources, IReadOnlyList<AskDJSource>? links)
    {
        if ((sources?.Count ?? 0) == 0)
        {
            return links ?? [];
        }

        if ((links?.Count ?? 0) == 0)
        {
            return sources ?? [];
        }

        return sources!.Concat(links!).ToList();
    }
}

public sealed record PlaybackAction(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("command")] string? Command,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("button_label")] string? ButtonLabel,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("uri")] string? Uri,
    [property: JsonPropertyName("context_uri")] string? ContextUri,
    [property: JsonPropertyName("device_id")] string? DeviceId,
    [property: JsonPropertyName("value")] object? Value,
    [property: JsonPropertyName("action_style")] string? ActionStyle = null,
    [property: JsonPropertyName("response_value")] string? ResponseValue = null,
    [property: JsonPropertyName("image_url")] string? ImageUrl = null,
    [property: JsonPropertyName("source_url")] string? SourceUrl = null,
    [property: JsonPropertyName("music_backend_revision")] int? MusicBackendRevision = null)
{
    public string DisplayLabel
    {
        get
        {
            if (IsYesConfirmation)
            {
                return "Ja";
            }

            if (IsNoConfirmation)
            {
                return "Nee";
            }

            return ButtonLabel ?? Label ?? Title ?? Command ?? "Actie";
        }
    }

    public bool IsConfirmation => string.Equals(Kind, "confirmation", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ActionStyle, "confirmation", StringComparison.OrdinalIgnoreCase);
    public bool IsPlayNowRecommendation => string.Equals(ActionStyle, "play_now", StringComparison.OrdinalIgnoreCase)
        && (string.Equals(Kind, "track", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Kind, "album", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Kind, "artist", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Kind, "playlist", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Kind, "track_mix", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Kind, "playback", StringComparison.OrdinalIgnoreCase));
    public bool IsYesConfirmation => IsConfirmation && string.Equals(ResponseValue ?? Value?.ToString(), "yes", StringComparison.OrdinalIgnoreCase);
    public bool IsNoConfirmation => IsConfirmation && string.Equals(ResponseValue ?? Value?.ToString(), "no", StringComparison.OrdinalIgnoreCase);
    public bool IsSaveCurrentTrackControl => string.Equals(Kind, "control", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Command, "save_current_track", StringComparison.OrdinalIgnoreCase);
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool HasExternalLink => !string.IsNullOrWhiteSpace(SourceUrl ?? Uri);
}

public sealed record RecentItem(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("played_at_label")] string? PlayedAtLabel,
    [property: JsonPropertyName("image_url")] string? ImageUrl);

public sealed record CommandResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("dj_text")] string? DjText,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("update_required")] bool? UpdateRequired = null,
    [property: JsonPropertyName("ha_version")] string? HaVersion = null,
    [property: JsonPropertyName("ha_major_minor")] string? HaMajorMinor = null,
    [property: JsonPropertyName("backend_version")] string? BackendVersion = null,
    [property: JsonPropertyName("queue")]
    [property: JsonConverter(typeof(QueueItemsJsonConverter))]
    IReadOnlyList<QueueItem>? Queue = null,
    [property: JsonPropertyName("items")] IReadOnlyList<QueueItem>? Items = null,
    [property: JsonPropertyName("context")] string? Context = null,
    [property: JsonPropertyName("context_uri")] string? ContextUri = null,
    [property: JsonPropertyName("contextUri")] string? ContextUriCamel = null,
    [property: JsonPropertyName("playlists")] IReadOnlyList<PlaylistItem>? Playlists = null,
    [property: JsonPropertyName("collection")] PlaylistEnvelope? Collection = null,
    [property: JsonPropertyName("ha_local_url")] string? HomeAssistantLocalUrl = null,
    [property: JsonPropertyName("ha_remote_url")] string? HomeAssistantRemoteUrl = null,
    [property: JsonPropertyName("remote_supported")] bool? RemoteSupported = null,
    [property: JsonPropertyName("music_backend")] string? MusicBackend = null,
    [property: JsonPropertyName("music_backend_name")] string? MusicBackendName = null,
    [property: JsonPropertyName("music_backend_available")] bool? MusicBackendAvailable = null,
    [property: JsonPropertyName("music_backend_revision")] int? MusicBackendRevision = null,
    [property: JsonPropertyName("music_backend_capabilities")] MusicBackendCapabilities? MusicBackendCapabilities = null,
    [property: JsonPropertyName("music_target_player")] MusicTargetPlayer? MusicTargetPlayer = null,
    [property: JsonPropertyName("music_backend_error")] MusicBackendError? MusicBackendError = null);

public sealed record StatusResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("spotify_configured")] bool? SpotifyConfigured,
    [property: JsonPropertyName("ask_dj_supported")] bool? AskDJSupported,
    [property: JsonPropertyName("ask_dj_voice_supported")] bool? AskDJVoiceSupported,
    [property: JsonPropertyName("backend_available")] bool? BackendAvailable,
    [property: JsonPropertyName("runtime_compatible")] bool? RuntimeCompatible,
    [property: JsonPropertyName("compatible")] bool? Compatible,
    [property: JsonPropertyName("update_required")] bool? UpdateRequired,
    [property: JsonPropertyName("ha_version")] string? HaVersion,
    [property: JsonPropertyName("ha_major_minor")] string? HaMajorMinor,
    [property: JsonPropertyName("backend_version")] string? BackendVersion,
    [property: JsonPropertyName("minimum_app_version")] string? MinimumAppVersion,
    [property: JsonPropertyName("playback")] PlaybackState? Playback,
    [property: JsonPropertyName("queue")]
    [property: JsonConverter(typeof(QueueItemsJsonConverter))]
    IReadOnlyList<QueueItem>? Queue,
    [property: JsonPropertyName("items")] IReadOnlyList<QueueItem>? Items,
    [property: JsonPropertyName("queue_items")] IReadOnlyList<QueueItem>? QueueItems,
    [property: JsonPropertyName("collection")] QueueEnvelope? Collection,
    [property: JsonPropertyName("playlists")] IReadOnlyList<PlaylistItem>? Playlists,
    [property: JsonPropertyName("playlist_items")] IReadOnlyList<PlaylistItem>? PlaylistItems,
    [property: JsonPropertyName("playlist_collection")] PlaylistEnvelope? PlaylistCollection,
    [property: JsonPropertyName("outputs")] IReadOnlyList<PlaybackOutput>? Outputs,
    [property: JsonPropertyName("output_devices")] IReadOnlyList<PlaybackOutput>? OutputDevices,
    [property: JsonPropertyName("devices")] IReadOnlyList<PlaybackOutput>? Devices,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("ha_local_url")] string? HomeAssistantLocalUrl = null,
    [property: JsonPropertyName("ha_remote_url")] string? HomeAssistantRemoteUrl = null,
    [property: JsonPropertyName("remote_supported")] bool? RemoteSupported = null,
    [property: JsonPropertyName("music_backend")] string? MusicBackend = null,
    [property: JsonPropertyName("music_backend_name")] string? MusicBackendName = null,
    [property: JsonPropertyName("music_backend_available")] bool? MusicBackendAvailable = null,
    [property: JsonPropertyName("music_backend_revision")] int? MusicBackendRevision = null,
    [property: JsonPropertyName("music_backend_capabilities")] MusicBackendCapabilities? MusicBackendCapabilities = null,
    [property: JsonPropertyName("music_target_player")] MusicTargetPlayer? MusicTargetPlayer = null,
    [property: JsonPropertyName("music_backend_error")] MusicBackendError? MusicBackendError = null);

public sealed record PlaybackState(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("is_playing")] bool? IsPlaying,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("album_artwork_url")] string? AlbumArtworkUrl,
    [property: JsonPropertyName("artwork_url")] string? ArtworkUrl,
    [property: JsonPropertyName("position_ms")] int? PositionMs,
    [property: JsonPropertyName("duration_ms")] int? DurationMs,
    [property: JsonPropertyName("progress_ms")] int? ProgressMs,
    [property: JsonPropertyName("volume_percent")] int? VolumePercent,
    [property: JsonPropertyName("active_output_device")] string? ActiveOutputDevice,
    [property: JsonPropertyName("output_device")] PlaybackOutput? OutputDevice,
    [property: JsonPropertyName("active_output")] PlaybackOutput? ActiveOutput);

public sealed record PlaybackOutput(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("is_active")] bool? IsActive)
{
    public string DisplayName => Label ?? Name ?? Id ?? "";
    public string CommandValue => Id ?? Name ?? Label ?? "";
}

public sealed record QueueEnvelope(
    [property: JsonPropertyName("items")] IReadOnlyList<QueueItem>? Items,
    [property: JsonPropertyName("queue")] IReadOnlyList<QueueItem>? Queue);

public sealed record PlaylistEnvelope(
    [property: JsonPropertyName("items")] IReadOnlyList<PlaylistItem>? Items,
    [property: JsonPropertyName("playlists")] IReadOnlyList<PlaylistItem>? Playlists,
    [property: JsonPropertyName("collection")] IReadOnlyList<PlaylistItem>? Collection);

public sealed record PlaylistItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("playlist_id")] string? PlaylistId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("display_title")] string? RawDisplayTitle,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("owner")] string? Owner,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("source_label")] string? SourceLabel,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("artwork_url")] string? ArtworkUrl,
    [property: JsonPropertyName("album_image_url")] string? AlbumImageUrl,
    [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
    [property: JsonPropertyName("uri")] string? Uri,
    [property: JsonPropertyName("context_uri")] string? ContextUri,
    [property: JsonPropertyName("playlist_uri")] string? PlaylistUri,
    [property: JsonPropertyName("playable")] bool? Playable,
    [property: JsonPropertyName("playback_action")] PlaybackAction? PlaybackAction,
    [property: JsonIgnore] int Position = 0)
{
    public string DisplayTitle => FirstNonEmpty(Name, Title, RawDisplayTitle, Value);
    public string DisplaySubtitle => FirstNonEmpty(Subtitle, Description, Owner, SourceLabel, Source);
    public string Artwork => FirstNonEmpty(ImageUrl, ArtworkUrl, AlbumImageUrl, ThumbnailUrl);
    public string CommandUri => FirstNonEmpty(Uri, ContextUri, PlaylistUri);
    public string StableId => FirstNonEmpty(Id, PlaylistId, CommandUri, $"{DisplayTitle}|{DisplaySubtitle}|{Position}");
    public bool IsPlayable => Playable ?? PlaybackAction is not null || !string.IsNullOrWhiteSpace(CommandUri);
    public bool HasArtwork => !string.IsNullOrWhiteSpace(Artwork);
    public bool HasNoArtwork => !HasArtwork;

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public sealed record QueueItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("item_id")] string? ItemId,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("display_title")] string? DisplayTitle,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("artist_name")] string? ArtistName,
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("album_name")] string? AlbumName,
    [property: JsonPropertyName("duration_ms")] int? DurationMs,
    [property: JsonPropertyName("duration")] string? Duration,
    [property: JsonPropertyName("uri")] string? Uri,
    [property: JsonPropertyName("track_uri")] string? TrackUri,
    [property: JsonPropertyName("context_uri")] string? ContextUri,
    [property: JsonPropertyName("album_image_url")] string? AlbumImageUrl,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("artwork_url")] string? ArtworkUrl,
    [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
    [property: JsonPropertyName("is_playing")] bool? IsPlaying,
    [property: JsonPropertyName("is_current")] bool? IsCurrent,
    [property: JsonPropertyName("playable")] bool? Playable,
    [property: JsonPropertyName("playback_action")] PlaybackAction? PlaybackAction,
    [property: JsonIgnore] int Position = 0)
{
    public string DisplayTitleValue => FirstNonEmpty(Title, Name, DisplayTitle);
    public string DisplaySubtitle => FirstNonEmpty(Artist, ArtistName, Subtitle);
    public string DisplayAlbum => FirstNonEmpty(Album, AlbumName);
    public string Artwork => FirstNonEmpty(AlbumImageUrl, ImageUrl, ThumbnailUrl, ArtworkUrl);
    public string CommandUri => FirstNonEmpty(Uri, TrackUri);
    public string StableId => FirstNonEmpty(CommandUri, Id, ItemId, $"{DisplayTitleValue}|{DisplaySubtitle}|{Position}");
    public bool IsPlayable => Playable ?? PlaybackAction is not null || !string.IsNullOrWhiteSpace(CommandUri);
    public bool IsActive => IsPlaying == true || IsCurrent == true;
    public bool HasArtwork => !string.IsNullOrWhiteSpace(Artwork);
    public bool HasNoArtwork => !HasArtwork;
    public string DurationLabel => !string.IsNullOrWhiteSpace(Duration)
        ? Duration!
        : DurationMs.HasValue ? $"{DurationMs.Value / 60000}:{DurationMs.Value / 1000 % 60:00}" : "";
    public string PositionLabel => Position > 0 ? Position.ToString("00") : "";

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public static class StatusResponseExtensions
{
    public static IReadOnlyList<PlaybackOutput>? ResolvedOutputs(this StatusResponse response)
    {
        return response.Outputs ?? response.OutputDevices ?? response.Devices;
    }

    public static IReadOnlyList<QueueItem>? ResolvedQueue(this StatusResponse response)
    {
        return response.Queue ?? response.QueueItems ?? response.Items ?? response.Collection?.Items ?? response.Collection?.Queue;
    }

    public static IReadOnlyList<QueueItem>? ResolvedQueue(this CommandResponse response)
    {
        return ApplyQueueContext(response.Queue ?? response.Items, FirstNonEmpty(response.ContextUri, response.ContextUriCamel, response.Context));
    }

    public static IReadOnlyList<PlaylistItem>? ResolvedPlaylists(this StatusResponse response)
    {
        return response.Playlists
            ?? response.PlaylistItems
            ?? response.PlaylistCollection?.Items
            ?? response.PlaylistCollection?.Playlists
            ?? response.PlaylistCollection?.Collection;
    }

    private static IReadOnlyList<QueueItem>? ApplyQueueContext(IReadOnlyList<QueueItem>? items, string contextUri)
    {
        if (items is null || string.IsNullOrWhiteSpace(contextUri))
        {
            return items;
        }

        return items.Select(item => string.IsNullOrWhiteSpace(item.ContextUri)
            ? item with { ContextUri = contextUri }
            : item).ToArray();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public sealed class QueueItemsJsonConverter : JsonConverter<IReadOnlyList<QueueItem>?>
{
    public override IReadOnlyList<QueueItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<QueueItem>>(root.GetRawText(), options);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var contextUri = ReadString(root, "context_uri")
            ?? ReadString(root, "contextUri")
            ?? ReadString(root, "context");
        var itemsElement = root.TryGetProperty("items", out var nestedItems) && nestedItems.ValueKind == JsonValueKind.Array
            ? nestedItems
            : root.TryGetProperty("queue", out var nestedQueue) && nestedQueue.ValueKind == JsonValueKind.Array
                ? nestedQueue
                : default;

        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var items = JsonSerializer.Deserialize<IReadOnlyList<QueueItem>>(itemsElement.GetRawText(), options);
        if (items is null || string.IsNullOrWhiteSpace(contextUri))
        {
            return items;
        }

        return items.Select(item => string.IsNullOrWhiteSpace(item.ContextUri)
            ? item with { ContextUri = contextUri }
            : item).ToArray();
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<QueueItem>? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value?.ToArray(), options);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

public static class QueueItemNormalizer
{
    public static IReadOnlyList<QueueItem> Normalize(IReadOnlyList<QueueItem>? items, int limit = 100)
    {
        var normalized = new List<QueueItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var position = 1;
        foreach (var raw in items ?? [])
        {
            var item = raw with { Position = position };
            if (string.IsNullOrWhiteSpace(item.DisplayTitleValue))
            {
                continue;
            }

            if (!seen.Add(item.StableId))
            {
                continue;
            }

            normalized.Add(item);
            position++;
            if (normalized.Count >= limit)
            {
                break;
            }
        }

        return normalized;
    }
}

public static class PlaylistItemNormalizer
{
    public static IReadOnlyList<PlaylistItem> Normalize(IReadOnlyList<PlaylistItem>? items, int limit = 100)
    {
        var normalized = new List<PlaylistItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var position = 1;
        foreach (var raw in items ?? [])
        {
            var item = raw with { Position = position };
            if (string.IsNullOrWhiteSpace(item.DisplayTitle))
            {
                continue;
            }

            if (!seen.Add(item.StableId))
            {
                continue;
            }

            normalized.Add(item);
            position++;
            if (normalized.Count >= limit)
            {
                break;
            }
        }

        return normalized;
    }
}
