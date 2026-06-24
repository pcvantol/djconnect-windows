# Privacy

DJConnect desktop is a thin client for the Home Assistant `djconnect`
integration.

The app may store locally:

- stable install identity;
- non-secret sync cursors such as `history_revision` and `clear_revision`;
- non-secret app preferences such as onboarding, What's New, permissions,
  diagnostics level, wakeword prompt dismissal and local mini-game highscores;
- bounded redacted diagnostic log entries;
- the DJConnect bearer token in the platform credential store.

The app must not store as source of truth:

- Spotify credentials or OAuth tokens;
- Home Assistant long-lived access tokens;
- DJ Memory;
- Ask DJ server history;
- raw audio recordings;
- full prompts or secret-bearing backend responses.

Ask DJ text and returned messages are displayed in the app, but server-side
history remains owned by Home Assistant. Local display caches must clear when
the backend clear revision advances or pairing becomes stale.

Logs, feedback bodies and crash reports are prepared locally and are redacted
before preview, copy, storage or GitHub issue URL creation. DJConnect does not
upload diagnostics automatically. Opening a GitHub issue only opens a browser
with redacted text prefilled; the user decides whether to submit it.

Demo Mode is local and session-only. It uses sample data and must not call Home
Assistant, advertise mDNS or write credentials.

The Privacy screen must not display device serials, account names, hostnames,
bearer tokens, pairing codes or private Home Assistant URLs.
