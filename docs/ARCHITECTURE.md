# Architecture

DJConnect is a native desktop client for the Home Assistant `djconnect`
integration. It is not an ESP32 emulator and does not own playback credentials;
it is a first-class app client identified by `client_type`.

## Trust Boundary

Home Assistant owns:

- pairing and DJConnect bearer-token lifecycle;
- Spotify OAuth and playback backend credentials;
- Ask DJ intent interpretation, DJ Memory and server-side chat history;
- playback commands and follow-up confirmation state;
- Assist/STT/TTS and native Home Assistant entities.

The desktop app owns:

- native Windows/macOS UI through .NET MAUI;
- local non-secret app settings;
- stable app install identity and `device_id`;
- app-private storage for the DJConnect bearer token only;
- rendering Ask DJ messages, actions, recent-played lists and status.

The app must never store Spotify credentials, Home Assistant long-lived access
tokens, OAuth refresh tokens, DJ Memory or Ask DJ history as the source of
truth.

## Targets

`DJConnect.Windows`

Single-project .NET MAUI app targeting:

```text
net10.0-windows10.0.19041.0
net10.0-maccatalyst
```

The project contains the MAUI shell, view model, typed Home Assistant client,
request/response models, settings store, platform credential storage and
platform entrypoints for Windows and Mac Catalyst.

The UI follows the macOS app's desktop flow: a persistent sidebar navigates to
Now Playing, Ask DJ, Queue, Playlists, Settings, Privacy, Logs, Feedback,
Mini-games and About/Legal sections, while the selected workflow renders in the
detail area. Pairing and Home Assistant configuration live in the desktop
connection flow rather than in a mobile-style tab bar.

## Runtime Flow

1. The app creates or loads a stable install id.
2. The install id becomes `djconnect-windows-XXXXXXXXXXXX`.
3. First launch shows the interactive welcome wizard until
   `DJConnectWelcomeSeen` is stored locally. While onboarding is visible, the
   app does not advertise `_djconnect._tcp`.
4. After onboarding is dismissed, the local Client API starts on a LAN-reachable
   random port and the pairing screen shows Client adres plus a short pairing
   code.
5. The app advertises `_djconnect._tcp` only while pairable: not onboarding, not
   demo mode, not paired and local Client API running.
6. Home Assistant calls `GET /api/device/pairing-info` and then
   `POST /api/device/pair` on the local Client API.
7. On successful pairing, the app stores only the Home Assistant-issued
   DJConnect device token in app-private credential storage, stops mDNS
   advertising and enters runtime UI.
8. Runtime status is posted to `POST /api/djconnect/status`; the Now Playing
   page renders playback, output devices and compatibility state from that
   response.
9. Now Playing controls send only generic commands such as `toggle_playback`,
   `previous_track`, `next_track`, `seek`, `volume` and `select_output` through
   `POST /api/djconnect/command`.
10. Queue renders backend-provided queue state from supported status shapes
   (`queue`, `items`, `queue_items` and collection envelopes). Items are
   normalized, deduplicated by stable id/signature and capped at 100 rendered
   rows. Starting a queue item sends only a generic `queue_item_play` command or
   a backend-returned playback action.
11. Ask DJ uses server-side history and message endpoints. Text chat always
    goes to `POST /api/djconnect/ask_dj/message`; voice/PTT capture is reserved
    for `POST /api/djconnect/voice` with `audio/wav` once a platform capture
    backend exists. The Windows client merges server messages by stable id,
    preserves pending local user bubbles, honors `clear_revision`, prunes from
    trim metadata and renders system messages, optional audio replay,
    response-owned images/sources/items and backend-returned actions without
    client-side intent, memory, follow-up or playback reconstruction.
12. Playback actions and follow-up buttons are sent through
   `POST /api/djconnect/command`.
13. Playlists render backend-provided playlist shapes (`playlists`,
    `playlist_items` and collection envelopes), normalize title/subtitle/artwork
    aliases, dedupe deterministically, cap rendering at 100 rows and start a
    selected playlist only through a generic `playlist_start` command or
    backend-returned action.
14. Logs, feedback bodies and crash reports are redacted before persistence,
    preview, clipboard copy or issue URL creation. The app never uploads these
    diagnostics automatically.

