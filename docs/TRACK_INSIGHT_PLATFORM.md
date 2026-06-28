# DJConnect Windows Client - Track Insight Platform

## Vision

Track Insight is a flagship DJConnect feature. It is not a sub-feature of Ask
DJ, even though Ask DJ can open it.

The Home Assistant backend owns music analysis, intent interpretation and cache
state. The Windows client owns the native desktop experience: navigation,
rendering, local presentation state, demo data and future visualization. The
result should feel like a premium Windows desktop feature that explains the
currently playing music clearly, visually and privately.

## Product Architecture

Track Insight is a first-class destination with its own:

- navigation entry;
- screen and view model state;
- shared service interface;
- renderer;
- demo provider;
- future history and settings;
- future desktop visualizer.

Ask DJ is an entry point, not a separate Track Insight renderer.

## Navigation

Track Insight should appear as a top-level sidebar destination alongside:

- Now Playing;
- Track Insight;
- Ask DJ;
- Queue;
- Playlists;
- Settings.

The current Now Playing entry point can remain while the dedicated destination
is introduced incrementally.

## Entry Points

Users can open Track Insight from multiple places:

- Main navigation: open the dedicated Track Insight screen.
- Now Playing: use an Analyze Track / Track Insight button.
- Ask DJ: user questions such as "Tell me about this track", "Analyze this
  track", "What is special about this song?", "Give me Track Insight" and
  "Geef Track Insight voor dit nummer" go through
  `POST /api/djconnect/ask_dj/message`. When the backend responds with
  `intent`, `action`, `type` or `open_screen` set to `track_insight`, the app
  routes to the shared Track Insight UI.
- Auto Track Insight: optional future setting that updates Track Insight when
  the foreground app detects a new playing track.

Ask DJ must not display a separate legacy Track Analysis UI. It should deep-link
or hydrate the shared Track Insight surface with the backend-provided
`track_insight` payload.

## Shared Engine

There must be one Windows Track Insight implementation. Main navigation, Now
Playing, Ask DJ and future Auto Track Insight all use the same:

- `TrackInsightRequest` and `TrackInsightResponse` models;
- `TrackInsightResult` parser/deserializer;
- `TrackInsightPresentation` renderer model;
- Home Assistant service call;
- demo provider data shape;
- visual profile mapping;
- history/cache policy.

The UI must not know whether data came from Home Assistant or Demo Mode.

## Backend Contract

Direct Track Insight uses:

```text
POST /api/djconnect/track_insight
```

The client sends Home Assistant auth and explicit track fields when available:

- `title`;
- `artist`;
- `album`;
- optional `entity_id`;
- optional `player_id`;
- optional `music_backend`;
- `locale`;
- `force_refresh`;
- `include_visual_profile`.

If explicit metadata is missing, Home Assistant resolves Now Playing. The
client renders `no_track_playing` as an empty state.

The normalized response is `track_insight`:

- `track`;
- `analysis`;
- `music_dna`;
- `visual_profile`;
- `cache`.

Music DNA Match is read from `track_insight.music_dna.match_percent`.
`visual_profile` is rendering hints only. The client must not expect
server-generated images or video.

## Ask DJ Contract

Track Insight intent phrases are sent through the normal Ask DJ endpoint:

```text
POST /api/djconnect/ask_dj/message
```

When a response has `intent`, `action`, `type` or `open_screen` equal to
`track_insight`, the app renders or routes to Track Insight using the included
`track_insight` payload.

Track Insight Ask DJ responses are read-only unless the backend explicitly
returns `playback_actions[]`. The app must not synthesize Play Now buttons,
reuse previous artwork/media or infer actions from prose.

## Windows Track Insight Screen

The dedicated Windows screen should contain:

- album artwork;
- title, artist and album;
- Music DNA Match;
- vibe and "why it fits you";
- backend-provided analysis sections;
- timeline when present;
- DJ tips when present;
- limitations and provider diagnostics when present;
- visual profile hints;
- cache state;
- future history section;
- future favorites section;
- future visualizer.

