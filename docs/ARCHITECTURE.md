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

## Runtime Flow

1. The app creates or loads a stable install id.
2. The install id becomes `djconnect-windows-XXXXXXXXXXXX`.
3. The user configures a Home Assistant URL and either enters an existing token
   or pairs with a Home Assistant pairing code.
4. Runtime status is posted to `POST /api/djconnect/status`.
5. Ask DJ uses server-side history and message endpoints.
6. Playback actions and follow-up buttons are sent through
   `POST /api/djconnect/command`.

## Release Artifacts

The source repo release tag is `vX.Y.Z`. The public unsigned release workflow
publishes diagnostic artifacts to `pcvantol/djconnect-app-releases` with
platform-specific tags:

- `windows/vX.Y.Z`
- `maccatalyst/vX.Y.Z`

The same workflow publishes startup What's New content as static files on
`djconnect.dev`:

- `/release-notes/windows/{en|nl}/vX.Y.Z.json`
- `/release-notes/maccatalyst/{en|nl}/vX.Y.Z.json`

These artifacts are not signed Windows installers and not notarized Mac apps.

## Ask DJ And History

Ask DJ is backend-owned. The app sends user text to
`POST /api/djconnect/ask_dj/message`, renders returned `user_message`,
`assistant_message`, `playback_actions`, `confirmation_actions` and `items`,
and syncs history through `GET /api/djconnect/ask_dj/history`.

`history_revision` and `clear_revision` are persisted as local sync cursors.
If the server clear revision advances, local cached display messages are
cleared. Server trim metadata must be honored without parsing the text of
retention system messages.

## Client Type Open Point

The current Home Assistant docs list `esp32`, `ios`, `macos`, `watchos` and
`raspberry_pi`. This repo uses `windows` centrally in
`DJConnectContract.ClientType` so the value can change in one place if the
backend contract adopts another spelling.

Minimal backend/doc follow-up:

- add `windows` as an app client type;
- accept `djconnect-windows-XXXXXXXXXXXX` device IDs;
- hide ESP-only OTA/hardware entities for Windows clients.
