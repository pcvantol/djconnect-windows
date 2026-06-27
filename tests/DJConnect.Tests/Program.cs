using System.Text.Json;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Services;

var tests = new (string Name, Action Run)[]
{
    ("Client identity uses windows contract", ClientIdentityUsesWindowsContract),
    ("Client identity pads short install IDs", ClientIdentityPadsShortInstallIds),
    ("Pairing payload serializes HA compatibility fields", PairingPayloadSerializesCompatibilityFields),
    ("Status payload serializes app protocol metadata", StatusPayloadSerializesAppProtocolMetadata),
    ("Ask DJ request serializes server-side message contract", AskDJRequestSerializesServerSideContract),
    ("Ask DJ response deserializes exchange messages", AskDJResponseDeserializesExchangeMessages),
    ("Ask DJ response deserializes media sources links and dj text", AskDJResponseDeserializesMediaSourcesLinksAndDjText),
    ("Ask DJ technical track analysis v2 renders sections timeline and tips", AskDJTechnicalTrackAnalysisV2RendersSectionsTimelineAndTips),
    ("Ask DJ technical track analysis renders MetaBrainz metadata context separately", AskDJTechnicalTrackAnalysisRendersMetaBrainzMetadataContextSeparately),
    ("Ask DJ technical track analysis without metadata remains compatible", AskDJTechnicalTrackAnalysisWithoutMetadataRemainsCompatible),
    ("Ask DJ technical track analysis providers render as diagnostics", AskDJTechnicalTrackAnalysisProvidersRenderAsDiagnostics),
    ("Ask DJ technical track analysis MetaBrainz provider statuses remain diagnostics", AskDJTechnicalTrackAnalysisMetaBrainzProviderStatusesRemainDiagnostics),
    ("Ask DJ technical track analysis without providers remains compatible", AskDJTechnicalTrackAnalysisWithoutProvidersRemainsCompatible),
    ("Ask DJ technical track analysis tolerates unknown providers", AskDJTechnicalTrackAnalysisToleratesUnknownProviders),
    ("Ask DJ technical track analysis unavailable renders skipped provider diagnostics", AskDJTechnicalTrackAnalysisUnavailableRendersSkippedProviderDiagnostics),
    ("Ask DJ technical track analysis v2 without timeline renders sections", AskDJTechnicalTrackAnalysisV2WithoutTimelineRendersSections),
    ("Ask DJ technical track analysis unavailable renders limitations", AskDJTechnicalTrackAnalysisUnavailableRendersLimitations),
    ("Ask DJ technical track analysis v1 fallback renders measured inferred and limitations", AskDJTechnicalTrackAnalysisV1FallbackRendersMeasuredInferredAndLimitations),
    ("Ask DJ technical track analysis renders unknown values generically", AskDJTechnicalTrackAnalysisRendersUnknownValuesGenerically),
    ("Ask DJ technical track analysis without playback actions has no stale buttons", AskDJTechnicalTrackAnalysisWithoutPlaybackActionsHasNoStaleButtons),
    ("Ask DJ message presentation supports system audio and confirmations", AskDJMessagePresentationSupportsSystemAudioAndConfirmations),
    ("Ask DJ history deserializes revisions trim metadata and recent items", AskDJHistoryDeserializesRevisionsTrimMetadataAndRecentItems),
    ("Playback action deserializes confirmation command", PlaybackActionDeserializesConfirmationCommand),
    ("Playback action detects backend play now recommendations", PlaybackActionDetectsBackendPlayNowRecommendations),
    ("Playback action detects save current track control", PlaybackActionDetectsSaveCurrentTrackControl),
    ("Playback command payload stays generic", PlaybackCommandPayloadStaysGeneric),
    ("Save current track command payload uses backend contract", SaveCurrentTrackCommandPayloadUsesBackendContract),
    ("Diagnostic redaction removes secrets", DiagnosticRedactionRemovesSecrets),
    ("Version compatibility enforces app protocol minor", VersionCompatibilityEnforcesAppProtocolMinor),
    ("Queue normalization deduplicates and limits items", QueueNormalizationDeduplicatesAndLimitsItems),
    ("Playlist normalization supports aliases dedupe and limits", PlaylistNormalizationSupportsAliasesDedupeAndLimits),
    ("Pairing code generator creates six digits", PairingCodeGeneratorCreatesSixDigits),
    ("Welcome seen flag defaults to first launch", WelcomeSeenFlagDefaultsToFirstLaunch),
    ("Whats New last seen version defaults empty", WhatsNewLastSeenVersionDefaultsEmpty),
    ("Crash report flags default to clean first launch", CrashReportFlagsDefaultToCleanFirstLaunch),
    ("Wakeword defaults to disabled and not dismissed", WakewordDefaultsToDisabledAndNotDismissed),
    ("Demo mode defaults to session off", DemoModeDefaultsToSessionOff),
    ("Monkey test mode is explicit and environment driven", MonkeyTestModeIsExplicitAndEnvironmentDriven),
    ("Diagnostic log preferences are bounded by default", DiagnosticLogPreferencesAreBoundedByDefault),
    ("Permission explanation flags default to unseen", PermissionExplanationFlagsDefaultToUnseen),
    ("Protocol 3.2 uses local-only pairing transport", Protocol32UsesLocalOnlyPairingTransport),
    ("Protocol 3.2 falls back from local to remote after pairing", Protocol32FallsBackFromLocalToRemoteAfterPairing),
    ("Protocol 3.2 marks offline when no HA URL is reachable", Protocol32MarksOfflineWhenNoUrlReachable),
    ("Protocol 3.2 parses backend summary", Protocol32ParsesBackendSummary),
    ("Protocol 3.2 parses safe backend error object", Protocol32ParsesSafeBackendErrorObject),
    ("Backend-aware actions preserve Music Assistant value", BackendAwareActionsPreserveMusicAssistantValue),
    ("Backend-aware actions carry backend revision", BackendAwareActionsCarryBackendRevision),
    ("Backend error responses deserialize stale and unsupported contracts", BackendErrorResponsesDeserializeStaleAndUnsupportedContracts),
    ("Protocol 3.2 keeps app local API and mDNS inactive", Protocol32KeepsAppLocalApiAndMdnsInactive)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"ok - {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"not ok - {test.Name}");
        Console.Error.WriteLine(ex.Message);
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} test(s) failed.");
    return 1;
}

