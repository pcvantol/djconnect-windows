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
    [property: JsonPropertyName("items")] IReadOnlyList<RecentItem>? Items);

public sealed record AskDJMessageResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("user_message")] AskDJMessage? UserMessage,
    [property: JsonPropertyName("assistant_message")] AskDJMessage? AssistantMessage,
    [property: JsonPropertyName("history_revision")] long? HistoryRevision,
    [property: JsonPropertyName("clear_revision")] long? ClearRevision,
    [property: JsonPropertyName("playback_actions")] IReadOnlyList<PlaybackAction>? PlaybackActions,
    [property: JsonPropertyName("confirmation_actions")] IReadOnlyList<PlaybackAction>? ConfirmationActions,
    [property: JsonPropertyName("items")] IReadOnlyList<RecentItem>? Items,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("error")] string? Error);

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
    [property: JsonPropertyName("value")] object? Value);

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
    [property: JsonPropertyName("error")] string? Error);

public sealed record StatusResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("spotify_configured")] bool? SpotifyConfigured,
    [property: JsonPropertyName("ask_dj_supported")] bool? AskDJSupported,
    [property: JsonPropertyName("ask_dj_voice_supported")] bool? AskDJVoiceSupported,
    [property: JsonPropertyName("playback")] PlaybackState? Playback,
    [property: JsonPropertyName("error")] string? Error);

public sealed record PlaybackState(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("is_playing")] bool? IsPlaying,
    [property: JsonPropertyName("image_url")] string? ImageUrl);