## Desktop Utility Flows

- Settings groups connection, output/playback, Ask DJ, Demo Mode, permissions,
  diagnostics and app metadata. It never asks for bearer tokens, Spotify
  credentials, private Home Assistant URLs as feedback text or removed Spotify
  override settings.
- Logs/Diagnostics store redacted structured entries with timestamp, level, id
  and message. Storage is bounded to 500 persisted entries and 120 visible
  rows.
- Feedback and Crash report screens are local preparation tools. Users can
  preview and edit the generated Markdown, copy it to the clipboard or open a
  prefilled GitHub issue URL. Submission remains manual.
- Privacy explains local data, Home Assistant-owned state, permissions and
  deletion/reset actions without exposing private identifiers.
- Demo Mode is session-only. It loads local sample status, queue, playlists and
  Ask DJ responses only after explicit start, disables mDNS/Home Assistant
  calls and clears demo runtime state when stopped.
- Wakeword settings and prompt state exist, but the feature is gated off until
  a real foreground wakeword engine is available. Push-to-talk and text Ask DJ
  remain available without wakeword.

## Local Pairing API And mDNS

The local Client API exposes:

- `GET /api/device/pairing-info`
- `GET /api/device/info`
- `POST /api/device/pair`

mDNS service type is `_djconnect._tcp`. TXT records contain only
non-secret local pairing metadata:

- `client_type=windows`
- `platform=windows`
- `device_id=<stable install id>`
- `device_name=<friendly device name>`
- `firmware=<protocol line>`
- `app_version=<app version>`
- `pairing_status=unpaired|pairing|paired`
- `paired=false` while pairable
- `pair_code=<current pairing code>` only while pairable
- `local_url=<http://lan-ip:port>`

Tokens, Spotify credentials, Home Assistant long-lived tokens, account names,
hostnames, Wi-Fi SSIDs and private backend URLs must never be logged or exposed
through TXT records. Reset pairing clears the DJConnect device token, rotates
the install id and pairing code, returns to the pairing screen and re-enables
pairable mDNS.

## Release Artifacts

The source repo release tag is `vX.Y.Z`. The public unsigned release workflow
publishes diagnostic artifacts to `pcvantol/djconnect-app-releases` with
platform-specific tags:

- `windows/vX.Y.Z`
- `maccatalyst/vX.Y.Z`

The Windows public release contains separate `x64` and `arm64` diagnostic
zips. The ARM64 artifact targets Windows on ARM, including Parallels Windows
VMs running on Apple Silicon Macs.

The same workflow publishes startup What's New content as static files on
`djconnect.dev`:

- `/release-notes/windows/{en|nl}/vX.Y.Z.json`
- `/release-notes/maccatalyst/{en|nl}/vX.Y.Z.json`

These artifacts are not signed Windows installers and not notarized Mac apps.

## Ask DJ And History

Ask DJ is backend-owned. The app sends user text to
`POST /api/djconnect/ask_dj/message`, renders returned `user_message`,
`assistant_message`, `messages[]`, `text`/`dj_text`/`message`, `images`,
`sources`, `playback_actions`, `confirmation_actions` and `items`, and syncs
history through `GET /api/djconnect/ask_dj/history`.

`history_revision` and `clear_revision` are persisted as local sync cursors.
If the server clear revision advances, local cached display messages are
cleared. Server trim metadata must be honored without parsing the text of
retention system messages.

The client never adds Play Now buttons, album art, TTS replay buttons, sources
or action behavior by inspecting answer text. Playback/follow-up buttons exist
only when `playback_actions[]` or `confirmation_actions[]` is present. Raw
Spotify URIs and backend IDs are kept out of visible answer text.

## Client Type Open Point

The current Home Assistant docs list `esp32`, `ios`, `macos`, `watchos` and
`raspberry_pi`. This repo uses `windows` centrally in
`DJConnectContract.ClientType` so the value can change in one place if the
backend contract adopts another spelling.

Minimal backend/doc follow-up:

- add `windows` as an app client type;
- accept `djconnect-windows-XXXXXXXXXXXX` device IDs;
- hide ESP-only OTA/hardware entities for Windows clients.