Console.WriteLine($"{tests.Length} test(s) passed.");
return 0;

static void ClientIdentityUsesWindowsContract()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", " Studio PC ");

    AssertEqual("abc123def4567890", identity.InstallId);
    AssertEqual(DJConnectContract.ClientType, identity.ClientType);
    AssertEqual("Studio PC", identity.DeviceName);
    AssertEqual("djconnect-windows-ABC123DEF456", identity.DeviceId);
}

static void ClientIdentityPadsShortInstallIds()
{
    var identity = ClientIdentity.CreateOrLoad("a-b", "pc");

    AssertEqual("djconnect-windows-AB0000000000", identity.DeviceId);
}

static void PairingPayloadSerializesCompatibilityFields()
{
    var payload = new PairingPayload(
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "123456",
        "123456",
        "123456");

    using var document = JsonSerializer.SerializeToDocument(payload);
    var root = document.RootElement;

    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("device_id").GetString());
    AssertEqual("Studio PC", root.GetProperty("device_name").GetString());
    AssertEqual("windows", root.GetProperty("client_type").GetString());
    AssertEqual("123456", root.GetProperty("pairing_token").GetString());
    AssertEqual("123456", root.GetProperty("pair_code").GetString());
    AssertEqual("123456", root.GetProperty("pairing_code").GetString());
}

static void StatusPayloadSerializesAppProtocolMetadata()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildStatusPayload(identity);
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"client_type\":\"windows\""), "status must include Windows client type");
    AssertTrue(serialized.Contains("\"firmware\":\"windows-app\""), "status must identify the app surface as firmware metadata for HA compatibility");
    AssertTrue(serialized.Contains("\"app_version\":\"3.2.1\""), "status must include app version");
    AssertTrue(serialized.Contains("\"protocol_version\":\"3.2\""), "status must include protocol line");
}

