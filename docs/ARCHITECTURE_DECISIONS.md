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
- DJ Memory and Ask DJ history are server-side.
- Windows Credential Manager and macOS Keychain are the appropriate local
  stores for the one app-owned credential.

## Centralize Client Identity

Decision: keep `client_type` and device-id prefix in `DJConnectContract`.

Rationale:

- Windows support is not yet documented in the backend contract.
- Changing the canonical spelling or prefix should not require sweeping the UI
  and service code.

## Publish Platform-Specific Public Releases

Decision: publish unsigned Windows and Mac Catalyst diagnostic artifacts to the
shared public release repository with namespaced tags.

Rationale:

- The Apple app already uses platform tags in
  `pcvantol/djconnect-app-releases`.
- `windows/vX.Y.Z` and `maccatalyst/vX.Y.Z` keep app startup fallback release
  metadata unambiguous.
- EN/NL static What's New files on `djconnect.dev` avoid relying on GitHub API
  availability during normal startup.

Trade-off: these artifacts are useful for diagnostics and internal validation
only until Windows signing/installers and Mac Catalyst notarization are added.
