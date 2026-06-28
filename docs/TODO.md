# TODO

- Validate the `3.2.2` local-pairing and remote-fallback upgrade against a live
  Home Assistant DJConnect `3.2.x` backend.
- Run the Home Assistant `FIELD_TEST_APP_CLIENTS.md` checklist for Windows and
  record app build, HA version, DJConnect integration version, backend
  (`spotify_direct` or `music_assistant`), local pairing result, remote command
  result and any HA Repair issue.
- Field-test Music Assistant unsupported capability responses and
  `stale_backend_action` refresh behavior from real Ask DJ playback actions.
- Field-test Ask DJ `links[]`/`sources[]`, mood-zone recommendations and
  `audio_response:auto|always|never` behavior against a live HA instance.
- Validate EN/NL What's New publication on `djconnect.dev` during the next
  website release-note smoke test.
- Extend tests for stale-pairing handling and any future real wakeword engine.
- Validate feedback, crash-report and logs redaction with manual UI smoke tests
  on Windows.
- Validate Playlists, Queue and Ask DJ action execution against a live Home
  Assistant `djconnect` backend.
- Validate generated platform app icons in installed Windows and Mac Catalyst
  bundles.
- Add Windows packaging/signing and Mac Catalyst signing/notarization plan.
- Smoke-test Music Assistant and Spotify Direct playback actions without
  Spotify-only UI assumptions.
