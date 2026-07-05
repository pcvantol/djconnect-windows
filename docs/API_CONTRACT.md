# API Contract

This document records the Home Assistant `djconnect` endpoints used by the
desktop app. The backend remains the source of truth; this repo mirrors the
client-facing contract needed for Windows/macOS desktop runtime.

## Identity

Current local constants:

```json
{
  "client_type": "windows",
  "device_id": "djconnect-windows-XXXXXXXXXXXX"
}
```

`windows` is canonical for protocol `3.2.x`. Windows does not expose a
client-hosted local API or mDNS pairing service.

## Pairing

```http
POST /api/djconnect/v1/pair
```

Payload includes:

- `device_id`
- `device_name`
- `client_type`
- `pair_code`
- `app_version`

Response may include `success`, `client_type`, `device_token`, `device_id`,
`ha_pairing_status`, `message`, `error`, `ha_local_url`, `ha_remote_url`,
`api_base`, endpoint paths, capability flags, `remote_supported` and music
backend summary fields. The app stores only the returned DJConnect bearer token
plus local/remote Home Assistant URL metadata. Pairing must use the local Home
Assistant URL; remote pairing is rejected and remote URLs are used only after
successful local pairing.

The Windows pairing screen is outbound-only. It asks for the local Home
Assistant URL and the Home Assistant-generated six-digit pairing code, then the
user presses the pairing button. It must not show a Windows client address,
generate/copy its own pairing code or wait for Home Assistant to call Windows
back.

## Status

```http
POST /api/djconnect/v1/status
```

Payload includes `device_id`, `device_name`, `client_type`, `firmware:
"windows-app"` and app/protocol version metadata. Response may include
`spotify_configured`, `ask_dj_supported`, `ask_dj_voice_supported`,
`voice_supported`, `ask_dj_audio_response_supported` and `playback`.
Authenticated DJConnect requests include `Authorization: Bearer <device_token>`
and `X-DJConnect-Device-ID: <device_id>`.

HTTP `401`/`403` means stale pairing or invalid token. HTTP `426` means
protocol mismatch and must not be treated as a token failure. At runtime the
client chooses the local HA URL when reachable, falls back to the remote HA URL
when local is unreachable and remote is supported, and marks itself offline
when neither URL is reachable.

User-visible API guidance is localized in the client. Backend protocol values,
JSON field names, endpoint paths, bearer tokens and `client_type: "windows"`
must remain byte-for-byte unchanged. Error codes such as
`client_type_mismatch`, `invalid_pair_code`, `invalid_client_type`,
`not_configured`, `unauthorized` and stale auth signals are mapped by the
client into concise localized instructions instead of being shown directly.

Pair/status/command/Ask DJ responses may include this backend summary:

```json
{
  "ha_local_url": "http://192.168.1.x:8123",
  "ha_remote_url": "https://example.ui.nabu.casa",
  "remote_supported": true,
  "music_backend": "music_assistant",
  "music_backend_name": "Music Assistant",
  "music_backend_available": true,
  "music_backend_revision": 4,
  "music_backend_capabilities": {
    "supports_search": true,
    "supports_queue": true,
    "supports_outputs": true,
    "supports_favorites": false,
    "supports_recently_played": true,
    "supports_top_items": false
  },
  "music_target_player": {
    "id": "media_player.mass_woonkamer",
    "name": "Woonkamer"
  },
  "music_backend_error": null
}
```

`music_backend_error` is either `null` or a safe object with `code` and
user-facing `message`. The app never expects raw backend exceptions, Spotify
OAuth tokens, Home Assistant long-lived tokens or Music Assistant secrets in
that field.

Backend-specific playback actions carry `music_backend_revision`. The client
forwards this revision with the action payload and treats
`stale_backend_action` as a prompt to refresh or ask Ask DJ again. Backend
capability gaps use `unsupported_backend_capability` and should be shown as a
clear unavailable-feature message, without phantom Spotify UI.

## Ask DJ