static void AskDJRequestSerializesServerSideContract()
{
    var request = new AskDJRequest(
        "msg-1",
        "djconnect-windows-ABC123DEF456",
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "Welke nummers hoorde ik net?",
        "Welke nummers hoorde ik net?",
        Mood: 72,
        AppVersion: DJConnectContract.AppVersion,
        ProtocolVersion: DJConnectContract.ProtocolLine);

    using var document = JsonSerializer.SerializeToDocument(request);
    var root = document.RootElement;

    AssertEqual("msg-1", root.GetProperty("client_message_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("client_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("device_id").GetString());
    AssertEqual("windows", root.GetProperty("client_type").GetString());
    AssertEqual("Welke nummers hoorde ik net?", root.GetProperty("text").GetString());
    AssertEqual("Welke nummers hoorde ik net?", root.GetProperty("message").GetString());
    AssertEqual("auto", root.GetProperty("audio_response").GetString());
    AssertEqual(72, root.GetProperty("mood").GetInt32());
    AssertEqual("3.2.1", root.GetProperty("app_version").GetString());
    AssertEqual("3.2", root.GetProperty("protocol_version").GetString());
}

static void AskDJResponseDeserializesExchangeMessages()
{
    const string json = """
    {
      "success": true,
      "history_revision": 44,
      "messages": [
        {
          "id": "user-1",
          "client_message_id": "client-1",
          "exchange_id": "exchange-1",
          "exchange_order": 0,
          "history_revision": 43,
          "role": "user",
          "text": "heb je playlists van snowpatrol"
        },
        {
          "id": "assistant-1",
          "client_message_id": "client-1",
          "exchange_id": "exchange-1",
          "exchange_order": 1,
          "history_revision": 44,
          "role": "assistant",
          "text": "Ik zoek iets passends voor Snow Patrol."
        }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJMessageResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertTrue(response!.Success, "message response should be successful");
    AssertNotNull(response.Messages);
    AssertEqual(2, response.Messages!.Count);
    AssertEqual("exchange-1", response.Messages[0].ExchangeId);
    AssertEqual(0, response.Messages[0].ExchangeOrder);
    AssertEqual(1, response.Messages[1].ExchangeOrder);
    AssertEqual("client-1|user", (response.Messages[0] with { Id = null }).StableKey);
    AssertEqual("client-1|assistant", (response.Messages[1] with { Id = null }).StableKey);
}

static void AskDJMessagePresentationSupportsSystemAudioAndConfirmations()
{
    var system = new AskDJMessage("sys-1", "assistant", "History trimmed", null, DateTimeOffset.Now, "system", null, null, null, null, null, Origin: "history_retention");
    var audio = new AskDJMessage("a-1", "assistant", "Luister nog eens.", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, "http://127.0.0.1/audio.wav");
    var yes = new PlaybackAction("yes", "confirmation", "ask_dj_followup_response", null, null, null, null, null, null, null, "yes", "confirmation", "yes");
    var no = yes with { Id = "no", Value = "no", ResponseValue = "no" };

    AssertTrue(system.IsSystem, "system messages must render as system");
    AssertTrue(!system.IsUser, "system messages must not render as user bubbles");
    AssertTrue(audio.HasAudio, "assistant messages with audio_url must show replay UI");
    AssertEqual("Ja", yes.DisplayLabel);
    AssertEqual("Nee", no.DisplayLabel);
}

static void AskDJResponseDeserializesMediaSourcesLinksAndDjText()
{
    const string json = """
    {
      "success": true,
      "dj_text": "Dit komt uit DJConnect Memory.",
      "images": [
        { "image_url": "https://example.invalid/cover.jpg", "title": "Cover" }
      ],
      "sources": [
        { "id": "djconnect_memory", "label": "djconnect_memory" },
        { "source": "metabrainz_metadata" }
      ],
      "links": [
        { "source": "bandsintown", "label": "Concertagenda", "url": "https://example.invalid/show" }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJMessageResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertEqual("Dit komt uit DJConnect Memory.", response!.DjText);
    AssertEqual("https://example.invalid/cover.jpg", response.Images![0].DisplayUrl);
    AssertEqual("djconnect_memory", response.Sources![0].DisplayLabel);
    AssertEqual("metabrainz_metadata", response.Sources![1].DisplayLabel);
    AssertEqual("Concertagenda", response.Links![0].DisplayLabel);
    AssertEqual("https://example.invalid/show", response.Links![0].DisplayUrl);

    var message = new AskDJMessage("links-1", "assistant", "Concerten", null, DateTimeOffset.Now, "assistant", null, null, null, null, response.Sources, Links: response.Links);
    AssertTrue(message.HasSources, "links should render on the same source surface");
    AssertEqual(3, message.DisplaySources.Count);
}

static void AskDJTechnicalTrackAnalysisV2RendersSectionsTimelineAndTips()
{
    const string json = """
    {
      "id": "analysis-1",
      "role": "assistant",
      "text": "Technische analyse staat hieronder.",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "mode": "available",
        "source": "measured",
        "confidence": "high",
        "sections": [
          { "id": "rhythm_bpm", "kind": "metric", "title": "Rhythm / BPM", "value": 128, "source": "measured", "confidence": "high" }
        ],
        "timeline": [
          { "kind": "intro", "label": "Intro", "start": "0:00", "end": "0:16", "source": "measured", "confidence": "high" }
        ],
        "dj_tips": [
          { "kind": "mix", "text": "Mix op de eerste phrase.", "source": "inferred", "confidence": "medium" }
        ],
        "limitations": [
          { "text": "Geen stems beschikbaar.", "source": "local_fallback", "confidence": "low" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.IsTechnicalTrackAnalysis, "technical intent must be detected");
    AssertTrue(message.HasTechnicalAnalysis, "analysis card must be available");
    AssertEqual(1, message.TechnicalAnalysis!.Sections.Count);
    AssertEqual(1, message.TechnicalAnalysis.Timeline.Count);
    AssertEqual(1, message.TechnicalAnalysis.Tips.Count);
    AssertEqual(1, message.TechnicalAnalysis.Limitations.Count);
    AssertTrue(message.TechnicalAnalysis.Sections[0].Meta.Contains("bron: measured", StringComparison.Ordinal), "source must be visible");
    AssertTrue(message.TechnicalAnalysis.Tips[0].Meta.Contains("confidence: medium", StringComparison.Ordinal), "confidence must be visible");
}

static void AskDJTechnicalTrackAnalysisRendersMetaBrainzMetadataContextSeparately()
{
    const string json = """
    {
      "id": "analysis-metabrainz",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "sources": [
        { "source": "metabrainz_metadata" }
      ],
      "analysis": {
        "contract_version": 2,
        "mode": "available",
        "source": "measured",
        "confidence": "high",
        "metadata": {
          "musicbrainz_recording_id": "0f4c2f62-2f7a-4f1d-a91d-01f3d3c00001",
          "match_score": 97,
          "recording_title": "Blue Monday",
          "artist": "New Order",
          "first_release_date": "1983-03-07",
          "release": {
            "title": "Blue Monday",
            "date": "1983-03-07",
            "country": "GB",
            "status": "Official"
          },
          "genres": ["new wave", "synth-pop"],
          "tags": ["dance-rock", "post-punk"],
          "listenbrainz_listen_count": 12345,
          "future_field": "ignored"
        },
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 130, "source": "spotify_audio_features", "confidence": "high" },
          { "id": "metadata_context", "title": "MusicBrainz / ListenBrainz", "summary": "Public metadata matched by recording id.", "source": "metabrainz_metadata", "confidence": "medium" },
          { "id": "buildup", "summary": "Builds gradually toward the break." }
        ],
        "timeline": [
          { "kind": "intro", "label": "Intro", "start": "0:00", "end": "0:16" }
        ],
        "providers": [
          { "provider_id": "metabrainz_metadata", "display_name": "MusicBrainz / ListenBrainz", "status": "used" }
        ],
        "limitations": [
          { "text": "MusicBrainz/ListenBrainz metadata is contextual and is not audio-DSP analysis.", "source": "metabrainz_metadata", "confidence": "medium" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertEqual("0f4c2f62-2f7a-4f1d-a91d-01f3d3c00001", message!.Analysis!.Metadata!.MusicBrainzRecordingId);
    AssertEqual(97, message.Analysis.Metadata.MatchScore);
    AssertEqual("Blue Monday", message.Analysis.Metadata.Release!.Title);
    AssertEqual("metabrainz_metadata", message.Sources![0].DisplayLabel);
    AssertEqual(2, message.TechnicalAnalysis!.Sections.Count);
    AssertTrue(!message.TechnicalAnalysis.Sections.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "metadata context must not render as measured section");
    AssertTrue(message.TechnicalAnalysis.HasContext, "metadata context should render in the context block");
    AssertTrue(message.TechnicalAnalysis.Context.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "context block should label MusicBrainz / ListenBrainz");
    AssertTrue(message.TechnicalAnalysis.Context.Any(row => row.Detail.Contains("12345", StringComparison.Ordinal)), "ListenBrainz listen count should be visible as context");
    AssertEqual(1, message.TechnicalAnalysis.Timeline.Count);
    AssertTrue(!message.TechnicalAnalysis.Timeline.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "metadata context must not create fake timeline labels");
    AssertEqual(1, message.TechnicalAnalysis.Limitations.Count);
    AssertTrue(message.TechnicalAnalysis.Limitations[0].Subtitle.Contains("contextual", StringComparison.OrdinalIgnoreCase), "metadata caveat should stay visible");
}

static void AskDJTechnicalTrackAnalysisWithoutMetadataRemainsCompatible()
{
    const string json = """
    {
      "id": "analysis-no-metadata",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 128 }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.Analysis!.Metadata is null, "missing metadata must remain optional");
    AssertEqual(1, message.TechnicalAnalysis!.Sections.Count);
    AssertEqual(0, message.TechnicalAnalysis.Context.Count);
}

static void AskDJTechnicalTrackAnalysisProvidersRenderAsDiagnostics()
{
    const string json = """
    {
      "id": "analysis-providers",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "mode": "available",
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 128 }
        ],
        "timeline": [
          { "kind": "intro", "label": "Intro", "start": "0:00", "end": "0:16" }
        ],
        "dj_tips": [
          { "kind": "mix", "text": "Mix op de eerste phrase." }
        ],
        "providers": [
          { "provider_id": "spotify_measured", "display_name": "Spotify measured", "status": "used", "requires_config": false },
          { "provider_id": "ha_conversation", "display_name": "HA Conversation", "status": "skipped", "reason": "not requested" },
          { "provider_id": "local_fallback", "display_name": "Local fallback", "status": "used" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertEqual(3, message!.Analysis!.Providers!.Count);
    AssertEqual(1, message.TechnicalAnalysis!.Sections.Count);
    AssertEqual(1, message.TechnicalAnalysis.Timeline.Count);
    AssertEqual(1, message.TechnicalAnalysis.Tips.Count);
    AssertEqual(3, message.TechnicalAnalysis.ProviderDiagnostics.Count);
    AssertEqual("Spotify measured", message.TechnicalAnalysis.ProviderDiagnostics[0].Title);
    AssertEqual("used", message.TechnicalAnalysis.ProviderDiagnostics[0].Subtitle);
    AssertTrue(!message.TechnicalAnalysis.Sections.Any(row => row.Title.Contains("Spotify", StringComparison.OrdinalIgnoreCase)), "providers must not replace normal analysis UI blocks");
}

static void AskDJTechnicalTrackAnalysisMetaBrainzProviderStatusesRemainDiagnostics()
{
    const string usedJson = """
    {
      "id": "analysis-metabrainz-provider-used",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 128 }
        ],
        "providers": [
          { "provider_id": "metabrainz_metadata", "display_name": "MusicBrainz / ListenBrainz", "status": "used" }
        ]
      }
    }
    """;

    const string skippedJson = """
    {
      "id": "analysis-metabrainz-provider-skipped",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 128 }
        ],
        "providers": [
          { "provider_id": "metabrainz_metadata", "display_name": "MusicBrainz / ListenBrainz", "status": "skipped", "reason": "rate_limited" }
        ]
      }
    }
    """;

    const string errorJson = """
    {
      "id": "analysis-metabrainz-provider-error",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 128 }
        ],
        "providers": [
          { "provider_id": "metabrainz_metadata", "display_name": "MusicBrainz / ListenBrainz", "status": "error", "reason": "timeout" }
        ]
      }
    }
    """;

    var used = JsonSerializer.Deserialize<AskDJMessage>(usedJson, JsonOptions());
    var skipped = JsonSerializer.Deserialize<AskDJMessage>(skippedJson, JsonOptions());
    var error = JsonSerializer.Deserialize<AskDJMessage>(errorJson, JsonOptions());

    AssertNotNull(used);
    AssertNotNull(skipped);
    AssertNotNull(error);
    AssertEqual("used", used!.TechnicalAnalysis!.ProviderDiagnostics[0].Subtitle);
    AssertEqual("skipped", skipped!.TechnicalAnalysis!.ProviderDiagnostics[0].Subtitle);
    AssertTrue(skipped.TechnicalAnalysis.ProviderDiagnostics[0].Meta.Contains("rate_limited", StringComparison.Ordinal), "rate limit reason should stay diagnostic");
    AssertEqual("error", error!.TechnicalAnalysis!.ProviderDiagnostics[0].Subtitle);
    AssertEqual(1, error.TechnicalAnalysis.Sections.Count);
    AssertEqual(0, error.TechnicalAnalysis.Context.Count);
}

static void AskDJTechnicalTrackAnalysisWithoutProvidersRemainsCompatible()
{
    const string json = """
    {
      "id": "analysis-no-providers",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_curve", "summary": "Rustige intro, hoge piek na de break." }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.Analysis!.Providers is null, "missing providers must deserialize as optional metadata");
    AssertEqual(1, message.TechnicalAnalysis!.Sections.Count);
    AssertEqual(0, message.TechnicalAnalysis.ProviderDiagnostics.Count);
}

static void AskDJTechnicalTrackAnalysisToleratesUnknownProviders()
{
    const string json = """
    {
      "id": "analysis-unknown-provider",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "rhythm_bpm", "title": "Rhythm / BPM", "value": 126 }
        ],
        "providers": [
          {
            "provider_id": "future_provider",
            "status": "deferred",
            "reason": "Authorization: Bearer secret-provider-token",
            "raw_prompt": "do not render me"
          }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertEqual("future provider", message!.TechnicalAnalysis!.ProviderDiagnostics[0].Title);
    AssertEqual("deferred", message.TechnicalAnalysis.ProviderDiagnostics[0].Subtitle);
    AssertTrue(message.TechnicalAnalysis.ProviderDiagnostics[0].Meta.Contains("reason:", StringComparison.Ordinal), "unknown reasons can remain diagnostic metadata");
    AssertTrue(!message.TechnicalAnalysis.ProviderDiagnostics[0].Meta.Contains("secret-provider-token", StringComparison.Ordinal), "provider diagnostics must redact accidental secrets");
    AssertTrue(!message.TechnicalAnalysis.ProviderDiagnostics[0].Meta.Contains("raw_prompt", StringComparison.OrdinalIgnoreCase), "unknown provider fields must be ignored");
}

static void AskDJTechnicalTrackAnalysisUnavailableRendersSkippedProviderDiagnostics()
{
    const string json = """
    {
      "id": "analysis-unavailable-providers",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "mode": "unavailable",
        "providers": [
          { "provider_id": "spotify_measured", "status": "skipped", "reason": "no current track" },
          { "provider_id": "ha_conversation", "status": "skipped", "requires_config": true },
          { "provider_id": "local_fallback", "status": "skipped" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.TechnicalAnalysis!.IsUnavailable, "unavailable mode should still be visible");
    AssertEqual(0, message.TechnicalAnalysis.Sections.Count);
    AssertEqual(0, message.TechnicalAnalysis.Timeline.Count);
    AssertEqual(0, message.TechnicalAnalysis.Tips.Count);
    AssertEqual(3, message.TechnicalAnalysis.ProviderDiagnostics.Count);
    AssertTrue(message.HasTechnicalAnalysis, "provider diagnostics may keep the diagnostic analysis card visible");
}

static void AskDJTechnicalTrackAnalysisV2WithoutTimelineRendersSections()
{
    const string json = """
    {
      "id": "analysis-2",
      "role": "assistant",
      "action": "track_analysis",
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_curve", "summary": "Rustige intro, hoge piek na de break.", "source": "inferred", "confidence": "low" }
        ],
        "dj_tips": [
          { "kind": "transition", "text": "Houd ruimte voor de break." }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.IsTechnicalTrackAnalysis, "track_analysis action must be detected");
    AssertEqual(1, message.TechnicalAnalysis!.Sections.Count);
    AssertEqual(0, message.TechnicalAnalysis.Timeline.Count);
    AssertEqual("energy curve", message.TechnicalAnalysis.Sections[0].Title);
    AssertTrue(message.TechnicalAnalysis.Sections[0].Meta.Contains("confidence: low", StringComparison.Ordinal), "low confidence must stay visible");
}

static void AskDJTechnicalTrackAnalysisUnavailableRendersLimitations()
{
    const string json = """
    {
      "id": "analysis-unavailable",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "mode": "unavailable",
        "source": "unavailable",
        "confidence": "low",
        "limitations": [
          { "text": "Analyse is uitgeschakeld op de server.", "source": "unavailable", "confidence": "low" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.TechnicalAnalysis!.IsUnavailable, "unavailable mode should render a fallback card");
    AssertEqual(0, message.TechnicalAnalysis.Sections.Count);
    AssertEqual(1, message.TechnicalAnalysis.Limitations.Count);
}

static void AskDJTechnicalTrackAnalysisV1FallbackRendersMeasuredInferredAndLimitations()
{
    const string json = """
    {
      "id": "analysis-v1",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "measured": {
          "bpm": 124,
          "sections": [
            { "label": "Intro", "start": "0:00", "end": "0:20", "source": "measured" }
          ]
        },
        "inferred": {
          "key": "Am",
          "confidence": "low"
        },
        "limitations": [
          { "message": "Structure labels are estimates.", "confidence": "low" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.HasTechnicalAnalysis, "v1 fallback must render");
    AssertTrue(message.TechnicalAnalysis!.Sections.Any(row => row.Title == "Gemeten" && row.Subtitle == "BPM"), "measured bpm must render");
    AssertTrue(message.TechnicalAnalysis.Sections.Any(row => row.Title == "Ingeschat" && row.Subtitle == "Key"), "inferred key must render");
    AssertTrue(message.TechnicalAnalysis.Sections.Any(row => row.Title == "Intro" && row.Subtitle.Contains("0:00", StringComparison.Ordinal)), "measured sections are the only v1 timestamps");
    AssertEqual(1, message.TechnicalAnalysis.Limitations.Count);
}

static void AskDJTechnicalTrackAnalysisRendersUnknownValuesGenerically()
{
    const string json = """
    {
      "id": "analysis-unknown",
      "role": "assistant",
      "intent": { "intent": "technical_track_analysis" },
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "spectral_flux_magic", "kind": "future_kind", "value": "wide", "source": "future_sensor", "confidence": "experimental" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertEqual("spectral flux magic", message!.TechnicalAnalysis!.Sections[0].Title);
    AssertEqual("future kind", message.TechnicalAnalysis.Sections[0].Subtitle);
    AssertTrue(message.TechnicalAnalysis.Sections[0].Meta.Contains("future_sensor", StringComparison.Ordinal), "unknown source must render");
}

static void AskDJTechnicalTrackAnalysisWithoutPlaybackActionsHasNoStaleButtons()
{
    const string json = """
    {
      "id": "analysis-no-actions",
      "role": "assistant",
      "action": "track_analysis",
      "analysis": {
        "contract_version": 2,
        "sections": [
          { "id": "instrumentation", "summary": "Sparse drums and pads." }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(!message!.HasPlaybackActions, "missing playback_actions must not imply buttons from previous bubbles");
    AssertTrue(!message.HasImages, "missing images must not imply artwork from previous bubbles");
}

static void AskDJHistoryDeserializesRevisionsTrimMetadataAndRecentItems()
{
    const string json = """
    {
      "success": true,
      "history_revision": 42,
      "clear_revision": 3,
      "history_limit": 1000,
      "history_trimmed_before": "2026-06-23T12:34:56Z",
      "history_trimmed_count": 7,
      "messages": [
        {
          "id": "assistant-1",
          "role": "assistant",
          "text": "Dit hoorde je net.",
          "message_kind": "assistant",
          "items": [
            {
              "kind": "track",
              "title": "Even Flow",
              "subtitle": "Pearl Jam",
              "played_at_label": "12:34",
              "image_url": "/api/djconnect/image_proxy/token"
            }
          ]
        }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJHistoryResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertTrue(response!.Success, "history response should be successful");
    AssertEqual(42L, response.HistoryRevision);
    AssertEqual(3L, response.ClearRevision);
    AssertEqual(1000, response.HistoryLimit);
    AssertEqual(7, response.HistoryTrimmedCount);
    AssertEqual(1, response.Messages.Count);
    AssertEqual("assistant", response.Messages[0].Role);
    AssertEqual("Even Flow", response.Messages[0].Items![0].Title);
}

static void PlaybackActionDeserializesConfirmationCommand()
{
    const string json = """
    {
      "id": "yes",
      "kind": "confirmation",
      "command": "ask_dj_followup_response",
      "label": "Ja graag",
      "title": "Start ochtendmix"
    }
    """;

    var action = JsonSerializer.Deserialize<PlaybackAction>(json, JsonOptions());

    AssertNotNull(action);
    AssertEqual("confirmation", action!.Kind);
    AssertEqual("ask_dj_followup_response", action.Command);
    AssertEqual("Ja graag", action.Label);
}

static void PlaybackActionDetectsBackendPlayNowRecommendations()
{
    var action = new PlaybackAction("track-1", "track", null, "Play Now", "Play Now", "Zombie", "The Cranberries", "spotify:track:secret", null, null, null, "play_now");

    AssertTrue(action.IsPlayNowRecommendation, "track play_now action must use the backend recommendation command contract");
    AssertTrue(!action.IsConfirmation, "play_now action must not be treated as a follow-up confirmation");
}

static void PlaybackActionDetectsSaveCurrentTrackControl()
{
    const string json = """
    {
      "id": "save-current",
      "kind": "control",
      "command": "save_current_track",
      "button_label": "Zet in favorieten"
    }
    """;

    var action = JsonSerializer.Deserialize<PlaybackAction>(json, JsonOptions());

    AssertNotNull(action);
    AssertTrue(action!.IsSaveCurrentTrackControl, "save_current_track control actions must use the direct command path");
    AssertTrue(!action.IsPlayNowRecommendation, "save_current_track must not be routed as ask_dj_play_recommendation");
    AssertEqual("Zet in favorieten", action.DisplayLabel);
    AssertTrue(!action.HasImage, "control actions must not imply album art");
}

static void PlaybackCommandPayloadStaysGeneric()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildCommandPayload(identity, "volume", new { volume_percent = 42 }, "cmd-1");
    var json = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(json.Contains("\"command\":\"volume\""), "payload must include the generic command");
    AssertTrue(json.Contains("\"client_message_id\":\"cmd-1\""), "payload must include a client message id");
    AssertTrue(json.Contains("\"client_id\":\"djconnect-windows-ABC123DEF456\""), "payload must include the stable client id");
    AssertTrue(json.Contains("\"client_type\":\"windows\""), "payload must include the Windows client type");
    AssertTrue(json.Contains("\"args\":{"), "generic command payloads must preserve args");
    AssertTrue(!json.Contains("spotify_source", StringComparison.OrdinalIgnoreCase), "payload must not include Spotify source overrides");
    AssertTrue(!json.Contains("liked_proxy_playlist_uri", StringComparison.OrdinalIgnoreCase), "payload must not include removed playlist override fields");
    AssertTrue(!json.Contains("refresh_token", StringComparison.OrdinalIgnoreCase), "payload must not include Spotify credentials");
}

static void SaveCurrentTrackCommandPayloadUsesBackendContract()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildCommandPayload(identity, "save_current_track", null, "save-1");
    var json = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(json.Contains("\"command\":\"save_current_track\""), "payload must request save_current_track");
    AssertTrue(json.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\""), "payload must include device_id");
    AssertTrue(json.Contains("\"client_type\":\"windows\""), "payload must include Windows client type");
    AssertTrue(!json.Contains("\"args\""), "save_current_track payload must not require args");
    AssertTrue(!json.Contains("ask_dj_play_recommendation", StringComparison.OrdinalIgnoreCase), "save_current_track must not use recommendation routing");
}

static void DiagnosticRedactionRemovesSecrets()
{
    const string source = """
    Authorization: Bearer abcdefghijklmnopqrstuvwxyz123456
    bearer_token=secret-device-token-value
    pairing_code=123456
    pair_code: 654321
    bootstrap_proof=proof-secret-value
    token: eyJaaaaaaaaaaaaaaaaaaaaaaaa.eyJbbbbbbbbbbbbbbbbbbbb.cccccccccccccc
    push_token=push-secret-value
    cookie=sessionid
    api_key=api-secret-value
    url=http://192.168.1.10:8123/api/djconnect
    """;

    var redacted = DiagnosticRedactor.Redact(source);

    AssertTrue(!redacted.Contains("abcdefghijklmnopqrstuvwxyz123456", StringComparison.Ordinal), "Authorization bearer token must be redacted");
    AssertTrue(!redacted.Contains("secret-device-token-value", StringComparison.Ordinal), "device token must be redacted");
    AssertTrue(!redacted.Contains("123456", StringComparison.Ordinal), "pairing code must be redacted");
    AssertTrue(!redacted.Contains("654321", StringComparison.Ordinal), "pair code must be redacted");
    AssertTrue(!redacted.Contains("proof-secret-value", StringComparison.Ordinal), "bootstrap proof must be redacted");
    AssertTrue(!redacted.Contains("eyJaaaaaaaa", StringComparison.Ordinal), "HA token must be redacted");
    AssertTrue(!redacted.Contains("push-secret-value", StringComparison.Ordinal), "push token must be redacted");
    AssertTrue(!redacted.Contains("sessionid", StringComparison.Ordinal), "cookie value must be redacted");
    AssertTrue(!redacted.Contains("api-secret-value", StringComparison.Ordinal), "API key must be redacted");
    AssertTrue(!redacted.Contains("192.168.1.10", StringComparison.Ordinal), "private URL must be redacted");
    AssertTrue(redacted.Contains("Authorization: <redacted>", StringComparison.Ordinal), "redaction marker should remain useful");
}

static void VersionCompatibilityEnforcesAppProtocolMinor()
{
    var sameMinor = VersionCompatibility.Evaluate("3.2", "3.2.1", null, false, null);
    var olderMinor = VersionCompatibility.Evaluate("3.2", "3.1.12", null, false, null);
    var newerMinor = VersionCompatibility.Evaluate("3.2", null, "3.3", false, null);
    var explicitMismatch = VersionCompatibility.Evaluate("3.2", "3.2.1", null, true, "version_mismatch");
    var devEscapeHatch = VersionCompatibility.Evaluate("3.2", "0.0.0", null, true, "version_mismatch");

    AssertTrue(sameMinor.IsCompatible, "patch versions may differ within the same app protocol minor");
    AssertTrue(!olderMinor.IsCompatible, "older HA minor must require an update");
    AssertTrue(!newerMinor.IsCompatible, "newer HA minor must require an update");
    AssertTrue(!explicitMismatch.IsCompatible, "explicit version_mismatch must require an update");
    AssertTrue(devEscapeHatch.IsCompatible, "0.0.0 is kept as a dev escape hatch");
    AssertEqual("3.2", olderMinor.RequiredMajorMinor);
    AssertEqual("3.1", olderMinor.HomeAssistantMajorMinor);
}

static void QueueNormalizationDeduplicatesAndLimitsItems()
{
    var repeated = new QueueItem("same", null, "Song", null, null, "Artist", null, "Album", 120_000, null, "uri:same", null, null, null, null, null, null, false, false, true, null);
    var items = new List<QueueItem> { repeated, repeated, new(null, null, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, true, null) };
    for (var i = 0; i < 150; i++)
    {
        items.Add(new QueueItem($"id-{i}", null, $"Song {i}", null, null, "Artist", null, null, 100_000, null, $"uri:{i}", null, null, null, null, null, null, false, false, true, null));
    }

    var normalized = QueueItemNormalizer.Normalize(items);

    AssertEqual(100, normalized.Count);
    AssertEqual("same", normalized[0].StableId);
    AssertEqual("01", normalized[0].PositionLabel);
    AssertTrue(normalized.All(item => !string.IsNullOrWhiteSpace(item.DisplayTitleValue)), "queue normalization must skip untitled items");
    AssertEqual(normalized.Count, normalized.Select(item => item.StableId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
}

static void PlaylistNormalizationSupportsAliasesDedupeAndLimits()
{
    var repeated = new PlaylistItem("same", null, null, "Friday", null, null, "Owner mix", null, "Peter", null, null, "https://example.com/a.jpg", null, null, null, "playlist:same", null, null, true, null);
    var items = new List<PlaylistItem> { repeated, repeated, new(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, true, null) };
    for (var i = 0; i < 150; i++)
    {
        items.Add(new PlaylistItem($"id-{i}", null, $"Playlist {i}", null, null, null, null, $"Description {i}", null, null, null, null, null, null, null, $"playlist:{i}", null, null, true, null));
    }

    var normalized = PlaylistItemNormalizer.Normalize(items);

    AssertEqual(100, normalized.Count);
    AssertEqual("same", normalized[0].StableId);
    AssertEqual("Friday", normalized[0].DisplayTitle);
    AssertEqual("Owner mix", normalized[0].DisplaySubtitle);
    AssertTrue(normalized[0].HasArtwork, "playlist artwork aliases should be supported");
    AssertTrue(normalized.All(item => !string.IsNullOrWhiteSpace(item.DisplayTitle)), "playlist normalization must skip untitled items");
    AssertEqual(normalized.Count, normalized.Select(item => item.StableId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
}

static void PairingCodeGeneratorCreatesSixDigits()
{
    var code = PairingCodeGenerator.CreateCode();

    AssertEqual(6, code.Length);
    AssertTrue(code.All(char.IsDigit), "pairing code should contain only digits");
}

static void WelcomeSeenFlagDefaultsToFirstLaunch()
{
    var settings = new AppSettings();

    AssertTrue(!settings.DJConnectWelcomeSeen, "fresh installs must show onboarding before pairing");
    AssertTrue(!settings.HasCompletedOnboarding, "legacy onboarding flag must also default to first launch");
}

static void WhatsNewLastSeenVersionDefaultsEmpty()
{
    var settings = new AppSettings();

    AssertEqual("", settings.LastSeenAppVersion);
}

static void CrashReportFlagsDefaultToCleanFirstLaunch()
{
    var settings = new AppSettings();

    AssertTrue(!settings.HasCleanShutdownState, "fresh installs must not show crash prompt before any session marker exists");
    AssertTrue(settings.CleanShutdown, "fresh installs should default to clean until startup marks the active session dirty");
    AssertTrue(!settings.CrashPromptPending, "fresh installs must not have a pending crash prompt");
}

static void WakewordDefaultsToDisabledAndNotDismissed()
{
    var settings = new AppSettings();

    AssertTrue(!settings.WakewordEnabled, "wakeword must be disabled by default");
    AssertTrue(!settings.WakewordPromptDismissed, "wakeword prompt must not be pre-dismissed");
    AssertEqual("Hey DJ", settings.WakePhrase);
}

static void DemoModeDefaultsToSessionOff()
{
    var settings = new AppSettings();

    AssertTrue(!settings.IsDemoMode, "demo mode must be off by default and treated as session-only");
}

static void MonkeyTestModeIsExplicitAndEnvironmentDriven()
{
    var names = new[]
    {
        "DJCONNECT_DEMO_MONKEY_TEST",
        "DJCONNECT_MONKEY_TEST",
        "DJCONNECT_UI_TEST",
        "MONKEY_TEST",
        "UITEST"
    };

    foreach (var name in names)
    {
        Environment.SetEnvironmentVariable(name, null);
    }

    try
    {
        AssertTrue(!MonkeyTestMode.IsEnabled, "monkey test mode must be opt-in");

        Environment.SetEnvironmentVariable("DJCONNECT_DEMO_MONKEY_TEST", "true");
        AssertTrue(MonkeyTestMode.IsEnabled, "DJCONNECT_DEMO_MONKEY_TEST should enable monkey mode");

        Environment.SetEnvironmentVariable("DJCONNECT_DEMO_MONKEY_TEST", "0");
        AssertTrue(!MonkeyTestMode.IsEnabled, "falsey values must not enable monkey mode");

        Environment.SetEnvironmentVariable("MONKEY_TEST", "yes");
        AssertTrue(MonkeyTestMode.IsEnabled, "legacy monkey env should remain supported");
    }
    finally
    {
        foreach (var name in names)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}

static void DiagnosticLogPreferencesAreBoundedByDefault()
{
    var settings = new AppSettings();

    AssertEqual("info", settings.LogLevel);
    AssertEqual(0, settings.DiagnosticLogLines.Count);
}

static void PermissionExplanationFlagsDefaultToUnseen()
{
    var settings = new AppSettings();

    AssertTrue(!settings.PermissionExplanationMicrophoneSeen, "microphone explanation must not be pre-accepted");
    AssertTrue(!settings.PermissionExplanationNotificationsSeen, "notification explanation must not be pre-accepted");
    AssertTrue(!settings.PermissionExplanationLocalNetworkSeen, "local network explanation must not be pre-accepted");

    settings.PermissionExplanationMicrophoneSeen = true;
    settings.PermissionExplanationNotificationsSeen = true;
    settings.PermissionExplanationLocalNetworkSeen = true;
    var json = JsonSerializer.Serialize(settings, JsonOptions());

    AssertTrue(json.Contains("\"DJConnectPermissionExplanation.microphone.seen\":true"), "microphone explanation must use the requested persisted key");
    AssertTrue(json.Contains("\"DJConnectPermissionExplanation.notifications.seen\":true"), "notification explanation must use the requested persisted key");
    AssertTrue(json.Contains("\"DJConnectPermissionExplanation.localNetwork.seen\":true"), "local network explanation must use the requested persisted key");
}

static void Protocol32UsesLocalOnlyPairingTransport()
{
    var manager = new HomeAssistantTransportManager((url, _) => Task.FromResult(url.Contains("local", StringComparison.OrdinalIgnoreCase)));
    manager.UpdateUrls("http://ha-local:8123", "https://remote.example", true);

    var local = manager.ResolvePairingAsync("http://ha-local:8123", CancellationToken.None).GetAwaiter().GetResult();
    var remoteOnly = manager.ResolvePairingAsync("https://remote.example", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(HomeAssistantConnectionMode.Local, local.Mode);
    AssertEqual("http://ha-local:8123", local.ActiveUrl);
    AssertEqual(HomeAssistantConnectionMode.Offline, remoteOnly.Mode);
    AssertTrue(remoteOnly.ActiveUrl is null, "remote URL must not be used for first pairing");
}

static void Protocol32FallsBackFromLocalToRemoteAfterPairing()
{
    var manager = new HomeAssistantTransportManager((url, _) => Task.FromResult(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));
    manager.UpdateUrls("http://ha-local:8123", "https://remote.example", true);

    var state = manager.ResolveRuntimeAsync(CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(HomeAssistantConnectionMode.Remote, state.Mode);
    AssertEqual("https://remote.example", state.ActiveUrl);
}

static void Protocol32MarksOfflineWhenNoUrlReachable()
{
    var manager = new HomeAssistantTransportManager((_, _) => Task.FromResult(false));
    manager.UpdateUrls("http://ha-local:8123", "https://remote.example", true);

    var state = manager.ResolveRuntimeAsync(CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(HomeAssistantConnectionMode.Offline, state.Mode);
    AssertTrue(state.ActiveUrl is null, "offline transport must not keep an active URL");
}

static void Protocol32ParsesBackendSummary()
{
    const string json = """
    {
      "success": true,
      "ha_local_url": "http://192.168.1.2:8123",
      "ha_remote_url": "https://example.ui.nabu.casa",
      "remote_supported": true,
      "music_backend": "music_assistant",
      "music_backend_name": "Music Assistant",
      "music_backend_available": true,
      "music_backend_revision": 4,
      "music_backend_capabilities": {
        "supports_search": true,
        "supports_queue": true,
        "supports_outputs": true,
        "supports_favorites": false,
        "supports_recently_played": true,
        "supports_top_items": false
      },
      "music_target_player": {
        "id": "media_player.mass_woonkamer",
        "name": "Woonkamer"
      }
    }
    """;

    var response = JsonSerializer.Deserialize<StatusResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertEqual("http://192.168.1.2:8123", response!.HomeAssistantLocalUrl);
    AssertEqual("https://example.ui.nabu.casa", response.HomeAssistantRemoteUrl);
    AssertTrue(response.RemoteSupported == true, "remote support must parse");
    AssertEqual("Music Assistant", response.MusicBackendName);
    AssertEqual(4, response.MusicBackendRevision);
    AssertTrue(response.MusicBackendCapabilities!.CompactSummary.Contains("queue", StringComparison.Ordinal), "capabilities summary should include supported features");
    AssertEqual("Woonkamer", response.MusicTargetPlayer!.Name);
}

static void Protocol32ParsesSafeBackendErrorObject()
{
    const string json = """
    {
      "success": true,
      "music_backend": "music_assistant",
      "music_backend_available": false,
      "music_backend_error": {
        "code": "unsupported_backend_capability",
        "message": "The selected music backend does not provide recent listening history."
      }
    }
    """;

    var response = JsonSerializer.Deserialize<StatusResponse>(json, JsonOptions());
    var summary = new MusicBackendSummary(
        response!.MusicBackend,
        response.MusicBackendName,
        response.MusicBackendAvailable,
        response.MusicBackendRevision,
        response.MusicBackendCapabilities,
        response.MusicTargetPlayer,
        response.MusicBackendError);

    AssertNotNull(response);
    AssertEqual("unsupported_backend_capability", response.MusicBackendError!.Code);
    AssertEqual("The selected music backend does not provide recent listening history.", response.MusicBackendError.Message);
    AssertTrue(summary.IsUnavailable, "safe backend error object should mark backend summary unavailable");
    AssertEqual("The selected music backend does not provide recent listening history.", summary.ErrorText);
}

static void BackendAwareActionsPreserveMusicAssistantValue()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    const string json = """
    {
      "id": "ma-1",
      "kind": "track",
      "action_style": "play_now",
      "value": {
        "item_id": "library://track/123",
        "provider": "music_assistant",
        "media_type": "track",
        "target_player_id": "media_player.mass_woonkamer"
      }
    }
    """;
    var action = JsonSerializer.Deserialize<PlaybackAction>(json, JsonOptions())!;
    var payload = DJConnectApiClient.BuildActionCommandPayload(identity, "ask_dj_play_recommendation", action.Value, "ma-action-1");
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"provider\":\"music_assistant\""), "Music Assistant provider value must be forwarded");
    AssertTrue(serialized.Contains("\"item_id\":\"library://track/123\""), "Music Assistant item id must be forwarded");
    AssertTrue(!serialized.Contains("spotify:track", StringComparison.OrdinalIgnoreCase), "Music Assistant action must not require a Spotify URI");
}

static void BackendAwareActionsCarryBackendRevision()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var action = new PlaybackAction("track-1", "track", null, "Play Now", "Play Now", "Song", "Artist", "spotify:track:abc", null, null, null, "play_now", MusicBackendRevision: 7);
    var payload = DJConnectApiClient.BuildActionCommandPayload(identity, "ask_dj_play_recommendation", action, "rev-action-1");
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"music_backend_revision\":7"), "backend revision must be forwarded when HA includes it");
    AssertTrue(serialized.Contains("\"client_type\":\"windows\""), "action payload must include Windows client type");
}

static void BackendErrorResponsesDeserializeStaleAndUnsupportedContracts()
{
    const string staleJson = """
    {
      "success": false,
      "error": "stale_backend_action",
      "message": "This action was created for a previous music backend. Ask DJ again for a fresh recommendation."
    }
    """;
    const string unsupportedJson = """
    {
      "success": false,
      "error": "unsupported_backend_capability",
      "capability": "supports_recently_played",
      "backend": "music_assistant",
      "message": "The selected music backend does not provide recent listening history."
    }
    """;

    var stale = JsonSerializer.Deserialize<CommandResponse>(staleJson, JsonOptions());
    var unsupported = JsonSerializer.Deserialize<CommandResponse>(unsupportedJson, JsonOptions());

    AssertNotNull(stale);
    AssertTrue(!stale!.Success, "stale backend action response should be unsuccessful");
    AssertEqual("stale_backend_action", stale.Error);
    AssertNotNull(unsupported);
    AssertEqual("unsupported_backend_capability", unsupported!.Error);
    AssertEqual("The selected music backend does not provide recent listening history.", unsupported.Message);
}

static void Protocol32KeepsAppLocalApiAndMdnsInactive()
{
    AssertEqual("3.2", DJConnectContract.ProtocolLine);
    AssertTrue(!DJConnectApiClient.BuildCommandPayload(ClientIdentity.CreateOrLoad("abc123def4567890"), "status").ContainsKey("local_url"), "Windows command payload must not expose a client-hosted local URL");
}

static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertNotNull<T>(T? value)
{
    if (value is null)
    {
        throw new InvalidOperationException("Expected a non-null value.");
    }
}
