# Architecture Decisions

## Use .NET MAUI For Desktop

Decision: use .NET MAUI instead of WPF.

Rationale:

- .NET runs on macOS, but WPF is Windows-only.
- MAUI gives one C# UI/codebase for Windows and macOS.
- It keeps the app native enough for desktop use without Electron.
- It lets the shared Home Assistant contract layer stay UI-framework-light.

Trade-off: macOS builds use Mac Catalyst. If DJConnect later needs deeper
AppKit-specific behavior, a separate native macOS shell could host the same
core contract layer.

## Keep Backend Contracts Typed

Decision: model Home Assistant request/response payloads with C# records and
`System.Text.Json` attributes.

Rationale:

- The Apple app has explicit models for pairing, status, Ask DJ history and
  playback actions.
- Typed payloads keep protocol drift visible during review.
- The app should not construct DJConnect JSON through ad hoc string building.

## Store Only The DJConnect Bearer Token

Decision: keep non-secret settings in JSON and store the bearer token in the
platform credential store.

Rationale:

- Home Assistant owns Spotify OAuth and backend playback credentials.
- Music DNA and Ask DJ history are server-side.
- Windows Credential Manager and macOS Keychain are the appropriate local
  stores for the one app-owned credential.

## Centralize Client Identity

Decision: keep `client_type` and device-id prefix in `DJConnectContract`.

Rationale:

- `windows` is canonical for the shared `3.2.x` app-client contract.
- The Windows device-id prefix is part of the client identity contract.
- Keeping both values central prevents protocol drift across UI and service
  code.

## Publish Platform-Specific Public Releases

Decision: publish unsigned Windows and Mac Catalyst diagnostic artifacts to the
shared public release repository with namespaced tags.

Rationale:

- The Apple app already uses platform tags in
  `pcvantol/djconnect-app-releases`.
- `windows/vX.Y.Z` and `maccatalyst/vX.Y.Z` keep app startup fallback release
  metadata unambiguous.
- EN/NL/DE/FR/ES static What's New files on `djconnect.dev` avoid relying on
  GitHub API availability during normal startup.

Trade-off: these artifacts are useful for diagnostics and internal validation
only until Windows signing/installers and Mac Catalyst notarization are added.

## Centralize Diagnostic Redaction

Decision: use a shared `DiagnosticRedactor` before diagnostics reach storage,
preview, clipboard text or GitHub issue URLs.

Rationale:

- Feedback, crash reports and logs are user-controlled preparation tools, not
  automatic upload surfaces.
- A single redaction path keeps bearer tokens, Authorization headers, pairing
  codes, bootstrap proofs, HA tokens, push tokens, cookies, secrets and private
  URLs out of visible/exported text.
- Tests can guard the privacy contract without needing MAUI UI automation.

## Keep Demo Mode Session-Only

Decision: force Demo Mode off during startup and avoid persisting it as a
connection state.

Rationale:

- Demo Mode must never mask a real stale pairing/token problem across launches.
- It must not call Home Assistant or write credentials.
- A fresh launch should always prefer real pairing/runtime state.

## Gate Wakeword Until There Is A Real Engine

Decision: keep wakeword prompt/settings state, but expose the feature as
unavailable until a real foreground wakeword listener exists.

Rationale:

- The app should not imply background audio capture or hotword detection that is
  not implemented.
- Push-to-talk and typed Ask DJ remain usable without wakeword.
- Future wakeword work needs explicit privacy, permission and lifecycle checks.
