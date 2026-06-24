# Security

Report private security issues to `security@djconnect.dev`.

## Secret Handling

- Do not log bearer tokens, Authorization headers, Spotify OAuth tokens,
  passwords, Home Assistant long-lived tokens or raw secret-bearing payloads.
- Store the DJConnect bearer token only in Windows Credential Manager or macOS
  Keychain.
- Keep local JSON settings non-secret.
- Treat `401`/`403` from authenticated DJConnect routes as stale pairing.
- Treat HTTP `426` protocol mismatch as an update-required condition, not as a
  token failure.

## Scope

This repo contains a desktop client. Home Assistant owns Spotify OAuth,
backend playback, DJ Memory, Ask DJ history and Assist/TTS.
