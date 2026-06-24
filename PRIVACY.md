# Privacy

DJConnect desktop is a thin client for the Home Assistant `djconnect`
integration.

The app may store locally:

- Home Assistant base URL;
- stable install identity;
- non-secret sync cursors such as `history_revision` and `clear_revision`;
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
