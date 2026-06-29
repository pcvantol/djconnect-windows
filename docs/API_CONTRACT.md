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
POST /api/djconnect/pair
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

## Status

```http
POST /api/djconnect/status
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
POST /api/djconnect/ask_dj/message
```

Voice / push-to-talk requests, once the platform capture backend is available:

```http
POST /api/djconnect/voice
Content-Type: multipart/form-data
audio: audio/wav
```

History sync:

```http
GET /api/djconnect/ask_dj/history?since_revision=<number>
```

Clear:

```http
POST /api/djconnect/ask_dj/history/clear
```

Text and voice requests include `client_type`, `device_id`, `device_name`,
`client_id`, `client_message_id`, `audio_response`, app/protocol version
metadata and optional numeric `mood`. The server derives the canonical mood
zone: `chill` for `0`-`24`, `groove` for `25`-`59`, `energy` for `60`-`84`
and `party` for `85`-`100`. The app persists `history_revision` and
`clear_revision` as sync cursors.
History is Home Assistant user scoped and limited by the backend to 1000
messages per HA user.

If `messages[]` is present, it is canonical. The client uses
`exchange_id`/`exchange_order` only to preserve the server-provided user then
assistant order. It does not reconstruct intents, follow-up context, playback
actions, memory or history locally.

## Actions

Ask DJ playback actions, recommendation actions, output actions and follow-up
confirmations are sent to:

```http
POST /api/djconnect/command
```

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
to Home Assistant intact through `/api/djconnect/command`.

Response `text`, `dj_text` or `message` is rendered as the main answer.
`images[]`, `sources[]`, `links[]`, `items[]`, `playback_actions[]` and
`confirmation_actions[]` are rendered only when they are explicitly present on
the response or message. The client must not reuse previous album art/media,
show a TTS replay button without `audio_url`, expose raw Spotify URIs/backend
IDs in visible text or infer actions from answer text.

Track Insight can be opened directly from Now Playing with
`POST /api/djconnect/track_insight`. The client sends Home Assistant auth and
the explicit track fields it has (`title`, `artist`, `album`, optional
`entity_id`, `player_id`, `music_backend`, `locale`, `force_refresh` and
`include_visual_profile`). If metadata is missing, Home Assistant resolves Now
Playing. `no_track_playing` is rendered as an empty state.

Ask DJ Track Insight responses are detected when `intent.intent`, `action`,
`type` or `open_screen` is `track_insight`. They are informational/read-only
unless `playback_actions[]` is explicitly present. The client renders the
normalized `track_insight` object: `track`, `analysis`, `music_dna`,
`visual_profile` and `cache`. Music DNA Match is read from
`track_insight.music_dna.match_percent`. `visual_profile` is treated only as
rendering hints; the client does not expect server-generated images or video.
The client must not parse prose in `text` or `dj_text` to infer BPM,
timestamps, song-section labels or DJ tips.

## Queue And Playlists

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

DJConnect websocket payloads still include DJConnect identity and auth fields:
`device_token`, `device_id`, `client_id`, `device_name` and canonical
`client_type`. The `device_token` is sent inside DJConnect payloads only after
Home Assistant websocket auth has succeeded. Any websocket error, timeout,
disconnect, auth failure, protocol mismatch, malformed result or missing
capability falls back immediately to the existing HTTP flow for the current
action. WebSocket transport failures must not clear pairing or be treated as
stale pairing.

These remain HTTP-only:

- `/api/djconnect/pair`;
- `/api/djconnect/status`;
- `/api/djconnect/voice`;
- `/api/djconnect/ask_dj/history`;
- `/api/djconnect/ask_dj/history/clear`;
- `/api/djconnect/ask_dj/idle_suggestion`;
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