Fields such as BPM, key, genre, mood, energy, danceability, intensity, vibe and
texture are rendered only when the backend provides them through
`track_insight.analysis` or compatible section items. The client never invents
analysis values.

## Parser Policy

Windows parses structured JSON from Home Assistant. If the backend returns only
plain answer text, the app may show that text as an Ask DJ answer or empty Track
Insight state, but it must not parse prose to infer BPM, key, genre, timestamps,
structure labels, tips or Music DNA.

This keeps Windows aligned with the HA integration v3.2.2 contract and avoids
client-side disagreement with the backend.

## Visualization

Track Insight should eventually include a deterministic Windows visualizer.
The same Track Insight payload should always render the same visual profile.
Never randomize persistent visual identity.

Input:

- `track_insight.music_dna`;
- `track_insight.analysis`;
- `track_insight.visual_profile`;
- stable track identity.

Output:

- color palette;
- glow intensity;
- pulse speed;
- waveform style;
- particle density;
- motion style;
- spectrum profile.

Implementation options for .NET MAUI desktop:

- MAUI `GraphicsView` for a cross-platform canvas;
- WinUI/Composition-specific acceleration for Windows where beneficial;
- lightweight Mac Catalyst equivalent for shared desktop behavior.

Respect:

- Reduce Motion;
- power/battery state where available;
- background/hidden view lifecycle;
- high contrast and accessibility settings;
- dark mode.

Animations pause when the app is backgrounded, music is stopped or the
visualizer is hidden.

## External Display

Windows should treat external display support as a future "VibeCast" mode.
This is not required for the first Track Insight platform step.

Future behavior:

- detect additional displays;
- allow a dedicated visualizer window on another screen;
- keep the main window as controller;
- fall back to normal desktop window movement/fullscreen.

AirPlay-specific behavior belongs to Apple platforms; Windows should express
the same product idea through external monitor and fullscreen support.

## Auto Track Insight

Auto Track Insight is off by default.

After a user successfully opens Track Insight, the app may ask:

> Would you like DJConnect to automatically analyze every new track while the
> app is open?

Settings:

- Track Insight;
- Auto Track Insight;
- description: "Automatically analyze each new track while DJConnect is open."

Only active when:

- app is foreground;
- music is playing;
- track identity changes;
- analysis is not already cached or visible;
- debounce has elapsed.

Avoid duplicate analysis calls.

## Demo Mode

Track Insight must work in Demo Mode without Home Assistant.

Demo Mode should support:

- direct Track Insight;
- Ask DJ Track Insight intent;
- future Auto Track Insight;
- future visualizer;
- navigation.

Create a service boundary:

- `ITrackInsightService`;
- `HomeAssistantTrackInsightService`;
- `DemoTrackInsightService`.

The view model consumes the interface and does not branch on provider details
outside demo/session setup.

Demo tracks should include at least 10 varied examples, covering electronic,
ambient, pop, rock and metal so the visual profile can demonstrate range.

## Privacy

Preferred wording:

- Rendered privately on your device.
- Private by design.
- Home Assistant generates the analysis; DJConnect renders it locally.

Do not claim Apple Intelligence, Windows Copilot or any other platform AI
unless that framework is actually used.

## Performance

Targets:

- smooth desktop interaction;
- stable frame pacing for visualizer work;
- minimal CPU while idle;
- no animation work while hidden/backgrounded;
- bounded local history/cache storage.

The first implementation can be a static premium renderer. GPU-oriented
visualization should be introduced behind clear performance and accessibility
checks.

## Incremental Delivery

1. Keep the current direct Now Playing Track Insight entry point.
2. Add a top-level Track Insight sidebar destination.
3. Move Ask DJ Track Insight responses to hydrate/deep-link to that destination.
4. Introduce `ITrackInsightService` with Home Assistant and Demo providers.
5. Add local demo tracks and deterministic visual profile mapping.
6. Add Auto Track Insight as an off-by-default setting.
7. Add the animated visualizer after accessibility and performance guardrails
   are in place.