Text requests:

```http
POST /api/djconnect/v1/ask_dj/message
```

Voice / push-to-talk requests, once the platform capture backend is available:

```http
POST /api/djconnect/v1/voice
Content-Type: multipart/form-data
audio: audio/wav
```

History sync:

```http
GET /api/djconnect/v1/ask_dj/history?since_revision=<number>
```

Clear:

```http
POST /api/djconnect/v1/ask_dj/history/clear
```

Text and voice requests include `client_type`, `device_id`, `device_name`,
`client_id`, `client_message_id`, `audio_response`, app/protocol version
metadata, BCP-47 `language`/`locale` values such as `nl-NL` or `en-GB`, and
optional numeric `mood` and `music_dna_key` when available. Raw WAV voice uploads also send
`X-DJConnect-Language` and `X-DJConnect-Locale` headers because the audio body
cannot be represented as JSON. The server derives the canonical mood zone:
`chill` for `0`-`24`, `groove` for `25`-`59`, `energy` for `60`-`84` and
`party` for `85`-`100`. The app persists `history_revision` and
`clear_revision` as sync cursors.
History is Home Assistant user scoped and limited by the backend to 1000
messages per HA user.

If `messages[]` is present, it is canonical. The client uses
`exchange_id`/`exchange_order` only to preserve the server-provided user then
assistant order. It does not reconstruct intents, follow-up context, playback
actions, memory or history locally.

Generated-text decoration is backend-authoritative. The client shows the
generated text spark only when `assistant_message.is_generated_text` is `true`
or, when no `assistant_message` is present, response-level `is_generated_text`
is `true`. Missing metadata, fallback text, system messages, errors, Play Now
fallback labels and Track Insight local/fallback analysis must not show the
spark. When shown, the spark is inline before the generated answer text, never
before the role label.

Assistant bubbles are colored from mood context using the backend/client mood
zone thresholds above. User bubbles stay the fixed user color.

## Actions

Ask DJ playback actions, recommendation actions, output actions and follow-up
confirmations are sent to:

```http
POST /api/djconnect/v1/command
```

Commands that can produce user-facing text or audio include the same BCP-47
`language`/`locale` metadata as Ask DJ text requests. Protocol values such as
`client_type`, command names, endpoint paths, JSON keys, service ids, entity ids
and tokens are never localized.

Confirmation actions default to `command: "ask_dj_followup_response"`.
Recommendation actions with `action_style: "play_now"` for track, album,
artist, playlist or track-mix actions default to
`command: "ask_dj_play_recommendation"` unless the backend provides a more
specific command. `command: "ask_dj_message"` sends `value.text`/`value.prompt`
back as a new Ask DJ backend message, or forwards the backend action to the
command endpoint.

Recent-played informational responses render returned `items[]` as compact
lists. The app must not invent Play Now buttons unless `playback_actions[]` is
present. Playback actions are backend-owned. Spotify Direct actions may contain
`spotify:` URIs; Music Assistant actions may contain `item_id`, `provider`,
`media_type` and `target_player_id`. The client forwards action/value data back
to Home Assistant intact through `/api/djconnect/v1/command`.

Response `text`, `dj_text` or `message` is rendered as the main answer.
`images[]`, `sources[]`, `links[]`, `items[]`, `playback_actions[]` and
`confirmation_actions[]` are rendered only when they are explicitly present on
the response or message. The client must not reuse previous album art/media,
show a TTS replay button without `audio_url`, expose raw Spotify URIs/backend
IDs in visible text or infer actions from answer text.

Track Insight can be opened directly from Now Playing with
`POST /api/djconnect/v1/track_insight`. The client sends Home Assistant auth and
the explicit track fields it has (`title`, `artist`, `album`, optional
`entity_id`, `player_id`, `music_backend`, `language`, `locale`, `mood`,
`music_dna_key`, `force_refresh` and `include_visual_profile`). If metadata is
missing, Home Assistant resolves Now Playing. `no_track_playing` is rendered as
an empty state.

