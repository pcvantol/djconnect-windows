# Windows Profile Adoption Report

Epic 3B Phase 3 brings the Windows client onto the canonical Profile Platform
contract used by the Apple reference implementation. Home Assistant remains the
authority for Profile resolution, Music DNA, Ask DJ history, recommendations,
privacy policy and backend routing. Windows sends request context and renders
the response.

## Parity Checklist

| Area | Classification | Notes |
| --- | --- | --- |
| Network request context | Same | Profile-aware requests can carry `profile_id`, `session_id`, `private_session` and `request_source` with existing `device_id` and `client_type`. Existing device-mapped clients may omit `profile_id`. |
| Ask DJ | Same | Windows sends canonical context on Ask DJ text/voice requests and decodes `profile_id`, `music_dna_key`, `resolved_profile`, `resolution` and privacy metadata. Conversation ownership remains backend-side. |
| Music DNA | Same | Requests carry Profile context; responses decode canonical Profile metadata. Music DNA content is still rendered only from backend fields. |
| Discover | Same | Feed, refresh and play request shapes carry Profile context. Recommendations remain backend-owned. |
| Track Insight | Same | Track Insight requests carry Profile context and render backend analysis only. |
| Capability discovery | Same transport behavior, partial display | Windows already uses `djconnect/capabilities` for websocket feature detection and now preserves Profile Platform context on websocket payloads. The settings UI displays capability support from backend summaries. |
| Private Session | Same contract support | Windows can send `private_session`; backend decides persistence suppression. Persistence-only local behavior is not introduced. |
| Cache isolation | Different by design for this phase | Windows persists only sync cursors and bounded logs today; no durable recommendation/Music DNA cache is introduced. Future explicit Profile switching should key any display caches by Profile. |
| Profile switching | Missing by design | Contract fields are ready, but Windows does not add Profile CRUD or a switching UI until Home Assistant exposes current/selectable Profile state for clients. |
| Settings | Same within available contract | Settings display Current Profile, Current Household placeholder, Current Backend, Current Music Account target, Privacy Mode and capability support. Household details are not invented locally. |
| Error handling | Same | Structured Profile Platform errors map to repair/setup guidance and are not treated as stale pairing by default. |

## Implemented

- Added shared Profile context/metadata models:
  `DJConnectProfileContext`, `DJConnectResolvedProfile`,
  `DJConnectProfileResolution` and `DJConnectProfilePrivacy`.
- Added canonical Profile fields to Ask DJ, voice, Track Insight, Music DNA,
  Discover, command/status response and history response models.
- Added Profile context propagation to HTTP requests, multipart voice requests,
  query-string Discover requests and websocket fast-path payloads.
- Added localized Profile Platform error guidance for:
  `profile_required`, `invalid_profile`, `device_not_mapped`,
  `profile_backend_missing`, `profile_music_account_missing`,
  `profile_backend_account_mismatch`, `profile_access_denied`,
  `private_session_restriction` and `invalid_request_context`.
- Updated Settings diagnostics to render resolved Profile, Household placeholder,
  backend, music target account/player, privacy mode and capabilities without
  adding Profile CRUD.
- Added regression tests for canonical Windows Profile request generation,
  Profile response metadata decoding and Profile error localization.

## Deferred

- Profile CRUD, Household management, export/import and profile switching UI.
- Household display beyond a placeholder until the backend response exposes a
  safe household summary.
- Cache-key migration for future explicit Profile switching; current Windows
  state is limited and does not own backend intelligence caches.
- Negative Discover feedback and Profile export controls, which remain outside
  this phase.

## Differences From Apple

- Apple is the reference implementation. Windows follows the same contract but
  presents Profile metadata in the desktop Settings diagnostics surface.
- Windows does not introduce an Apple-style profile selector yet. This is
  intentional because Windows does not own Profile lists or Profile CRUD.
- Windows websocket fast path remains a local opt-in transport, matching the
  existing Windows transport policy.

## Remaining Parity Gaps

- Add explicit Profile selection only after Home Assistant exposes selectable
  Profile state and capabilities for client switching.
- Key future display caches by resolved `profile_id` and bypass persistence when
  `private_session` is active.
- Expand capability UI when the backend advertises richer Profile Platform
  capability details over HTTP/status responses.

## Recommendations For Raspberry Pi Adoption

- Start with an Apple/Windows parity review before implementation.
- Treat Pi as an Ambient Client, defaulting to shared, room or household
  Profiles rather than personal Profile assumptions.
- Reuse the same request context fields and structured Profile errors.
- Keep Ask DJ history read-only or limited unless the resolved Profile is
  explicitly shared-safe.
- Avoid any local recommendation, Music DNA or mood ownership on Pi.
