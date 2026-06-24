# DJConnect Desktop Chat Bootstrap Prompt

Use this prompt to initialize a fresh AI/Codex chat for this repository.

```text
Werk in repo `/Users/pcvantol/Documents/GitHub/djconnect-windows`.

Lees eerst:
- README.md
- docs/HANDOFF.md
- docs/ARCHITECTURE.md
- docs/ARCHITECTURE_DECISIONS.md
- docs/API_CONTRACT.md
- docs/TECHNICAL_DESIGN_DECISIONS.md
- docs/DEVELOPMENT.md
- docs/TODO.md
- docs/ISSUES.md
- SECURITY.md
- PRIVACY.md

Belangrijke regels:
- Dit is een .NET MAUI desktop app voor Windows en macOS.
- Huidige desktop app release: `3.1.0`.
- Home Assistant blijft eigenaar van pairing, Spotify OAuth/backend playback,
  Ask DJ history, DJ Memory, Assist/TTS en command execution.
- De app bewaart geen Spotify credentials, OAuth tokens, DJ Memory of Ask DJ
  server history als bron van waarheid.
- Het enige app-owned credential is de DJConnect bearer token in Windows
  Credential Manager of macOS Keychain.
- `client_type: "windows"` is centraal configureerbaar maar nog een backend
  contract open punt.
- Geen secrets/tokens/wachtwoorden loggen of committen.
- Gebruik `rg` voor zoeken en `apply_patch` voor handmatige edits.
```
