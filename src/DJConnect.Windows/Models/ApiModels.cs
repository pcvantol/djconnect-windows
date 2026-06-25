using System.Text.Json.Serialization;

namespace DJConnect.Windows.Models;

public sealed record PairingPayload(
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("pairing_token")] string PairingToken,
    [property: JsonPropertyName("pair_code")] string PairCode,
    [property: JsonPropertyName("pairing_code")] string PairingCode);

public sealed record PairingResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("device_token")] string? DeviceToken,
    [property: JsonPropertyName("ha_pairing_status")] string? PairingStatus,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("error")] string? Error);

public sealed record AskDJRequest(
    [property: JsonPropertyName("client_message_id")] string ClientMessageId,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("client_type")] string ClientType,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, object?>? Metadata = null,
    [property: JsonPropertyName("audio_response")] string AudioResponse = "auto");

public sealed record AskDJHistoryResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("history_revision")] long HistoryRevision,
    [property: JsonPropertyName("clear_revision")] long ClearRevision,
    [property: JsonPropertyName("history_limit")] int? HistoryLimit,
    [property: JsonPropertyName("history_trimmed_before")] DateTimeOffset? HistoryTrimmedBefore,
    [property: JsonPropertyName("history_trimmed_count")] int? HistoryTrimmedCount,
    [property: JsonPropertyName("messages")] IReadOnlyList<AskDJMessage> Messages,
    [property: JsonPropertyName("error")] string? Error = null);

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
    [property: JsonIgnore] bool IsFailed = false)
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
    public bool HasSources => (Sources?.Count ?? 0) > 0;
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
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("dj_text")] string? DjText,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("audio_url")] string? AudioUrl,
    [property: JsonPropertyName("error")] string? Error);

public sealed record AskDJVoiceRequest(
    string ClientMessageId,
    string AudioResponse = "auto");

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
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("kind")] string? Kind)
{
    public string DisplayLabel => FirstNonEmpty(Label, Name, Id, Kind);

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
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
    [property: JsonPropertyName("source_url")] string? SourceUrl = null)
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
    [property: JsonPropertyName("queue")] IReadOnlyList<QueueItem>? Queue = null,
    [property: JsonPropertyName("items")] IReadOnlyList<QueueItem>? Items = null,
    [property: JsonPropertyName("playlists")] IReadOnlyList<PlaylistItem>? Playlists = null,
    [property: JsonPropertyName("collection")] PlaylistEnvelope? Collection = null);

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
    [property: JsonPropertyName("queue")] IReadOnlyList<QueueItem>? Queue,
    [property: JsonPropertyName("items")] IReadOnlyList<QueueItem>? Items,
    [property: JsonPropertyName("queue_items")] IReadOnlyList<QueueItem>? QueueItems,
    [property: JsonPropertyName("collection")] QueueEnvelope? Collection,
    [property: JsonPropertyName("playlists")] IReadOnlyList<PlaylistItem>? Playlists,
    [property: JsonPropertyName("playlist_items")] IReadOnlyList<PlaylistItem>? PlaylistItems,
    [property: JsonPropertyName("playlist_collection")] PlaylistEnvelope? PlaylistCollection,
    [property: JsonPropertyName("outputs")] IReadOnlyList<PlaybackOutput>? Outputs,
    [property: JsonPropertyName("output_devices")] IReadOnlyList<PlaybackOutput>? OutputDevices,
    [property: JsonPropertyName("devices")] IReadOnlyList<PlaybackOutput>? Devices,
    [property: JsonPropertyName("error")] string? Error);

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
    [property: JsonPropertyName("subtitle")] string? Subtitle,
    [property: JsonPropertyName("album")] string? Album,
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
    public string DisplaySubtitle => FirstNonEmpty(Artist, Subtitle, Album);
    public string DisplayAlbum => Album ?? "";
    public string Artwork => FirstNonEmpty(AlbumImageUrl, ImageUrl, ArtworkUrl, ThumbnailUrl);
    public string CommandUri => FirstNonEmpty(Uri, TrackUri, ContextUri);
    public string StableId => FirstNonEmpty(Id, ItemId, CommandUri, $"{DisplayTitleValue}|{DisplaySubtitle}|{Position}");
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

    public static IReadOnlyList<PlaylistItem>? ResolvedPlaylists(this StatusResponse response)
    {
        return response.Playlists
            ?? response.PlaylistItems
            ?? response.PlaylistCollection?.Items
            ?? response.PlaylistCollection?.Playlists
            ?? response.PlaylistCollection?.Collection;
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