Ask DJ Track Insight responses are detected when `intent.intent`, `action`,
`type` or `open_screen` is `track_insight`. They are informational/read-only
unless `playback_actions[]` is explicitly present. The client renders the
normalized `track_insight` object: `track`, `analysis`, `music_dna`,
`visual_profile` and `cache`. Music DNA Match is read from
`track_insight.music_dna.match_percent`. `visual_profile` is treated only as
rendering hints; the client does not expect server-generated images or video.
The client must not parse prose in `text` or `dj_text` to infer tempo,
musical-key, timestamps, song-section labels or DJ tips, and it does not render
tempo or musical-key fields as Track Insight cards.

## Music DNA

Music DNA is opt-in and server-authoritative. The Windows client does not infer
profile conclusions locally and renders only backend fields.

Endpoints:

```http
POST /api/djconnect/v1/music_dna/profile
POST /api/djconnect/v1/music_dna/settings
POST /api/djconnect/v1/music_dna/clear
```

Profile requests include `client_id`, `client_type: "windows"`, `device_id`,
`device_name`, `language`, `locale`, optional `music_dna_key` and optional
numeric `mood`. Settings and clear requests keep the same identity/context;
clear removes profile data but preserves the opt-in setting.

The profile renderer may use only backend-provided `summary`,
`favorite_genres`, `favorite_artists`, `recent_tracks`,
`recent_favorite_tracks`, `playtime`, `listening_rhythm`, `mood_mix`,
`energy_profile`, `repeat_magnets`, `explicit_positives`, `taste_anchors`,
`recommendation_signals`, `mood_profile` or `mood`, `taste_direction`,
`based_on` and `updated_at`. Backend array order is preserved. Compact top
values are shown without local re-sorting. Missing blocks, empty arrays and
empty objects are hidden.

Music DNA is explicitly opt-in. `settings` with `enabled: true` enables the
server profile. `settings` with `enabled: false` disables Music DNA and clears
learned knowledge server-side. `clear` clears learned knowledge while keeping
the current opt-in setting. If `profile` returns `enabled: false`, Windows shows
the opt-in state instead of dashboard cards.

Mood rendering prefers `average` plus `average_zone`, then falls back to
`value` plus `zone`. It must not show a dash or "too little data" copy when
`sample_count` is positive or a value exists. Energy rendering prefers
`energy_percent` with `zone` as label and may show backend `danceability` and
`intensity` as secondary text. "Based on" uses `based_on` or backend recent
signals, never a local guess. Track signal text is formatted as
`title — artist` or `title — artist · album`.

Dashboard block visibility follows backend eligibility:

- `playtime` renders only when `total_seconds > 0` and uses backend
  `formatted_total` when present.
- `listening_rhythm` renders only when `sample_count >= 3`.
- `mood_mix` and `energy_profile` render only when `sample_count > 0`.
- `repeat_magnets`, `explicit_positives` and `taste_anchors` render only when
  `eligible == true` and their item arrays are non-empty.
- `blocked_artists` and `blocked_items` are not prominent dashboard cards.

## Queue And Playlists

## Music Discovery

Ontdek / Music Discovery is a first-class feature gated by Music DNA opt-in.
Windows must not request recommendations while Music DNA is disabled. If Music
DNA is disabled, the app shows the consent/empty state and may enable Music DNA
through the normal Music DNA settings endpoint before loading the feed.
Responses with `enabled: false` or `error: "music_dna_disabled"` are treated as
non-renderable feeds even when item arrays are present. Stale pairing errors
follow the shared DJConnect cleanup path and show the pair-again state.

Endpoints:

```http
GET /api/djconnect/v1/music_discovery
POST /api/djconnect/v1/music_discovery/refresh
POST /api/djconnect/v1/music_discovery/play
```

