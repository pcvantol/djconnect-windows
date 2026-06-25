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

Open point: `windows` is not yet listed in the canonical backend docs. See
`docs/ARCHITECTURE.md` for the backend follow-up.

## Pairing

```http
POST /api/device/pair
```

Payload includes:

- `device_id`
- `device_name`
- `client_type`
- `pairing_token`
- `pair_code`
- `pairing_code`

Response may include `device_token`, `ha_pairing_status`, `message` and
`error`. The app stores only the returned DJConnect bearer token.

## Status

```http
POST /api/djconnect/status
```

Payload includes `device_id`, `device_name`, `client_type` and client version
metadata. Response may include `spotify_configured`, `ask_dj_supported`,
`ask_dj_voice_supported` and `playback`.

HTTP `401`/`403` means stale pairing or invalid token. HTTP `426` means
protocol mismatch and must not be treated as a token failure.

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
`client_id` and `client_message_id`. The app persists `history_revision` and
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
present.

Response `text`, `dj_text` or `message` is rendered as the main answer.
`images[]`, `sources[]`, `items[]`, `playback_actions[]` and
`confirmation_actions[]` are rendered only when they are explicitly present on
the response or message. The client must not reuse previous album art/media,
show a TTS replay button without `audio_url`, expose raw Spotify URIs/backend
IDs in visible text or infer actions from answer text.

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

## Diagnostics And User-Prepared Reports

Feedback, logs and crash reports are local client-generated Markdown/text
exports. They are not sent to Home Assistant and are not uploaded by API. The
client redacts bearer tokens, Authorization headers, pairing codes, bootstrap
proofs, Home Assistant long-lived tokens, push tokens, cookies, secrets,
password/API-key patterns and private URLs before preview, copy or GitHub issue
URL construction.