Requests include the Windows DJConnect identity (`client_type: "windows"`,
`client_id`, `device_id`, `device_name`) plus `language`, `locale`, `mood` and
`music_dna_key` where supported. The feed endpoint sends that identity as GET
query parameters; refresh and play send JSON bodies. Feed and refresh responses are server-owned;
the client renders supported recommendation kinds (`track`, `album`, `artist`,
`playlist`) with backend title, subtitle/context, artwork, subtle
confidence/relevance and optional backend `reason`. Reasons are shown through
an explicit info action and are hidden when missing.

Play Now sends the recommendation id or item id, kind, available URI metadata
and `source: "music_discovery"` to `/music_discovery/play`. Positive Music DNA
signals are handled by Home Assistant; Windows does not calculate them locally.

Queue state may appear as `queue`, `items`, `queue_items` or inside collection
envelopes. Playlist state may appear as `playlists`, `playlist_items` or inside
collection envelopes. The app normalizes aliases such as `title`, `name`,
`display_title`, `subtitle`, `description`, `owner`, `source`, artwork URL
fields and URI/context fields before rendering.

The client caps rendered queue and playlist rows at 100 and deduplicates by
stable ids or deterministic signatures. Starting backend-owned media uses
generic commands only:

```json
{
  "command": "playlist_start",
  "playlist_uri": "<backend-provided-uri>",
  "output_id": "<selected-output-id>"
}
```

Queue item start follows the same pattern with `queue_item_play` or a
backend-returned action. Removed Spotify override fields such as
`spotify_source` and `liked_proxy_playlist_uri` must not be emitted.

## Local WebSocket Fast Path

HTTP remains the canonical DJConnect transport and the safe default. The client
must not enable Home Assistant's native `/api/websocket` fast path by default.
It is a live-test opt-in only, limited to local Home Assistant URLs and only
when a valid Home Assistant websocket auth token/mechanism is available.
Remote/Nabu Casa sessions stay on HTTP unless explicitly supported later.

The client first uses the Home Assistant websocket auth flow. The paired
DJConnect `device_token` must not be assumed to authenticate `/api/websocket`.
After Home Assistant auth succeeds, the client sends `djconnect/capabilities`.
WebSocket is used only when capability detection succeeds, the response has
`websocket_supported: true`, `transports.websocket: true` and `commands[]`
contains the required route:

- `djconnect/command`;
- `djconnect/ask_dj/message`;
- `djconnect/track_insight`.
- `djconnect/music_dna/profile`;
- `djconnect/music_dna/settings`;
- `djconnect/music_dna/clear`.
- `djconnect/music_discovery/feed`;
- `djconnect/music_discovery/refresh`;
- `djconnect/music_discovery/play`.

DJConnect websocket payloads still include DJConnect identity and auth fields:
`device_token`, `device_id`, `client_id`, `device_name` and canonical
`client_type`. The `device_token` is sent inside DJConnect payloads only after
Home Assistant websocket auth has succeeded. Any websocket error, timeout,
disconnect, auth failure, protocol mismatch, malformed result or missing
capability falls back immediately to the existing HTTP flow for the current
action. WebSocket transport failures must not clear pairing or be treated as
stale pairing.

These remain HTTP-only:

- `/api/djconnect/v1/pair`;
- `/api/djconnect/v1/status`;
- `/api/djconnect/v1/voice`;
- `/api/djconnect/v1/ask_dj/history`;
- `/api/djconnect/v1/ask_dj/history/clear`;
- `/api/djconnect/v1/ask_dj/idle_suggestion`;
- push registration;
- image proxy;
- TTS/audio download URLs;
- Spotify OAuth callback.

Windows does not expose or require local `/api/device/*` endpoints.

## Diagnostics And User-Prepared Reports

Feedback, logs and crash reports are local client-generated Markdown/text
exports. They are not sent to Home Assistant and are not uploaded by API. The
client redacts bearer tokens, Authorization headers, pairing codes, bootstrap
proofs, Home Assistant long-lived tokens, push tokens, cookies, secrets,
password/API-key patterns and private URLs before preview, copy or GitHub issue
URL construction.
