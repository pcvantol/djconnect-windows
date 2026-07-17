using System.Text.Json;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Resources;
using DJConnect.Windows.Services;

var tests = new (string Name, Action Run)[]
{
    ("Client identity uses windows contract", ClientIdentityUsesWindowsContract),
    ("Client identity pads short install IDs", ClientIdentityPadsShortInstallIds),
    ("Pairing payload serializes Windows app contract", PairingPayloadSerializesWindowsAppContract),
    ("Pairing client posts only to Home Assistant pair endpoint", PairingClientPostsOnlyToHomeAssistantPairEndpoint),
    ("Pairing deeplink accepts Windows payload", PairingDeepLinkAcceptsWindowsPayload),
    ("Pairing deeplink rejects wrong client type", PairingDeepLinkRejectsWrongClientType),
    ("Pairing deeplink rejects wrong pair path", PairingDeepLinkRejectsWrongPairPath),
    ("Pairing deeplink activation queues payloads", PairingDeepLinkActivationQueuesPayloads),
    ("Windows manifest registers DJConnect protocol", WindowsManifestRegistersDjConnectProtocol),
    ("Pairing errors show localized user guidance", PairingErrorsShowLocalizedUserGuidance),
    ("Authenticated requests include bearer token and device header", AuthenticatedRequestsIncludeBearerTokenAndDeviceHeader),
    ("Status payload serializes app protocol metadata", StatusPayloadSerializesAppProtocolMetadata),
    ("Status response parses Profile metadata", StatusResponseParsesProfileMetadata),
    ("Ask DJ request serializes server-side message contract", AskDJRequestSerializesServerSideContract),
    ("Ask DJ message request includes current locale", AskDJMessageRequestIncludesCurrentLocale),
    ("Ask DJ app client contract omits raw route and legacy aliases", AskDJAppClientContractOmitsRawRouteAndLegacyAliases),
    ("Ask DJ response deserializes exchange messages", AskDJResponseDeserializesExchangeMessages),
    ("Ask DJ response deserializes media sources links and dj text", AskDJResponseDeserializesMediaSourcesLinksAndDjText),
    ("Ask DJ spark follows generated text metadata only", AskDJSparkFollowsGeneratedTextMetadataOnly),
    ("Ask DJ track insight v2 renders sections timeline and tips", AskDJTrackInsightV2RendersSectionsTimelineAndTips),
    ("Ask DJ track insight renders MetaBrainz metadata context separately", AskDJTrackInsightRendersMetaBrainzMetadataContextSeparately),
    ("Ask DJ track insight without metadata remains compatible", AskDJTrackInsightWithoutMetadataRemainsCompatible),
    ("Ask DJ track insight providers render as diagnostics", AskDJTrackInsightProvidersRenderAsDiagnostics),
    ("Ask DJ track insight MetaBrainz provider statuses remain diagnostics", AskDJTrackInsightMetaBrainzProviderStatusesRemainDiagnostics),
    ("Ask DJ track insight without providers remains compatible", AskDJTrackInsightWithoutProvidersRemainsCompatible),
    ("Ask DJ track insight tolerates unknown providers", AskDJTrackInsightToleratesUnknownProviders),
    ("Ask DJ track insight unavailable renders skipped provider diagnostics", AskDJTrackInsightUnavailableRendersSkippedProviderDiagnostics),
    ("Ask DJ track insight v2 without timeline renders sections", AskDJTrackInsightV2WithoutTimelineRendersSections),
    ("Ask DJ track insight unavailable renders limitations", AskDJTrackInsightUnavailableRendersLimitations),
    ("Ask DJ track insight suppresses BPM and key fields", AskDJTrackInsightSuppressesBpmAndKeyFields),
    ("Ask DJ track insight renders unknown values generically", AskDJTrackInsightRendersUnknownValuesGenerically),
    ("Ask DJ track insight without playback actions has no stale buttons", AskDJTrackInsightWithoutPlaybackActionsHasNoStaleButtons),
    ("Ask DJ message presentation supports system audio and confirmations", AskDJMessagePresentationSupportsSystemAudioAndConfirmations),
    ("DJ announcement capabilities without speaker lock speaker outputs", DJAnnouncementCapabilitiesWithoutSpeakerLockSpeakerOutputs),
    ("DJ announcement capabilities with speaker support all outputs", DJAnnouncementCapabilitiesWithSpeakerSupportAllOutputs),
    ("DJ announcement client audio follows delivery and audio URL", DJAnnouncementClientAudioFollowsDeliveryAndAudioUrl),
    ("Ask DJ history deserializes revisions trim metadata and recent items", AskDJHistoryDeserializesRevisionsTrimMetadataAndRecentItems),
    ("Ask DJ clear response flags clear local cache", AskDJClearResponseFlagsClearLocalCache),
    ("Ask DJ history clear HTTP sends identity", AskDJHistoryClearHttpSendsIdentity),
    ("Ask DJ history clear WebSocket sends identity", AskDJHistoryClearWebSocketSendsIdentity),
    ("Ask DJ history export uses HTTP server envelope", AskDJHistoryExportUsesHttpServerEnvelope),
    ("Ask DJ higher clear revision clears before merge", AskDJHigherClearRevisionClearsBeforeMerge),
    ("Ask DJ empty messages after clear do not restore old messages", AskDJEmptyMessagesAfterClearDoNotRestoreOldMessages),
    ("Playback action deserializes confirmation command", PlaybackActionDeserializesConfirmationCommand),
    ("Playback output preserves HA cached Spotify metadata", PlaybackOutputPreservesHaCachedSpotifyMetadata),
    ("Playback output active state ignores cached alone", PlaybackOutputActiveStateIgnoresCachedAlone),
    ("Output picker renders live and cached HA outputs", OutputPickerRendersLiveAndCachedHaOutputs),
    ("Output picker replaces latest HA output list", OutputPickerReplacesLatestHaOutputList),
    ("Ask DJ output action preserves cached output metadata", AskDJOutputActionPreservesCachedOutputMetadata),
    ("Playback action detects backend play now recommendations", PlaybackActionDetectsBackendPlayNowRecommendations),
    ("Playback action detects save current track control", PlaybackActionDetectsSaveCurrentTrackControl),
    ("Playback command payload stays generic", PlaybackCommandPayloadStaysGeneric),
    ("Save current track command payload uses backend contract", SaveCurrentTrackCommandPayloadUsesBackendContract),
    ("Diagnostic redaction removes secrets", DiagnosticRedactionRemovesSecrets),
    ("Version compatibility enforces app protocol minor", VersionCompatibilityEnforcesAppProtocolMinor),
    ("Queue normalization deduplicates and limits items", QueueNormalizationDeduplicatesAndLimitsItems),
    ("Queue command response supports artist metadata and nested shapes", QueueCommandResponseSupportsArtistMetadataAndNestedShapes),
    ("Playlist normalization supports aliases dedupe and limits", PlaylistNormalizationSupportsAliasesDedupeAndLimits),
    ("Pairing code is entered from Home Assistant", PairingCodeIsEnteredFromHomeAssistant),
    ("Pairing UI has outbound-only copy", PairingUiHasOutboundOnlyCopy),
    ("Welcome seen flag defaults to first launch", WelcomeSeenFlagDefaultsToFirstLaunch),
    ("Whats New last seen version defaults empty", WhatsNewLastSeenVersionDefaultsEmpty),
    ("Crash report flags default to clean first launch", CrashReportFlagsDefaultToCleanFirstLaunch),
    ("Wakeword defaults to disabled and not dismissed", WakewordDefaultsToDisabledAndNotDismissed),
    ("Demo mode defaults to session off", DemoModeDefaultsToSessionOff),
    ("Monkey test mode is explicit and environment driven", MonkeyTestModeIsExplicitAndEnvironmentDriven),
    ("Diagnostic log preferences are bounded by default", DiagnosticLogPreferencesAreBoundedByDefault),
    ("Permission explanation flags default to unseen", PermissionExplanationFlagsDefaultToUnseen),
    ("Protocol 3.2 uses local-only pairing transport", Protocol32UsesLocalOnlyPairingTransport),
    ("Protocol 3.2 requires no local device callback path", Protocol32RequiresNoLocalDeviceCallbackPath),
    ("Protocol 3.2 falls back from local to remote after pairing", Protocol32FallsBackFromLocalToRemoteAfterPairing),
    ("Pairing response persists remote URL and API capabilities", PairingResponsePersistsRemoteUrlAndApiCapabilities),
    ("Stale pairing errors trigger local cleanup policy", StalePairingErrorsTriggerLocalCleanupPolicy),
    ("Protocol 3.2 marks offline when no HA URL is reachable", Protocol32MarksOfflineWhenNoUrlReachable),
    ("Protocol 3.2 parses backend summary", Protocol32ParsesBackendSummary),
    ("Protocol 3.2 parses safe backend error object", Protocol32ParsesSafeBackendErrorObject),
    ("DJConnect Home Assistant HTTP routes use canonical v1 prefix", DJConnectHomeAssistantHttpRoutesUseCanonicalV1Prefix),
    ("Release workflow pushes notes from current HEAD", ReleaseWorkflowPushesNotesFromCurrentHead),
    ("Release workflow extracts only current changelog section", ReleaseWorkflowExtractsOnlyCurrentChangelogSection),
    ("Windows interactive GUI smoke relay is isolated from the service runner", WindowsInteractiveGuiSmokeRelayIsIsolated),
    ("Release context docs mention current app version", ReleaseContextDocsMentionCurrentAppVersion),
    ("Backend-aware actions preserve Music Assistant value", BackendAwareActionsPreserveMusicAssistantValue),
    ("Backend-aware actions carry backend revision", BackendAwareActionsCarryBackendRevision),
    ("Command payload includes current locale and preserves protocol values", CommandPayloadIncludesCurrentLocaleAndPreservesProtocolValues),
    ("Command payload carries DJ announcement output only", CommandPayloadCarriesDjAnnouncementOutputOnly),
    ("Raw voice upload includes language headers", RawVoiceUploadIncludesLanguageHeaders),
    ("Transport options require local HA websocket auth opt-in", TransportOptionsRequireLocalHaWebSocketAuthOptIn),
    ("Fast path diagnostics formatter renders safe summary", FastPathDiagnosticsFormatterRendersSafeSummary),
    ("WebSocket fast path stays disabled without HA auth token", WebSocketFastPathStaysDisabledWithoutHaAuthToken),
    ("WebSocket fast path detects capabilities", WebSocketFastPathDetectsCapabilities),
    ("WebSocket command success skips HTTP", WebSocketCommandSuccessSkipsHttp),
    ("WebSocket command payload includes current locale", WebSocketCommandPayloadIncludesCurrentLocale),
    ("WebSocket missing capability falls back to HTTP", WebSocketMissingCapabilityFallsBackToHttp),
    ("WebSocket Ask DJ message success uses revisions", WebSocketAskDJMessageSuccessUsesRevisions),
    ("WebSocket Ask DJ payload includes current locale", WebSocketAskDJPayloadIncludesCurrentLocale),
    ("WebSocket Track Insight success renders Music DNA", WebSocketTrackInsightSuccessRendersMusicDna),
    ("Track Insight payload includes identity and title artist", TrackInsightPayloadIncludesIdentityAndTitleArtist),
    ("Track Insight payload includes mood and Music DNA key", TrackInsightPayloadIncludesMoodAndMusicDnaKey),
    ("Profile-aware requests generate canonical Windows context", ProfileAwareRequestsGenerateCanonicalWindowsContext),
    ("Profile response metadata decodes canonical envelope", ProfileResponseMetadataDecodesCanonicalEnvelope),
    ("Profile platform errors localize as repair states", ProfilePlatformErrorsLocalizeAsRepairStates),
    ("Music DNA profile decodes mood and energy backend shapes", MusicDnaProfileDecodesMoodAndEnergyBackendShapes),
    ("Music DNA dashboard hides disabled and empty blocks", MusicDnaDashboardHidesDisabledAndEmptyBlocks),
    ("Music DNA dashboard renders optional eligible blocks", MusicDnaDashboardRendersOptionalEligibleBlocks),
    ("Music DNA settings and clear payloads include identity context", MusicDnaSettingsAndClearPayloadsIncludeIdentityContext),
    ("WebSocket Music DNA profile success skips HTTP", WebSocketMusicDnaProfileSuccessSkipsHttp),
    ("WebSocket Music DNA unsupported falls back to HTTP", WebSocketMusicDnaUnsupportedFallsBackToHttp),
    ("Discover nav appears in Apple order", DiscoverNavAppearsInAppleOrder),
    ("Discover consent UI is wired to Music DNA commands", DiscoverConsentUiIsWiredToMusicDnaCommands),
    ("Music Discovery feed renders supported items and reasons", MusicDiscoveryFeedRendersSupportedItemsAndReasons),
    ("Music Discovery disabled response does not render feed", MusicDiscoveryDisabledResponseDoesNotRenderFeed),
    ("Music Discovery feed request carries identity query", MusicDiscoveryFeedRequestCarriesIdentityQuery),
    ("WebSocket Music Discovery feed success skips HTTP", WebSocketMusicDiscoveryFeedSuccessSkipsHttp),
    ("Music Discovery refresh uses endpoint and websocket", MusicDiscoveryRefreshUsesEndpointAndWebSocket),
    ("Music Discovery play sends source and identity", MusicDiscoveryPlaySendsSourceAndIdentity),
    ("WebSocket timeout falls back to HTTP exactly once", WebSocketTimeoutFallsBackToHttpExactlyOnce),
    ("WebSocket auth error falls back to HTTP", WebSocketAuthErrorFallsBackToHttp),
    ("Remote connection stays HTTP", RemoteConnectionStaysHttp),
    ("WebSocket payload includes identity and token without diagnostic leaks", WebSocketPayloadIncludesIdentityAndTokenWithoutDiagnosticLeaks),
    ("Backend error responses deserialize stale and unsupported contracts", BackendErrorResponsesDeserializeStaleAndUnsupportedContracts),
    ("Localization supports required locales", LocalizationSupportsRequiredLocales),
    ("Settings localization avoids diagnostic jargon", SettingsLocalizationAvoidsDiagnosticJargon),
    ("API error localizer maps user-facing guidance", ApiErrorLocalizerMapsUserFacingGuidance),
    ("API error localization preserves protocol values", ApiErrorLocalizationPreservesProtocolValues),
    ("Protocol 3.2 advertises no client callback endpoint", Protocol32AdvertisesNoClientCallbackEndpoint),
    ("Windows client code avoids removed DJConnect HA playback entities", WindowsClientCodeAvoidsRemovedDjconnectHaPlaybackEntities)
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

static void PairingPayloadSerializesWindowsAppContract()
{
    var payload = new PairingPayload(
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "123456",
        DJConnectContract.AppVersion,
        Platform: "windows",
        Locale: "nl",
        Language: "nl",
        Build: DJConnectContract.AppVersion);

    using var document = JsonSerializer.SerializeToDocument(payload);
    var root = document.RootElement;

    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("device_id").GetString());
    AssertEqual("Studio PC", root.GetProperty("device_name").GetString());
    AssertEqual("windows", root.GetProperty("client_type").GetString());
    AssertEqual("windows", root.GetProperty("platform").GetString());
    AssertEqual("123456", root.GetProperty("pair_code").GetString());
    AssertEqual("123456", root.GetProperty("pairing_token").GetString());
    AssertEqual("123456", root.GetProperty("pairing_code").GetString());
    AssertEqual("nl", root.GetProperty("locale").GetString());
    AssertEqual("nl", root.GetProperty("language").GetString());
    AssertEqual(DJConnectContract.AppVersion, root.GetProperty("app_version").GetString());
}

static void PairingClientPostsOnlyToHomeAssistantPairEndpoint()
{
    var http = new FakeHttpHandler("""
    {
      "success": true,
      "client_type": "windows",
      "device_token": "paired-device-token",
      "ha_local_url": "http://ha-local:8123"
    }
    """);
    var client = new DJConnectApiClient(new HttpClient(http));
    client.Configure("http://ha-local:8123", "old-token-that-must-not-be-sent");

    var response = client.PairAsync(new PairingPayload(
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "123456",
        DJConnectContract.AppVersion), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "pairing response should deserialize");
    AssertEqual("api/djconnect/v1/pair", http.LastPath.TrimStart('/'));
    AssertEqual("POST", http.LastMethod);
    AssertEqual("application/json", http.LastContentType);
    AssertEqual("windows", http.LastClientTypeHeader);
    AssertTrue(!http.RequestPaths.Any(path => path.Contains("/api/device/", StringComparison.OrdinalIgnoreCase)), "Windows pairing must not require a local /api/device callback");
    AssertTrue(string.IsNullOrWhiteSpace(http.LastAuthorization), "pairing request must not use a bearer token");
    AssertTrue(string.IsNullOrWhiteSpace(http.LastDeviceIdHeader), "pairing request must not use authenticated device header before token issue");
    AssertTrue(http.LastBody.Contains("\"pair_code\":\"123456\""), "pairing body must send the HA pair code");
    AssertTrue(http.LastBody.Contains("\"pairing_token\":\"123456\""), "pairing body must send compatibility pairing_token");
    AssertTrue(http.LastBody.Contains("\"pairing_code\":\"123456\""), "pairing body must send compatibility pairing_code");
}

static void PairingDeepLinkAcceptsWindowsPayload()
{
    const string payload = "djconnect://pair?ha_url=http%3A%2F%2Fhomeassistant.local%3A8123&pair_code=123456&client_type=windows&pair_path=%2Fapi%2Fdjconnect%2Fv1%2Fpair";

    var accepted = PairingDeepLinkPayload.TryParse(payload, out var result, out var reason);

    AssertTrue(accepted, $"payload should be accepted, got {reason}");
    AssertEqual("http://homeassistant.local:8123", result.HomeAssistantUrl);
    AssertEqual("123456", result.PairCode);
    AssertEqual("windows", result.ClientType);
    AssertEqual("/api/djconnect/v1/pair", result.PairPath);
}

static void PairingDeepLinkRejectsWrongClientType()
{
    const string payload = """{"ha_url":"http://homeassistant.local:8123","pair_code":"123456","client_type":"ios","pair_path":"/api/djconnect/v1/pair"}""";

    var accepted = PairingDeepLinkPayload.TryParse(payload, out _, out var reason);

    AssertTrue(!accepted, "wrong client_type must be rejected");
    AssertEqual("client_type", reason);
}

static void PairingDeepLinkRejectsWrongPairPath()
{
    const string payload = """{"ha_url":"http://homeassistant.local:8123","pair_code":"123456","client_type":"windows","pair_path":"/api/device/pair"}""";

    var accepted = PairingDeepLinkPayload.TryParse(payload, out _, out var reason);

    AssertTrue(!accepted, "wrong pair_path must be rejected");
    AssertEqual("pair_path", reason);
}

static void PairingDeepLinkActivationQueuesPayloads()
{
    while (PairingDeepLinkActivation.TryDequeue(out _))
    {
    }

    var eventCount = 0;
    void OnQueued(object? sender, EventArgs args) => eventCount++;

    PairingDeepLinkActivation.PayloadQueued += OnQueued;
    try
    {
        PairingDeepLinkActivation.Queue("   ");
        PairingDeepLinkActivation.Queue("djconnect://pair?ha_url=http%3A%2F%2Fhomeassistant.local%3A8123&pair_code=123456&client_type=windows&pair_path=%2Fapi%2Fdjconnect%2Fv1%2Fpair");

        AssertEqual(1, eventCount);
        AssertTrue(PairingDeepLinkActivation.TryDequeue(out var payload), "queued deeplink should be available to MainPage after activation");
        AssertTrue(payload.StartsWith("djconnect://pair?", StringComparison.Ordinal), "queued payload must preserve original URI");
        AssertTrue(!PairingDeepLinkActivation.TryDequeue(out _), "queue should be empty after consuming the payload");
    }
    finally
    {
        PairingDeepLinkActivation.PayloadQueued -= OnQueued;
    }
}

static void WindowsManifestRegistersDjConnectProtocol()
{
    var manifest = File.ReadAllText(Path.Combine(ProjectRoot(), "src", "DJConnect.Windows", "Platforms", "Windows", "Package.appxmanifest"));

    AssertTrue(manifest.Contains("Category=\"windows.protocol\"", StringComparison.Ordinal), "Windows manifest must register protocol activation");
    AssertTrue(manifest.Contains("<uap:Protocol Name=\"djconnect\">", StringComparison.Ordinal), "Windows manifest must register djconnect:// scheme");
}

static void PairingErrorsShowLocalizedUserGuidance()
{
    AppStrings.UseLanguage("nl");

    AssertEqual("Koppelcode klopt niet. Controleer de code in Home Assistant.", ApiErrorLocalizer.Pairing("invalid_pair_code"));
    AssertEqual("Koppelcode klopt niet. Controleer de code in Home Assistant.", ApiErrorLocalizer.Pairing("not_configured"));
    AssertEqual("Het gekozen app-type in Home Assistant klopt niet met deze app. Kies in Home Assistant de DJConnect Windows setup-flow en probeer opnieuw.", ApiErrorLocalizer.Pairing("client_type_mismatch"));
    AssertEqual("Verkeerd app-type gekozen in Home Assistant. Kies de DJConnect Windows setup-flow en gebruik de nieuwe koppelcode.", ApiErrorLocalizer.Pairing("invalid_client_type"));
    AssertEqual("Home Assistant reageerde niet op tijd. Controleer of dit apparaat op hetzelfde netwerk zit en probeer opnieuw.", ApiErrorLocalizer.Pairing(new TimeoutException()));
}

static void AuthenticatedRequestsIncludeBearerTokenAndDeviceHeader()
{
    var http = new FakeHttpHandler("""{"success":true}""");
    var client = new DJConnectApiClient(new HttpClient(http));
    var identity = TestIdentity();
    client.Configure(new DJConnectClientConfiguration(
        "http://ha-local:8123",
        "paired-device-token",
        false,
        null,
        identity.DeviceId));

    var response = client.GetStatusAsync(identity, CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "status response should deserialize");
    AssertEqual("Bearer paired-device-token", http.LastAuthorization);
    AssertEqual(identity.DeviceId, http.LastDeviceIdHeader);
    AssertEqual("api/djconnect/v1/status", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\""), "status body must include device identity");
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "status body must include Windows client type");
}

static void StatusPayloadSerializesAppProtocolMetadata()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildStatusPayload(identity);
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"client_type\":\"windows\""), "status must include Windows client type");
    AssertTrue(serialized.Contains("\"firmware\":\"windows-app\""), "status must identify the app surface as firmware metadata for HA compatibility");
    AssertTrue(serialized.Contains($"\"app_version\":\"{DJConnectContract.AppVersion}\""), "status must include app version");
    AssertTrue(serialized.Contains("\"protocol_version\":\"3.2\""), "status must include protocol line");
}

static void StatusResponseParsesProfileMetadata()
{
    const string json = """
    {
      "success": true,
      "profile_id": "profile-peter",
      "music_dna_key": "profile:profile-peter",
      "resolved_profile": {
        "id": "profile-peter",
        "name": "Peter",
        "type": "personal",
        "privacy_mode": "normal"
      },
      "resolution": {
        "source": "device_mapping",
        "fallback_used": false
      },
      "profile_privacy_mode": "normal",
      "profile_privacy": {
        "mode": "normal",
        "private_session": false
      }
    }
    """;

    var response = JsonSerializer.Deserialize<StatusResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertEqual("profile-peter", response!.ProfileId);
    AssertEqual("profile:profile-peter", response.MusicDnaKey);
    AssertEqual("Peter", response.ResolvedProfile!.Name);
    AssertEqual("device_mapping", response.Resolution!.Source);
    AssertEqual("normal", response.ProfilePrivacyMode);
    AssertTrue(response.ProfilePrivacy!.PrivateSession == false, "privacy object should preserve private session flag");
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
    AssertTrue(!root.TryGetProperty("message", out _), "Ask DJ app-client request must use text, not the removed raw/developer message alias");
    AssertEqual("auto", root.GetProperty("audio_response").GetString());
    AssertEqual(72, root.GetProperty("mood").GetInt32());
    AssertEqual(DJConnectContract.AppVersion, root.GetProperty("app_version").GetString());
    AssertEqual("3.2", root.GetProperty("protocol_version").GetString());
}

static void AskDJAppClientContractOmitsRawRouteAndLegacyAliases()
{
    var root = ProjectRoot();
    var scanRoots = new[]
    {
        Path.Combine(root, "src"),
        Path.Combine(root, "tests"),
        Path.Combine(root, "docs"),
        Path.Combine(root, "README.md"),
        Path.Combine(root, "CHANGELOG.md")
    };
    var offenders = scanRoots
        .SelectMany(RouteScanFiles)
        .SelectMany(file => File.ReadLines(file)
            .Select((line, index) => (File: file, LineNumber: index + 1, Line: line))
            .Where(row => ContainsRawAskDJRoute(row.Line)
                || ContainsExactProtocolValue(row.Line, "djconnect." + "ask_dj")
                || row.Line.Contains("spotify_" + "search_query", StringComparison.Ordinal)
                || row.Line.Contains("last_" + "spotify_search", StringComparison.Ordinal)))
        .Select(row => $"{Path.GetRelativePath(root, row.File)}:{row.LineNumber}: {row.Line.Trim()}")
        .ToList();

    AssertTrue(offenders.Count == 0, "removed Ask DJ raw route/service or legacy Spotify search aliases remain:\n" + string.Join("\n", offenders));
}

static void AskDJMessageRequestIncludesCurrentLocale()
{
    var http = new FakeHttpHandler("""{"success":true,"message":"ok"}""");
    var client = new DJConnectApiClient(new HttpClient(http));
    client.Configure("http://homeassistant.local:8123", "paired-device-token");

    var request = TestAskRequest("speel iets rustigs") with
    {
        Language = AppStrings.NormalizeApiLocale("nl"),
        Locale = AppStrings.NormalizeApiLocale("nl")
    };
    var response = client.SendAskDJMessageAsync(request, CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "Ask DJ response should deserialize");
    AssertEqual("api/djconnect/v1/ask_dj/message", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"language\":\"nl-NL\""), "Ask DJ message body must include BCP-47 language");
    AssertTrue(http.LastBody.Contains("\"locale\":\"nl-NL\""), "Ask DJ message body must include BCP-47 locale");
    AssertTrue(http.LastBody.Contains("\"mood\":72"), "Ask DJ message body must include current mood");
    AssertEqual("nl-NL", http.LastAcceptLanguageHeader);
    AssertEqual("nl-NL", http.LastLanguageHeader);
    AssertEqual("nl-NL", http.LastLocaleHeader);
    AssertEqual("72", http.LastMoodHeader);
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "Ask DJ message body must preserve Windows client type");
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

static void PlaybackOutputPreservesHaCachedSpotifyMetadata()
{
    const string json = """
    {
      "success": true,
      "available_outputs": [
        {
          "id": "spotify-speaker-1",
          "name": "Kitchen Speaker",
          "type": "speaker",
          "cached": true,
          "provider": "spotify",
          "source": "spotify",
          "supports_volume": true,
          "volume_percent": 37,
          "first_seen_at": "2026-06-01T10:00:00Z",
          "last_seen_at": "2026-07-09T20:30:00Z"
        }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<StatusResponse>(json, JsonOptions());
    var output = response!.ResolvedOutputs()!.Single();

    AssertEqual("spotify-speaker-1", output.Id);
    AssertEqual("Kitchen Speaker", output.Name);
    AssertEqual("speaker", output.Type);
    AssertTrue(output.Cached == true, "cached flag must be preserved");
    AssertEqual("spotify", output.Provider);
    AssertEqual("spotify", output.Source);
    AssertTrue(output.SupportsVolume == true, "supports_volume must be preserved");
    AssertEqual(37, output.VolumePercent);
    AssertEqual(DateTimeOffset.Parse("2026-06-01T10:00:00Z"), output.FirstSeenAt);
    AssertEqual(DateTimeOffset.Parse("2026-07-09T20:30:00Z"), output.LastSeenAt);
    AssertTrue(output.IsCachedSpotify, "Spotify cached target should be detectable for friendly failure copy");
}

static void PlaybackOutputActiveStateIgnoresCachedAlone()
{
    var cached = new PlaybackOutput("cached", "Cached Speaker", null, null, Cached: true, Provider: "spotify");
    var active = new PlaybackOutput("active", "Active Speaker", null, null, Active: true, Cached: true, Provider: "spotify");
    var isActive = new PlaybackOutput("is-active", "Is Active Speaker", null, true, Cached: true, Provider: "spotify");

    AssertTrue(!cached.IsCurrent, "cached:true alone must not mark an output active/current");
    AssertTrue(active.IsCurrent, "active:true should mark the output current");
    AssertTrue(isActive.IsCurrent, "is_active:true should mark the output current");
}

static void OutputPickerRendersLiveAndCachedHaOutputs()
{
    AppStrings.UseLanguage("en");
    var outputDevices = new List<PlaybackOutput>();
    ReplaceOutputList(outputDevices,
    [
        new PlaybackOutput("live", "Living Room", null, true, Provider: "spotify"),
        new PlaybackOutput("cached", "Kitchen", null, null, Cached: true, Provider: "spotify", LastSeenAt: DateTimeOffset.Parse("2026-07-01T10:00:00Z"))
    ]);

    var selectedOutput = outputDevices.FirstOrDefault(output => output.IsCurrent);

    AssertEqual(2, outputDevices.Count);
    AssertEqual("Living Room", outputDevices[0].DisplayNameWithStatus);
    AssertEqual("Kitchen · Recently seen", outputDevices[1].DisplayNameWithStatus);
    AssertEqual("cached", outputDevices[1].CommandValue);
    AssertTrue(outputDevices[1].IsCachedSpotify, "cached output remains selectable through its HA id");
    AssertEqual("Living Room", selectedOutput!.DisplayName);
}

static void OutputPickerReplacesLatestHaOutputList()
{
    var outputDevices = new List<PlaybackOutput>();
    ReplaceOutputList(outputDevices,
    [
        new PlaybackOutput("live", "Living Room", null, true),
        new PlaybackOutput("cached", "Kitchen", null, null, Cached: true, Provider: "spotify")
    ]);
    ReplaceOutputList(outputDevices,
    [
        new PlaybackOutput("live", "Living Room", null, true)
    ]);

    AssertEqual(1, outputDevices.Count);
    AssertTrue(!outputDevices.Any(output => output.Id == "cached"), "outputs missing from the latest HA response must not be resurrected locally");
}

static void AskDJOutputActionPreservesCachedOutputMetadata()
{
    const string json = """
    {
      "id": "assistant-output-action",
      "role": "assistant",
      "text": "Use Kitchen?",
      "playback_actions": [
        {
          "id": "cached-output-action",
          "kind": "output",
          "command": "set_output",
          "label": "Kitchen",
          "cached": true,
          "provider": "spotify",
          "source": "spotify",
          "first_seen_at": "2026-06-01T10:00:00Z",
          "last_seen_at": "2026-07-09T20:30:00Z",
          "output": {
            "id": "spotify-kitchen",
            "name": "Kitchen",
            "cached": true,
            "provider": "spotify",
            "source": "spotify",
            "last_seen_at": "2026-07-09T20:30:00Z"
          }
        }
      ]
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());
    var action = message!.PlaybackActions!.Single();

    AssertEqual("output", action.Kind);
    AssertTrue(action.Cached == true, "Ask DJ output action cached flag must be preserved");
    AssertEqual("spotify", action.Provider);
    AssertEqual("spotify", action.Source);
    AssertEqual(DateTimeOffset.Parse("2026-06-01T10:00:00Z"), action.FirstSeenAt);
    AssertEqual(DateTimeOffset.Parse("2026-07-09T20:30:00Z"), action.LastSeenAt);
    AssertNotNull(action.Output);
    AssertTrue(action.Output!.IsCachedSpotify, "nested output metadata must survive decoding");
    AssertEqual("spotify-kitchen", action.Output.CommandValue);
}

static void DJAnnouncementCapabilitiesWithoutSpeakerLockSpeakerOutputs()
{
    const string json = """
    {
      "speaker_configured": false,
      "supported_outputs": ["client_device", "text_only"]
    }
    """;

    var capabilities = JsonSerializer.Deserialize<DJAnnouncementCapabilities>(json, JsonOptions());

    AssertNotNull(capabilities);
    AssertTrue(capabilities!.Supports(DJAnnouncementOutput.ClientDevice), "client_device must be available without HA speaker");
    AssertTrue(capabilities.Supports(DJAnnouncementOutput.TextOnly), "text_only must be available without HA speaker");
    AssertTrue(!capabilities.Supports(DJAnnouncementOutput.Both), "both must be locked without HA speaker");
    AssertTrue(!capabilities.Supports(DJAnnouncementOutput.HaSpeaker), "ha_speaker must be locked without HA speaker");
}

static void DJAnnouncementCapabilitiesWithSpeakerSupportAllOutputs()
{
    const string json = """
    {
      "speaker_configured": true,
      "speaker_entity_id": "media_player.voice_preview",
      "speaker_name": "Voice Preview",
      "default_output": "both",
      "supported_outputs": ["client_device", "both", "ha_speaker", "text_only"]
    }
    """;

    var capabilities = JsonSerializer.Deserialize<DJAnnouncementCapabilities>(json, JsonOptions());

    AssertNotNull(capabilities);
    AssertTrue(capabilities!.HasSpeaker, "speaker config must parse");
    AssertEqual("Voice Preview", capabilities.SpeakerDisplayName);
    AssertEqual(DJAnnouncementOutput.Both, capabilities.EffectiveDefaultOutput());
    AssertTrue(capabilities.Supports(DJAnnouncementOutput.ClientDevice), "client_device must be available with HA speaker");
    AssertTrue(capabilities.Supports(DJAnnouncementOutput.Both), "both must be available with HA speaker");
    AssertTrue(capabilities.Supports(DJAnnouncementOutput.HaSpeaker), "ha_speaker must be available with HA speaker");
    AssertTrue(capabilities.Supports(DJAnnouncementOutput.TextOnly), "text_only must be available with HA speaker");
}

static void DJAnnouncementClientAudioFollowsDeliveryAndAudioUrl()
{
    var both = new AskDJMessage("both", "assistant", "Audio", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, "legacy.mp3", new DJAnnouncement(null, DJAnnouncementOutput.Both, null, "both.mp3", "mp3", null, null));
    var haSpeaker = both with { Id = "ha", Announcement = new DJAnnouncement(null, DJAnnouncementOutput.HaSpeaker, null, "ha.mp3", "mp3", null, null) };
    var textOnly = both with { Id = "text", Announcement = new DJAnnouncement(null, DJAnnouncementOutput.TextOnly, null, "text.mp3", "mp3", null, null) };
    var missingAnnouncementAudio = both with { Id = "missing", Announcement = new DJAnnouncement(null, DJAnnouncementOutput.Both, null, null, "mp3", null, null) };

    AssertTrue(both.HasAudio, "both + audio_url must allow local replay/autoplay after response fetch");
    AssertEqual("both.mp3", both.ClientAudioUrl);
    AssertTrue(!haSpeaker.HasAudio, "ha_speaker must never play local client audio");
    AssertTrue(!textOnly.HasAudio, "text_only must never play local client audio");
    AssertTrue(!missingAnnouncementAudio.HasAudio, "announcement without audio_url must be text-only for client audio");
}

static void AskDJResponseDeserializesMediaSourcesLinksAndDjText()
{
    const string json = """
    {
      "success": true,
      "dj_text": "Dit komt uit Music DNA.",
      "images": [
        { "image_url": "https://example.invalid/cover.jpg", "title": "Cover" }
      ],
      "sources": [
        { "id": "djconnect_music_dna", "label": "djconnect_music_dna" },
        { "source": "metabrainz_metadata" }
      ],
      "links": [
        { "source": "bandsintown", "label": "Concertagenda", "url": "https://example.invalid/show" }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJMessageResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertEqual("Dit komt uit Music DNA.", response!.DjText);
    AssertEqual("https://example.invalid/cover.jpg", response.Images![0].DisplayUrl);
    AssertEqual("djconnect_music_dna", response.Sources![0].DisplayLabel);
    AssertEqual("metabrainz_metadata", response.Sources![1].DisplayLabel);
    AssertEqual("Concertagenda", response.Links![0].DisplayLabel);
    AssertEqual("https://example.invalid/show", response.Links![0].DisplayUrl);

    var message = new AskDJMessage("links-1", "assistant", "Concerten", null, DateTimeOffset.Now, "assistant", null, null, null, null, response.Sources, Links: response.Links);
    AssertTrue(message.HasSources, "links should render on the same source surface");
    AssertEqual(3, message.DisplaySources.Count);
}

static void AskDJSparkFollowsGeneratedTextMetadataOnly()
{
    const string json = """
    {
      "success": true,
      "assistant_message": {
        "id": "assistant-generated",
        "role": "assistant",
        "text": "Dit is echt gegenereerde tekst.",
        "is_generated_text": true
      }
    }
    """;
    var response = JsonSerializer.Deserialize<AskDJMessageResponse>(json, JsonOptions());
    var generated = response!.AssistantMessage!;
    var fallback = new AskDJMessage("fallback", "assistant", "Play Now fallbacktekst", null, DateTimeOffset.Now, "assistant", null, null, null, null, null);
    var system = new AskDJMessage("system", "system", "History trimmed", null, DateTimeOffset.Now, "system", null, null, null, null, null)
    {
        IsGeneratedText = true
    };

    AssertTrue(generated.ShowGeneratedTextSpark, "assistant_message.is_generated_text=true must render the spark");
    AssertEqual("✦ ", generated.GeneratedTextSpark);
    AssertTrue(!fallback.ShowGeneratedTextSpark, "missing metadata must not render the spark");
    AssertTrue(!system.ShowGeneratedTextSpark, "system messages must not render the spark even if metadata is malformed");
    AssertEqual("#1B3556", (new AskDJMessage("chill", "assistant", "backend text", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, Mood: 12)).BubbleBackground);
    AssertEqual("#24345F", (new AskDJMessage("groove", "assistant", "backend text", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, Mood: 42)).BubbleBackground);
    AssertEqual("#442A76", (new AskDJMessage("energy", "assistant", "backend text", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, Mood: 72)).BubbleBackground);
    AssertEqual("#5A244E", (new AskDJMessage("party", "assistant", "backend text", null, DateTimeOffset.Now, "assistant", null, null, null, null, null, Mood: 92)).BubbleBackground);
    AssertEqual("#5539D7", (new AskDJMessage("user", "user", "hello", null, DateTimeOffset.Now, "user", null, null, null, null, null)).BubbleBackground);
}

static void AskDJTrackInsightV2RendersSectionsTimelineAndTips()
{
    const string json = """
    {
      "id": "analysis-1",
      "role": "assistant",
      "text": "Track Insight staat hieronder.",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "mode": "available",
        "source": "measured",
        "confidence": "high",
        "track": { "title": "Blue Monday", "artist": "New Order", "album": "Blue Monday" },
        "analysis": {
          "sections": [
            { "id": "energy_profile", "kind": "metric", "title": "Energy", "value": 128, "source": "measured", "confidence": "high" }
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
        },
        "music_dna": {
          "match_percent": 86,
          "why_it_fits": "This expands your Music DNA."
        }
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.IsTrackInsight, "track insight intent must be detected");
    AssertTrue(message.HasTrackInsight, "track insight card must be available");
    AssertEqual(2, message.TrackInsight!.Sections.Count);
    AssertEqual(1, message.TrackInsight.Timeline.Count);
    AssertEqual(1, message.TrackInsight.Tips.Count);
    AssertEqual(1, message.TrackInsight.Limitations.Count);
    AssertTrue(message.TrackInsight.Context.Any(row => row.Title == "Music DNA Match" && row.Detail == "86%"), "Music DNA match must be visible");
    AssertTrue(message.TrackInsight.Sections.Any(row => row.Title == "Energy"), "backend energy section must render");
    AssertTrue(message.TrackInsight.Tips[0].Meta.Contains("confidence: medium", StringComparison.Ordinal), "confidence must be visible");
}

static void AskDJTrackInsightRendersMetaBrainzMetadataContextSeparately()
{
    const string json = """
    {
      "id": "analysis-metabrainz",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "sources": [
        { "source": "metabrainz_metadata" }
      ],
      "track_insight": {
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
          { "id": "energy_profile", "title": "Energy", "value": 130, "source": "spotify_audio_features", "confidence": "high" },
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
    AssertEqual("0f4c2f62-2f7a-4f1d-a91d-01f3d3c00001", message!.TrackInsightData!.Metadata!.MusicBrainzRecordingId);
    AssertEqual(97, message.TrackInsightData.Metadata.MatchScore);
    AssertEqual("Blue Monday", message.TrackInsightData.Metadata.Release!.Title);
    AssertEqual("metabrainz_metadata", message.Sources![0].DisplayLabel);
    AssertEqual(2, message.TrackInsight!.Sections.Count);
    AssertTrue(!message.TrackInsight.Sections.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "metadata context must not render as measured section");
    AssertTrue(message.TrackInsight.HasContext, "metadata context should render in the context block");
    AssertTrue(message.TrackInsight.Context.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "context block should label MusicBrainz / ListenBrainz");
    AssertTrue(message.TrackInsight.Context.Any(row => row.Detail.Contains("12345", StringComparison.Ordinal)), "ListenBrainz listen count should be visible as context");
    AssertEqual(1, message.TrackInsight.Timeline.Count);
    AssertTrue(!message.TrackInsight.Timeline.Any(row => row.Title.Contains("MusicBrainz", StringComparison.OrdinalIgnoreCase)), "metadata context must not create fake timeline labels");
    AssertEqual(1, message.TrackInsight.Limitations.Count);
    AssertTrue(message.TrackInsight.Limitations[0].Subtitle.Contains("contextual", StringComparison.OrdinalIgnoreCase), "metadata caveat should stay visible");
}

static void AskDJTrackInsightWithoutMetadataRemainsCompatible()
{
    const string json = """
    {
      "id": "analysis-no-metadata",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 128 }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.TrackInsightData!.Metadata is null, "missing metadata must remain optional");
    AssertEqual(1, message.TrackInsight!.Sections.Count);
    AssertEqual(0, message.TrackInsight.Context.Count);
}

static void AskDJTrackInsightProvidersRenderAsDiagnostics()
{
    const string json = """
    {
      "id": "analysis-providers",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "mode": "available",
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 128 }
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
    AssertEqual(3, message!.TrackInsightData!.Providers!.Count);
    AssertEqual(1, message.TrackInsight!.Sections.Count);
    AssertEqual(1, message.TrackInsight.Timeline.Count);
    AssertEqual(1, message.TrackInsight.Tips.Count);
    AssertEqual(3, message.TrackInsight.ProviderDiagnostics.Count);
    AssertEqual("Spotify measured", message.TrackInsight.ProviderDiagnostics[0].Title);
    AssertEqual("used", message.TrackInsight.ProviderDiagnostics[0].Subtitle);
    AssertTrue(!message.TrackInsight.Sections.Any(row => row.Title.Contains("Spotify", StringComparison.OrdinalIgnoreCase)), "providers must not replace normal analysis UI blocks");
}

static void AskDJTrackInsightMetaBrainzProviderStatusesRemainDiagnostics()
{
    const string usedJson = """
    {
      "id": "analysis-metabrainz-provider-used",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 128 }
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
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 128 }
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
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 128 }
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
    AssertEqual("used", used!.TrackInsight!.ProviderDiagnostics[0].Subtitle);
    AssertEqual("skipped", skipped!.TrackInsight!.ProviderDiagnostics[0].Subtitle);
    AssertTrue(skipped.TrackInsight.ProviderDiagnostics[0].Meta.Contains("rate_limited", StringComparison.Ordinal), "rate limit reason should stay diagnostic");
    AssertEqual("error", error!.TrackInsight!.ProviderDiagnostics[0].Subtitle);
    AssertEqual(1, error.TrackInsight.Sections.Count);
    AssertEqual(0, error.TrackInsight.Context.Count);
}

static void AskDJTrackInsightWithoutProvidersRemainsCompatible()
{
    const string json = """
    {
      "id": "analysis-no-providers",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_curve", "summary": "Rustige intro, hoge piek na de break." }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertTrue(message!.TrackInsightData!.Providers is null, "missing providers must deserialize as optional metadata");
    AssertEqual(1, message.TrackInsight!.Sections.Count);
    AssertEqual(0, message.TrackInsight.ProviderDiagnostics.Count);
}

static void AskDJTrackInsightToleratesUnknownProviders()
{
    const string json = """
    {
      "id": "analysis-unknown-provider",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "energy_profile", "title": "Energy", "value": 126 }
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
    AssertEqual("future provider", message!.TrackInsight!.ProviderDiagnostics[0].Title);
    AssertEqual("deferred", message.TrackInsight.ProviderDiagnostics[0].Subtitle);
    AssertTrue(message.TrackInsight.ProviderDiagnostics[0].Meta.Contains("reason:", StringComparison.Ordinal), "unknown reasons can remain diagnostic metadata");
    AssertTrue(!message.TrackInsight.ProviderDiagnostics[0].Meta.Contains("secret-provider-token", StringComparison.Ordinal), "provider diagnostics must redact accidental secrets");
    AssertTrue(!message.TrackInsight.ProviderDiagnostics[0].Meta.Contains("raw_prompt", StringComparison.OrdinalIgnoreCase), "unknown provider fields must be ignored");
}

static void AskDJTrackInsightUnavailableRendersSkippedProviderDiagnostics()
{
    const string json = """
    {
      "id": "analysis-unavailable-providers",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
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
    AssertTrue(message!.TrackInsight!.IsUnavailable, "unavailable mode should still be visible");
    AssertEqual(0, message.TrackInsight.Sections.Count);
    AssertEqual(0, message.TrackInsight.Timeline.Count);
    AssertEqual(0, message.TrackInsight.Tips.Count);
    AssertEqual(3, message.TrackInsight.ProviderDiagnostics.Count);
    AssertTrue(message.HasTrackInsight, "provider diagnostics may keep the diagnostic track insight card visible");
}

static void AskDJTrackInsightV2WithoutTimelineRendersSections()
{
    const string json = """
    {
      "id": "analysis-2",
      "role": "assistant",
      "action": "track_insight",
      "track_insight": {
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
    AssertTrue(message!.IsTrackInsight, "track_insight action must be detected");
    AssertEqual(1, message.TrackInsight!.Sections.Count);
    AssertEqual(0, message.TrackInsight.Timeline.Count);
    AssertEqual("energy curve", message.TrackInsight.Sections[0].Title);
    AssertTrue(message.TrackInsight.Sections[0].Meta.Contains("confidence: low", StringComparison.Ordinal), "low confidence must stay visible");
}

static void AskDJTrackInsightUnavailableRendersLimitations()
{
    const string json = """
    {
      "id": "analysis-unavailable",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
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
    AssertTrue(message!.TrackInsight!.IsUnavailable, "unavailable mode should render a fallback card");
    AssertEqual(0, message.TrackInsight.Sections.Count);
    AssertEqual(1, message.TrackInsight.Limitations.Count);
}

static void AskDJTrackInsightSuppressesBpmAndKeyFields()
{
    const string json = """
    {
      "id": "analysis-no-bpm-key",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "analysis": {
          "sections": [
            {
              "id": "musical_measurements",
              "title": "Analysis",
              "items": [
                { "id": "bpm", "label": "BPM", "value": 128 },
                { "id": "musical_key", "label": "Key", "value": "A minor" },
                { "id": "energy_profile", "label": "Energy", "value": 76 }
              ]
            },
            { "id": "vibe", "title": "Vibe", "summary": "Driving and bright." }
          ]
        },
        "sections": [
          { "id": "tempo_bpm", "title": "BPM", "value": 128 },
          { "id": "key_signature", "title": "Key", "value": "A minor" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());
    var rendered = string.Join(" ", message!.TrackInsight!.Sections.Select(row => $"{row.Title} {row.Subtitle} {row.Detail}"));

    AssertTrue(!rendered.Contains("BPM", StringComparison.OrdinalIgnoreCase), "Track Insight must suppress BPM fields");
    AssertTrue(!rendered.Contains("A minor", StringComparison.OrdinalIgnoreCase), "Track Insight must suppress musical-key values");
    AssertTrue(!rendered.Contains("Key", StringComparison.OrdinalIgnoreCase), "Track Insight must suppress musical-key labels");
    AssertTrue(rendered.Contains("Energy", StringComparison.OrdinalIgnoreCase), "non-forbidden backend metrics should still render");
    AssertTrue(rendered.Contains("Vibe", StringComparison.OrdinalIgnoreCase), "non-forbidden backend sections should still render");
}

static void AskDJTrackInsightRendersUnknownValuesGenerically()
{
    const string json = """
    {
      "id": "analysis-unknown",
      "role": "assistant",
      "intent": { "intent": "track_insight" },
      "track_insight": {
        "contract_version": 2,
        "sections": [
          { "id": "spectral_flux_magic", "kind": "future_kind", "value": "wide", "source": "future_sensor", "confidence": "experimental" }
        ]
      }
    }
    """;

    var message = JsonSerializer.Deserialize<AskDJMessage>(json, JsonOptions());

    AssertNotNull(message);
    AssertEqual("spectral flux magic", message!.TrackInsight!.Sections[0].Title);
    AssertEqual("future kind", message.TrackInsight.Sections[0].Subtitle);
    AssertTrue(message.TrackInsight.Sections[0].Meta.Contains("future_sensor", StringComparison.Ordinal), "unknown source must render");
}

static void AskDJTrackInsightWithoutPlaybackActionsHasNoStaleButtons()
{
    const string json = """
    {
      "id": "analysis-no-actions",
      "role": "assistant",
      "action": "track_insight",
      "track_insight": {
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
              "image_url": "/api/djconnect/v1/image_proxy/token"
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

static void AskDJClearResponseFlagsClearLocalCache()
{
    const string json = """
    {
      "success": true,
      "cleared": true,
      "ask_dj_clear_required": true,
      "history_revision": 51,
      "clear_revision": 9,
      "messages": []
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJHistoryResponse>(json, JsonOptions());

    AssertNotNull(response);
    AssertTrue(response!.RequiresLocalClearAfterClearResponse(8), "clear response must clear local cache immediately");
    AssertTrue(response.RequiresLocalClearBeforeHistoryMerge(8), "higher clear revision must clear before merging history");
    AssertEqual(0, response.Messages.Count);
}

static void AskDJHistoryClearHttpSendsIdentity()
{
    var http = new FakeHttpHandler("""
    {
      "success": true,
      "cleared": true,
      "ask_dj_clear_required": true,
      "history_revision": 12,
      "clear_revision": 5,
      "messages": []
    }
    """);
    var client = NewClientWithFastPath(new FakeFastPath([]), http);
    client.Configure("http://homeassistant.local:8123", "device-token-123");

    var response = client.ClearAskDJHistoryAsync(TestIdentity(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.RequiresLocalClearAfterClearResponse(4), "HTTP clear response should require immediate local cache clear");
    AssertEqual("POST", http.LastMethod);
    AssertEqual("api/djconnect/v1/ask_dj/history/clear", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\""), "clear payload must include device_id");
    AssertTrue(http.LastBody.Contains("\"client_id\":\"djconnect-windows-ABC123DEF456\""), "clear payload must include client_id");
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "clear payload must preserve Windows client_type");
    AssertTrue(!http.LastBody.Contains("device-token-123", StringComparison.Ordinal), "clear HTTP body must not duplicate bearer token");
}

static void AskDJHistoryClearWebSocketSendsIdentity()
{
    var clearResponse = JsonSerializer.Deserialize<AskDJHistoryResponse>("""
    {
      "success": true,
      "cleared": true,
      "ask_dj_clear_required": true,
      "history_revision": 19,
      "clear_revision": 6,
      "messages": []
    }
    """, JsonOptions())!;
    var fastPath = new FakeFastPath(["djconnect/ask_dj/history/clear"])
        .WithResponse("djconnect/ask_dj/history/clear", clearResponse);
    var http = new FakeHttpHandler("""{"success":false,"history_revision":0,"clear_revision":0,"messages":[]}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.ClearAskDJHistoryAsync(TestIdentity(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.RequiresLocalClearAfterClearResponse(5), "websocket clear response should require immediate local cache clear");
    AssertEqual(0, http.RequestCount);
    AssertEqual("djconnect/ask_dj/history/clear", fastPath.Routes.Single());
    AssertEqual("djconnect-windows-ABC123DEF456", fastPath.LastPayload!["device_id"]);
    AssertEqual("djconnect-windows-ABC123DEF456", fastPath.LastPayload["client_id"]);
    AssertEqual("windows", fastPath.LastPayload["client_type"]);
    AssertEqual("device-token-123", fastPath.LastPayload["device_token"]);
}

static void AskDJHistoryExportUsesHttpServerEnvelope()
{
    const string exportEnvelope = """
    {
      "success": true,
      "format": "djconnect.ask_dj.history.export",
      "schema_version": 1,
      "exported_at": "2026-07-04T10:20:30Z",
      "exported_by_client_type": "windows",
      "app_version": "3.3.0",
      "user_id": "user-1",
      "history_revision": 12,
      "clear_revision": 2,
      "history_limit": 1000,
      "history_trimmed_before": null,
      "history_trimmed_count": 0,
      "messages": [],
      "server_time": "2026-07-04T10:20:31Z"
    }
    """;
    var fastPath = new FakeFastPath(["djconnect/ask_dj/history/export"]);
    var http = new FakeHttpHandler(exportEnvelope);
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var raw = client.ExportAskDJHistoryAsync(TestIdentity(), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(NormalizeJson(exportEnvelope), NormalizeJson(raw));
    AssertEqual("POST", http.LastMethod);
    AssertEqual("api/djconnect/v1/ask_dj/history/export", http.LastPath.TrimStart('/'));
    AssertEqual(0, fastPath.Routes.Count);
    AssertTrue(http.LastBody.Contains("\"identity\":", StringComparison.Ordinal), "export payload must include nested identity");
    AssertTrue(http.LastBody.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\"", StringComparison.Ordinal), "export identity must include device_id");
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\"", StringComparison.Ordinal), "export identity must include Windows client_type");
    AssertTrue(http.LastBody.Contains("\"device_name\":\"Studio PC\"", StringComparison.Ordinal), "export identity must include device name");
    AssertTrue(http.LastBody.Contains($"\"app_version\":\"{DJConnectContract.AppVersion}\"", StringComparison.Ordinal), "export payload must include app_version");
    AssertTrue(!http.LastBody.Contains("device-token-123", StringComparison.Ordinal), "export HTTP body must not duplicate bearer token");
}

static void AskDJHigherClearRevisionClearsBeforeMerge()
{
    const string json = """
    {
      "success": true,
      "history_revision": 30,
      "clear_revision": 11,
      "messages": [
        { "id": "fresh-assistant", "role": "assistant", "text": "Fresh history only." }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJHistoryResponse>(json, JsonOptions());
    var localMessages = new List<AskDJMessage>
    {
        new("old-user", "user", "old", null, DateTimeOffset.UtcNow, "user", null, null, null, null, null)
    };

    if (response!.RequiresLocalClearBeforeHistoryMerge(10))
    {
        localMessages.Clear();
    }

    localMessages.AddRange(response.Messages);

    AssertEqual(1, localMessages.Count);
    AssertEqual("fresh-assistant", localMessages[0].Id);
}

static void AskDJEmptyMessagesAfterClearDoNotRestoreOldMessages()
{
    const string json = """
    {
      "success": true,
      "cleared": true,
      "ask_dj_clear_required": true,
      "history_revision": 31,
      "clear_revision": 12,
      "messages": []
    }
    """;

    var response = JsonSerializer.Deserialize<AskDJHistoryResponse>(json, JsonOptions());
    var localMessages = new List<AskDJMessage>
    {
        new("old-assistant", "assistant", "old", null, DateTimeOffset.UtcNow, "assistant", null, null, null, null, null)
    };

    if (response!.RequiresLocalClearBeforeHistoryMerge(12))
    {
        localMessages.Clear();
    }

    localMessages.AddRange(response.Messages);

    AssertEqual(0, localMessages.Count);
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
    url=http://192.168.1.10:8123/api/djconnect/v1
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
    var sameMinor = VersionCompatibility.Evaluate("3.2", "3.2.2", null, false, null);
    var olderMinor = VersionCompatibility.Evaluate("3.2", "3.1.12", null, false, null);
    var newerMinor = VersionCompatibility.Evaluate("3.2", null, "3.3", false, null);
    var explicitMismatch = VersionCompatibility.Evaluate("3.2", "3.2.2", null, true, "version_mismatch");
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
    var repeated = new QueueItem("same", null, "Song", null, null, "Artist", null, null, "Album", null, 120_000, null, "uri:same", null, null, null, null, null, null, false, false, true, null);
    var items = new List<QueueItem> { repeated, repeated, new(null, null, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, true, null) };
    for (var i = 0; i < 150; i++)
    {
        items.Add(new QueueItem($"id-{i}", null, $"Song {i}", null, null, "Artist", null, null, null, null, 100_000, null, $"uri:{i}", null, null, null, null, null, null, false, false, true, null));
    }

    var normalized = QueueItemNormalizer.Normalize(items);

    AssertEqual(100, normalized.Count);
    AssertEqual("uri:same", normalized[0].StableId);
    AssertEqual("01", normalized[0].PositionLabel);
    AssertTrue(normalized.All(item => !string.IsNullOrWhiteSpace(item.DisplayTitleValue)), "queue normalization must skip untitled items");
    AssertEqual(normalized.Count, normalized.Select(item => item.StableId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
}

static void QueueCommandResponseSupportsArtistMetadataAndNestedShapes()
{
    const string nestedJson = """
        {
          "success": true,
          "queue": {
            "context_uri": "spotify:playlist:queue-context",
            "items": [
              {
                "id": "backend-id-1",
                "uri": "spotify:track:1",
                "title": "Nothing Else Matters",
                "artist_name": "Scala & Kolacny Brothers",
                "album_name": "Scala On The Rocks",
                "album_image_url": "https://example.com/album.jpg",
                "image_url": "https://example.com/image.jpg",
                "thumbnail_url": "https://example.com/thumb.jpg",
                "duration_ms": 312000
              }
            ]
          }
        }
        """;

    var nested = JsonSerializer.Deserialize<CommandResponse>(nestedJson, JsonOptions())!;
    var nestedItems = QueueItemNormalizer.Normalize(nested.ResolvedQueue());

    AssertEqual(1, nestedItems.Count);
    AssertEqual("Nothing Else Matters", nestedItems[0].DisplayTitleValue);
    AssertEqual("Scala & Kolacny Brothers", nestedItems[0].DisplaySubtitle);
    AssertEqual("Scala On The Rocks", nestedItems[0].DisplayAlbum);
    AssertEqual("https://example.com/album.jpg", nestedItems[0].Artwork);
    AssertEqual("spotify:track:1", nestedItems[0].StableId);
    AssertEqual("spotify:playlist:queue-context", nestedItems[0].ContextUri);

    const string flatJson = """
        {
          "success": true,
          "contextUri": "spotify:album:flat-context",
          "queue": [
            {
              "id": "backend-id-2",
              "title": "Fallback Song",
              "subtitle": "Subtitle Artist",
              "album": "Album Title",
              "thumbnail_url": "https://example.com/thumb.jpg"
            }
          ]
        }
        """;

    var flat = JsonSerializer.Deserialize<CommandResponse>(flatJson, JsonOptions())!;
    var flatItems = QueueItemNormalizer.Normalize(flat.ResolvedQueue());

    AssertEqual(1, flatItems.Count);
    AssertEqual("Subtitle Artist", flatItems[0].DisplaySubtitle);
    AssertEqual("Album Title", flatItems[0].DisplayAlbum);
    AssertEqual("https://example.com/thumb.jpg", flatItems[0].Artwork);
    AssertEqual("backend-id-2", flatItems[0].StableId);
    AssertEqual("spotify:album:flat-context", flatItems[0].ContextUri);
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

static void PairingCodeIsEnteredFromHomeAssistant()
{
    var settings = new AppSettings();

    AssertEqual("", settings.PairingCode);
}

static void PairingUiHasOutboundOnlyCopy()
{
    var xaml = File.ReadAllText(Path.Combine("src", "DJConnect.Windows", "MainPage.xaml"));
    var codeBehind = File.ReadAllText(Path.Combine("src", "DJConnect.Windows", "MainPage.xaml.cs"));
    var viewModel = File.ReadAllText(Path.Combine("src", "DJConnect.Windows", "ViewModels", "MainViewModel.cs"));

    AppStrings.UseLanguage("nl");
    AssertEqual("DJConnect koppelen", AppStrings.Get("Xaml_DJConnect_koppelen"));
    AssertTrue(AppStrings.Get("Xaml_Vul_of_scan_de_code_uit_Home_Assistant_terwi").Contains("Vul of scan de code uit Home Assistant", StringComparison.Ordinal), "pairing resource must explain QR/manual pairing");
    AssertTrue(xaml.Contains("Xaml_Lokale_Home_Assistant_URL", StringComparison.Ordinal), "pairing UI must ask for local HA URL through resources");
    AssertTrue(xaml.Contains("Xaml_Koppelcode", StringComparison.Ordinal), "pairing UI must ask for HA pair code through resources");
    AssertTrue(xaml.Contains("Xaml_Koppel_met_Home_Assistant", StringComparison.Ordinal), "pairing UI must expose the primary HA pairing action through resources");
    AssertTrue(xaml.Contains("IsEnabled=\"{Binding CanPair}\"", StringComparison.Ordinal), "pairing button must be disabled until input is valid");
    AssertTrue(xaml.Contains("Command=\"{Binding PairCommand}\"", StringComparison.Ordinal), "pairing UI must submit through PairCommand");
    AssertTrue(!xaml.Contains("Client adres", StringComparison.OrdinalIgnoreCase), "pairing UI must not show a Windows client address");
    AssertTrue(!xaml.Contains("Wacht op koppeling", StringComparison.OrdinalIgnoreCase), "pairing UI must not wait for an inbound Home Assistant callback");
    AssertTrue(!xaml.Contains("Koppelgegevens voor Home Assistant", StringComparison.OrdinalIgnoreCase), "pairing UI must not present data for HA to call back to Windows");
    AssertTrue(xaml.Contains("Xaml_Demo_Mode_starten", StringComparison.OrdinalIgnoreCase), "pairing UI should expose demo mode when supported");
    AssertTrue(!codeBehind.Contains("CopyPairingCode", StringComparison.OrdinalIgnoreCase), "Windows must not expose copy-code actions for app-generated pairing codes");
    AssertTrue(!viewModel.Contains("030610", StringComparison.Ordinal), "Windows must not ship a default pair code");
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

static void Protocol32RequiresNoLocalDeviceCallbackPath()
{
    var identity = TestIdentity();
    var commandPayload = DJConnectApiClient.BuildCommandPayload(identity, "status");
    var statusPayload = DJConnectApiClient.BuildStatusPayload(identity);

    AssertTrue(!commandPayload.ContainsKey("local_url"), "Windows command payload must not advertise a callback URL");
    AssertTrue(!statusPayload.ContainsKey("local_url"), "Windows status payload must not advertise a callback URL");
    AssertTrue(!commandPayload.Values.OfType<string>().Any(value => value.Contains("/api/device/", StringComparison.OrdinalIgnoreCase)), "Windows command payload must not expose /api/device paths");
}

static void Protocol32FallsBackFromLocalToRemoteAfterPairing()
{
    var manager = new HomeAssistantTransportManager((url, _) => Task.FromResult(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));
    manager.UpdateUrls("http://ha-local:8123", "https://remote.example", true);

    var state = manager.ResolveRuntimeAsync(CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(HomeAssistantConnectionMode.Remote, state.Mode);
    AssertEqual("https://remote.example", state.ActiveUrl);
}

static void PairingResponsePersistsRemoteUrlAndApiCapabilities()
{
    const string json = """
    {
      "success": true,
      "client_type": "windows",
      "device_id": "djconnect-windows-ABC123DEF456",
      "device_token": "secret-device-token",
      "ha_local_url": "http://ha-local:8123",
      "ha_remote_url": "https://example.ui.nabu.casa",
      "api_base": "/api/djconnect/v1",
      "voice_path": "/api/djconnect/v1/voice",
      "status_path": "/api/djconnect/v1/status",
      "event_path": "/api/djconnect/v1/event",
      "ask_dj_supported": true,
      "ask_dj_voice_supported": true,
      "ask_dj_audio_response_supported": true,
      "remote_supported": true
    }
    """;

    var response = JsonSerializer.Deserialize<PairingResponse>(json, JsonOptions());
    var manager = new HomeAssistantTransportManager((_, _) => Task.FromResult(true));
    manager.UpdateUrls(response!.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);

    AssertNotNull(response);
    AssertEqual("windows", response.ClientType);
    AssertEqual("djconnect-windows-ABC123DEF456", response.DeviceId);
    AssertEqual("secret-device-token", response.DeviceToken);
    AssertEqual("http://ha-local:8123", manager.Current.LocalUrl);
    AssertEqual("https://example.ui.nabu.casa", manager.Current.RemoteUrl);
    AssertTrue(manager.Current.RemoteSupported, "remote URL support must persist after local pairing");
    AssertEqual("/api/djconnect/v1", response.ApiBase);
    AssertEqual("/api/djconnect/v1/voice", response.VoicePath);
    AssertEqual("/api/djconnect/v1/status", response.StatusPath);
    AssertEqual("/api/djconnect/v1/event", response.EventPath);
    AssertTrue(response.AskDJSupported == true, "Ask DJ support must parse");
    AssertTrue(response.AskDJVoiceSupported == true, "Ask DJ voice support must parse");
    AssertTrue(response.AskDJAudioResponseSupported == true, "Ask DJ audio response support must parse");
}

static void StalePairingErrorsTriggerLocalCleanupPolicy()
{
    AssertTrue(PairingStatePolicy.RequiresLocalPairingCleanup("401 Unauthorized"), "401 must clear local pairing state");
    AssertTrue(PairingStatePolicy.RequiresLocalPairingCleanup("403 Forbidden"), "403 must clear local pairing state");
    AssertTrue(PairingStatePolicy.RequiresLocalPairingCleanup("not_configured"), "not_configured must clear local pairing state");
    AssertTrue(PairingStatePolicy.RequiresLocalPairingCleanup("stale pairing token"), "stale pairing must clear local pairing state");
    AssertTrue(!PairingStatePolicy.RequiresLocalPairingCleanup("timeout"), "network timeouts alone must not clear pairing state");
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

static void DJConnectHomeAssistantHttpRoutesUseCanonicalV1Prefix()
{
    var root = ProjectRoot();
    var paths = new[]
    {
        Path.Combine(root, "src"),
        Path.Combine(root, "tests"),
        Path.Combine(root, "docs"),
        Path.Combine(root, "README.md"),
        Path.Combine(root, "CHANGELOG.md"),
        Path.Combine(root, "CHAT_BOOTSTRAP.md")
    };
    var offenders = paths
        .SelectMany(RouteScanFiles)
        .SelectMany(file => File.ReadLines(file)
            .Select((line, index) => (File: file, LineNumber: index + 1, Line: line))
            .Where(row => HasLegacyDjConnectHttpRoute(row.Line)))
        .Select(row => $"{Path.GetRelativePath(root, row.File)}:{row.LineNumber}: {row.Line.Trim()}")
        .ToList();

    AssertTrue(offenders.Count == 0, "legacy /api/djconnect routes without /v1 remain:\n" + string.Join("\n", offenders));
}

static void ReleaseWorkflowPushesNotesFromCurrentHead()
{
    var workflow = File.ReadAllText(Path.Combine(ProjectRoot(), ".github", "workflows", "public-unsigned-release.yml"));

    AssertTrue(workflow.Contains("git fetch origin main --depth 1", StringComparison.Ordinal), "release notes publishing must refresh the remote release base");
    AssertTrue(workflow.Contains("git merge-base --is-ancestor origin/main HEAD", StringComparison.Ordinal), "release notes publishing must verify HEAD is based on origin/main");
    AssertTrue(workflow.Contains("git push origin HEAD:main", StringComparison.Ordinal), "release notes publishing must push the release commit explicitly");
    AssertTrue(!workflow.Contains("git push origin main", StringComparison.Ordinal), "release notes publishing must not rely on a potentially stale local main branch");
}

static void ReleaseWorkflowExtractsOnlyCurrentChangelogSection()
{
    var workflow = File.ReadAllText(Path.Combine(ProjectRoot(), ".github", "workflows", "public-unsigned-release.yml"));

    AssertTrue(workflow.Contains("extract_changelog_notes()", StringComparison.Ordinal), "release workflow should keep changelog extraction centralized");
    AssertTrue(workflow.Contains("$0 ~ \"^## \" version \"($| - )\" { capture=1; next }", StringComparison.Ordinal), "release notes must start at the matching version heading");
    AssertTrue(workflow.Contains("capture && /^## / { capture=0 }", StringComparison.Ordinal), "release notes must stop before the next changelog version");
    AssertTrue(!workflow.Contains("cat CHANGELOG.md", StringComparison.Ordinal), "release notes must not publish the whole changelog");
}

static void WindowsInteractiveGuiSmokeRelayIsIsolated()
{
    var root = ProjectRoot();
    var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "windows-post-deployment-smoke.yml"));
    var installer = File.ReadAllText(Path.Combine(root, "scripts", "runner", "Install-DJConnectInteractiveGuiSmokeRelay.ps1"));
    var relay = File.ReadAllText(Path.Combine(root, "scripts", "runner", "Invoke-DJConnectInteractiveGuiSmokeRelay.ps1"));

    AssertTrue(workflow.Contains("windows-internal-arm64-smoke-v2", StringComparison.Ordinal), "smoke must declare the interactive relay contract");
    AssertTrue(workflow.Contains("interactive-gui-smoke", StringComparison.Ordinal), "workflow must submit work to the relay rather than launch the GUI directly");
    AssertTrue(!workflow.Contains("Start-Process -FilePath $executable -PassThru", StringComparison.Ordinal), "service-runner workflow must not launch the GUI directly");
    AssertTrue(installer.Contains("/IT", StringComparison.Ordinal) && installer.Contains("/RL LIMITED", StringComparison.Ordinal), "relay task must be interactive and limited");
    AssertTrue(installer.Contains("${runnerIdentity}:(OI)(CI)M", StringComparison.Ordinal), "runner may write only relay requests");
    AssertTrue(installer.Contains("${InteractiveUser}:(OI)(CI)RX", StringComparison.Ordinal), "ACL variables before a colon must use explicit PowerShell delimiters");
    AssertTrue(relay.Contains("$process.SessionId -eq 0", StringComparison.Ordinal), "relay must reject session-zero GUI launch");
    AssertTrue(relay.Contains("Stop-Process -Id $process.Id", StringComparison.Ordinal), "relay must bound the launched GUI process");
}

static void ReleaseContextDocsMentionCurrentAppVersion()
{
    var root = ProjectRoot();
    var currentVersion = DJConnectContract.AppVersion;
    var files = new[]
    {
        Path.Combine(root, "README.md"),
        Path.Combine(root, "CHAT_BOOTSTRAP.md"),
        Path.Combine(root, "docs", "HANDOFF.md"),
        Path.Combine(root, "docs", "RELEASE.md"),
        Path.Combine(root, "docs", "TECHNICAL_DESIGN_DECISIONS.md"),
        Path.Combine(root, "docs", "TODO.md"),
        Path.Combine(root, "docs", "ISSUES.md")
    };

    var offenders = files
        .Select(file => (File: file, Text: File.ReadAllText(file)))
        .Where(item => !item.Text.Contains(currentVersion, StringComparison.Ordinal))
        .Select(item => Path.GetRelativePath(root, item.File))
        .ToList();

    AssertTrue(offenders.Count == 0, "release context docs must mention current app version " + currentVersion + ":\n" + string.Join("\n", offenders));
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

static void CommandPayloadIncludesCurrentLocaleAndPreservesProtocolValues()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildCommandPayload(identity, "ask_dj_followup_response", new { response = "yes" }, "msg-command-1", "en_GB", 72);
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"language\":\"en-GB\""), "command payload must include BCP-47 language");
    AssertTrue(serialized.Contains("\"locale\":\"en-GB\""), "command payload must include BCP-47 locale");
    AssertTrue(serialized.Contains("\"mood\":72"), "command payload must include current mood when available");
    AssertTrue(serialized.Contains("\"command\":\"ask_dj_followup_response\""), "command name must remain a protocol value");
    AssertTrue(serialized.Contains("\"client_type\":\"windows\""), "client_type must remain the Windows protocol value");
}

static void CommandPayloadCarriesDjAnnouncementOutputOnly()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildCommandPayload(identity, "ask_dj_followup_response", new { response = "yes" }, "msg-command-1", "nl", 42, DJAnnouncementOutput.HaSpeaker);
    var serialized = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(serialized.Contains("\"dj_announcement_output\":\"ha_speaker\""), "client setting must send the selected announcement output");
    AssertTrue(!serialized.Contains("dj_announcement_speaker_entity_id", StringComparison.OrdinalIgnoreCase), "Windows must never send HA speaker entity settings");
    AssertTrue(!serialized.Contains("media_player.", StringComparison.OrdinalIgnoreCase), "Windows command payload must not include HA speaker entity IDs");
}

static void RawVoiceUploadIncludesLanguageHeaders()
{
    var http = new FakeHttpHandler("""{"success":true,"message":"voice ok"}""");
    var client = new DJConnectApiClient(new HttpClient(http));
    client.Configure("http://homeassistant.local:8123", "paired-device-token");
    using var wav = new MemoryStream([0x52, 0x49, 0x46, 0x46]);

    var response = client.SendAskDJVoiceAsync(TestIdentity(), wav, new AskDJVoiceRequest("voice-1", Language: "fr"), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "voice response should deserialize");
    AssertEqual("api/djconnect/v1/voice", http.LastPath.TrimStart('/'));
    AssertEqual("fr-FR", http.LastLanguageHeader);
    AssertEqual("fr-FR", http.LastLocaleHeader);
    AssertTrue(http.LastBody.Contains("name=language"), "multipart voice upload must include language field");
    AssertTrue(http.LastBody.Contains("fr-FR"), "multipart voice upload must include BCP-47 locale");
    AssertTrue(http.LastBody.Contains("name=client_type"), "multipart voice upload must preserve client_type field");
    AssertTrue(http.LastBody.Contains("windows"), "multipart voice upload must preserve Windows client type");
}

static void TransportOptionsRequireLocalHaWebSocketAuthOptIn()
{
    var disabled = new DJConnectTransportOptions(false, "ha-ws-token");
    var missingToken = new DJConnectTransportOptions(true, null);
    var enabled = new DJConnectTransportOptions(true, "ha-ws-token");

    AssertTrue(!disabled.AllowsWebSocketFastPath(HomeAssistantConnectionMode.Local), "feature flag must gate websocket");
    AssertTrue(!missingToken.AllowsWebSocketFastPath(HomeAssistantConnectionMode.Local), "HA websocket auth token must be required");
    AssertTrue(!enabled.AllowsWebSocketFastPath(HomeAssistantConnectionMode.Remote), "remote sessions must stay HTTP by default");
    AssertTrue(enabled.AllowsWebSocketFastPath(HomeAssistantConnectionMode.Local), "local opt-in with HA auth token should allow websocket");
}

static void FastPathDiagnosticsFormatterRendersSafeSummary()
{
    var diagnostics = new FastPathDiagnostics("http", false, "timeout", null, ["djconnect/command"]);
    var body = new System.Text.StringBuilder();

    FastPathDiagnosticsFormatter.AppendTo(body, diagnostics);

    AssertEqual("http fallback", FastPathDiagnosticsFormatter.AboutText(diagnostics));
    AssertTrue(body.ToString().Contains("- Fast path transport: http"), "diagnostic export should include transport");
    AssertTrue(body.ToString().Contains("- WebSocket commands: djconnect/command"), "diagnostic export should include capabilities");
    AssertTrue(!body.ToString().Contains("token", StringComparison.OrdinalIgnoreCase), "diagnostic formatter must not introduce token fields");
}

static void WebSocketFastPathDetectsCapabilities()
{
    var fastPath = new FakeFastPath(["djconnect/command", "djconnect/ask_dj/message"]);
    var client = NewClientWithFastPath(fastPath, new FakeHttpHandler("{}"));
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    AssertTrue(client.FastPathDiagnostics.WebSocketConnected, "configured local websocket should report connected fake fast path");
    AssertEqual(2, client.FastPathDiagnostics.WebSocketCommands.Count);
    AssertTrue(client.FastPathDiagnostics.WebSocketCommands.Contains("djconnect/command"), "capabilities must include command route");
}

static void WebSocketFastPathStaysDisabledWithoutHaAuthToken()
{
    var fastPath = new FakeFastPath(["djconnect/command"]);
    var http = new FakeHttpHandler("""{"success":true,"message":"http default"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true);

    var response = client.RunCommandAsync(TestIdentity(), "play", CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "HTTP should remain the safe default without HA websocket auth");
    AssertEqual("http default", response.Message);
    AssertEqual(0, fastPath.Attempts);
    AssertEqual(1, http.RequestCount);
    AssertTrue(!client.FastPathDiagnostics.WebSocketConnected, "fast path should stay disconnected without HA auth token");
}

static void WebSocketCommandSuccessSkipsHttp()
{
    var fastPath = new FakeFastPath(["djconnect/command"])
        .WithResponse("djconnect/command", new CommandResponse(true, "ws ok", "ws ok", null));
    var http = new FakeHttpHandler("""{"success":true,"message":"http ok"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.RunCommandAsync(TestIdentity(), "play", CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "websocket command response should be used");
    AssertEqual("ws ok", response.Message);
    AssertEqual(0, http.RequestCount);
    AssertEqual("djconnect/command", fastPath.Routes.Single());
}

static void WebSocketCommandPayloadIncludesCurrentLocale()
{
    var fastPath = new FakeFastPath(["djconnect/command"])
        .WithResponse("djconnect/command", new CommandResponse(true, "ws ok", "ws ok", null));
    var client = NewClientWithFastPath(fastPath, new FakeHttpHandler("""{"success":true,"message":"http ok"}"""));
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    _ = client.RunCommandAsync(TestIdentity(), "ask_dj_followup_response", "es", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("ask_dj_followup_response", fastPath.LastPayload!["command"]);
    AssertEqual("es-ES", fastPath.LastPayload["language"]);
    AssertEqual("es-ES", fastPath.LastPayload["locale"]);
    AssertEqual("windows", fastPath.LastPayload["client_type"]);
}

static void WebSocketMissingCapabilityFallsBackToHttp()
{
    var fastPath = new FakeFastPath(["djconnect/ask_dj/message"]);
    var http = new FakeHttpHandler("""{"success":true,"message":"http ok"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.RunCommandAsync(TestIdentity(), "pause", CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "HTTP fallback should preserve command behavior");
    AssertEqual("http ok", response.Message);
    AssertEqual(1, http.RequestCount);
    AssertEqual(1, fastPath.Attempts);
}

static void WebSocketAskDJMessageSuccessUsesRevisions()
{
    var askResponse = JsonSerializer.Deserialize<AskDJMessageResponse>("""
    {
      "success": true,
      "history_revision": 77,
      "clear_revision": 3,
      "messages": [
        { "id": "assistant-1", "role": "assistant", "text": "Track Insight staat klaar." }
      ]
    }
    """, JsonOptions())!;
    var fastPath = new FakeFastPath(["djconnect/ask_dj/message"]).WithResponse("djconnect/ask_dj/message", askResponse);
    var http = new FakeHttpHandler("""{"success":true,"history_revision":1}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.SendAskDJMessageAsync(TestAskRequest("Tell me about this track"), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual(77, response.HistoryRevision);
    AssertEqual(3, response.ClearRevision);
    AssertEqual(0, http.RequestCount);
    AssertEqual("Tell me about this track", fastPath.LastPayload!["text"]);
}

static void WebSocketAskDJPayloadIncludesCurrentLocale()
{
    var askResponse = JsonSerializer.Deserialize<AskDJMessageResponse>("""
    { "success": true, "message": "ok" }
    """, JsonOptions())!;
    var fastPath = new FakeFastPath(["djconnect/ask_dj/message"]).WithResponse("djconnect/ask_dj/message", askResponse);
    var client = NewClientWithFastPath(fastPath, new FakeHttpHandler("""{"success":true}"""));
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");
    var request = TestAskRequest("Was draait er?") with
    {
        Language = "de-DE",
        Locale = "de-DE"
    };

    _ = client.SendAskDJMessageAsync(request, CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("de-DE", fastPath.LastPayload!["language"]);
    AssertEqual("de-DE", fastPath.LastPayload["locale"]);
    AssertEqual("windows", fastPath.LastPayload["client_type"]);
}

static void WebSocketTrackInsightSuccessRendersMusicDna()
{
    var trackInsight = new TrackInsightResponse(
        true,
        new TrackInsightResult(
            new TrackInsightTrack("Strobe", "deadmau5", "For Lack of a Better Name"),
            2,
            "available",
            "ha",
            "high",
            new TrackInsightAnalysis([], null, null, null, null),
            null,
            null,
            null,
            null,
            null,
            new TrackInsightMusicDna(91, "This expands your Music DNA.", null),
            new TrackInsightVisualProfile("Energetic", ["red"], "pulse"),
            new TrackInsightCache(false, DateTimeOffset.UtcNow),
            null));
    var fastPath = new FakeFastPath(["djconnect/track_insight"]).WithResponse("djconnect/track_insight", trackInsight);
    var http = new FakeHttpHandler("""{"success":false,"error":"http_should_not_run"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.GetTrackInsightAsync(TestTrackInsightRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "websocket Track Insight should return success");
    AssertEqual(91, response.TrackInsight!.MusicDna!.MatchPercent);
    AssertEqual(0, http.RequestCount);
}

static void TrackInsightPayloadIncludesIdentityAndTitleArtist()
{
    var http = new FakeHttpHandler("""
    {
      "success": false,
      "error": "no_track_playing",
      "message": "No track playing."
    }
    """);
    var client = NewClientWithFastPath(new FakeFastPath([]), http);
    client.Configure("http://homeassistant.local:8123", "device-token-123");

    _ = client.GetTrackInsightAsync(TestTrackInsightRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("POST", http.LastMethod);
    AssertEqual("api/djconnect/v1/track_insight", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\""), "Track Insight payload must include device_id");
    AssertTrue(http.LastBody.Contains("\"client_id\":\"djconnect-windows-ABC123DEF456\""), "Track Insight payload must include client_id");
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "Track Insight payload must preserve Windows client_type");
    AssertTrue(http.LastBody.Contains("\"title\":\"Strobe\""), "Track Insight payload must prefer title");
    AssertTrue(http.LastBody.Contains("\"artist\":\"deadmau5\""), "Track Insight payload must prefer artist");
    AssertTrue(!http.LastBody.Contains("track_name", StringComparison.OrdinalIgnoreCase), "Track Insight request should not send track_name alias when title is known");
    AssertTrue(!http.LastBody.Contains("artist_name", StringComparison.OrdinalIgnoreCase), "Track Insight request should not send artist_name alias when artist is known");
}

static void TrackInsightPayloadIncludesMoodAndMusicDnaKey()
{
    var fastPath = new FakeFastPath(["djconnect/track_insight"]) { Error = "force-http" };
    var http = new FakeHttpHandler("""{"success":false,"error":"no_track_playing"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    _ = client.GetTrackInsightAsync(TestTrackInsightRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(http.LastBody.Contains("\"mood\":72"), "Track Insight HTTP payload must include mood context");
    AssertTrue(http.LastBody.Contains("\"music_dna_key\":\"dna-studio\""), "Track Insight HTTP payload must include music_dna_key");
    AssertEqual(72, (int?)fastPath.LastPayload!["mood"]);
    AssertEqual("dna-studio", fastPath.LastPayload!["music_dna_key"]);
}

static void MusicDnaProfileDecodesMoodAndEnergyBackendShapes()
{
    const string json = """
    {
      "success": true,
      "enabled": true,
      "profile": {
        "summary": "Backend summary",
        "mood": { "average": 57, "average_zone": "Groove", "sample_count": 3 },
        "energy_profile": { "energy_percent": 68, "zone": "Energy", "sample_count": 4, "danceability": 71, "intensity": 64 },
        "recent_tracks": [
          { "title": "Strobe", "artist": "deadmau5", "album": "For Lack of a Better Name" }
        ],
        "based_on": "recent listening"
      }
    }
    """;

    var response = JsonSerializer.Deserialize<MusicDnaProfileResponse>(json, JsonOptions());
    var profile = response!.Profile!;

    AssertEqual("Groove gemiddeld · 57% · 3 signalen", profile.ResolvedMoodProfile!.MoodSummary);
    AssertEqual("Energy · 68% · danceability 71% · intensity 64%", profile.EnergyProfile!.EnergySummary);
    AssertEqual("Strobe — deadmau5 · For Lack of a Better Name", profile.RecentTracks![0].DisplaySubtitle);
    AssertEqual("recent listening", profile.BasedOn);
}

static void MusicDnaDashboardHidesDisabledAndEmptyBlocks()
{
    const string disabledJson = """
    { "success": true, "enabled": false, "profile": {} }
    """;
    const string summaryOnlyJson = """
    {
      "success": true,
      "enabled": true,
      "profile": {
        "summary": "Server summary",
        "favorite_genres": [],
        "repeat_magnets": { "eligible": false, "reason": "not enough data", "items": [ { "title": "Hidden" } ] },
        "taste_anchors": { "eligible": true, "items": [] },
        "blocked_artists": [ "Hidden Artist" ]
      }
    }
    """;

    var disabled = MusicDnaDashboard.From(JsonSerializer.Deserialize<MusicDnaProfileResponse>(disabledJson, JsonOptions())!);
    var summaryOnly = MusicDnaDashboard.From(JsonSerializer.Deserialize<MusicDnaProfileResponse>(summaryOnlyJson, JsonOptions())!);

    AssertTrue(disabled.IsDisabled, "enabled:false profile should render opt-in state");
    AssertEqual(0, disabled.Blocks.Count);
    AssertEqual("Server summary", summaryOnly.Summary);
    AssertEqual(0, summaryOnly.Blocks.Count);
}

static void MusicDnaDashboardRendersOptionalEligibleBlocks()
{
    const string json = """
    {
      "success": true,
      "enabled": true,
      "profile": {
        "summary": "Server summary",
        "favorite_genres": [ { "name": "Synth-pop" } ],
        "favorite_artists": [ { "name": "M83" } ],
        "recent_tracks": [ { "title": "Midnight City", "artist": "M83", "album": "Hurry Up" } ],
        "recent_favorite_tracks": [ { "title": "Innerbloom", "artist": "RÜFÜS DU SOL" } ],
        "playtime": {
          "total_seconds": 3661,
          "formatted_total": "1 h 1 min",
          "top_artists": [ { "name": "M83", "formatted_total": "30 min" } ],
          "top_albums": [ { "title": "Hurry Up", "artist": "M83" } ]
        },
        "listening_rhythm": {
          "sample_count": 3,
          "top_daypart": "evening",
          "top_weekday": "Friday",
          "dayparts": { "evening": 2 },
          "weekdays": { "Friday": 2 }
        },
        "mood_mix": { "sample_count": 4, "chill": 10, "groove": 40, "energy": 35, "party": 15 },
        "energy_profile": { "energy_percent": 68, "zone": "Energy", "sample_count": 4 },
        "repeat_magnets": { "eligible": true, "items": [ { "title": "Strobe", "artist": "deadmau5" } ] },
        "explicit_positives": {
          "eligible": true,
          "favorites": [ { "title": "Sweet Disposition", "artist": "The Temper Trap" } ],
          "recommendations": [ { "title": "1901", "artist": "Phoenix" } ]
        },
        "taste_anchors": { "eligible": true, "items": [ { "name": "night drive synths" } ] },
        "recommendation_signals": { "items": [ { "name": "melodic electronic" } ] }
      }
    }
    """;

    var dashboard = MusicDnaDashboard.From(JsonSerializer.Deserialize<MusicDnaProfileResponse>(json, JsonOptions())!);
    var titles = dashboard.Blocks.Select(block => block.Title).ToArray();

    AssertTrue(titles.Contains("Favorite genres"), "favorite genres should render when non-empty");
    AssertTrue(titles.Contains("Recent favorites"), "recent favorite tracks should render when non-empty");
    AssertTrue(dashboard.Blocks.Any(block => block.Title == "Playtime" && block.Detail == "1 h 1 min"), "playtime should use backend formatted_total");
    AssertTrue(dashboard.Blocks.Any(block => block.Title == "Listening rhythm"), "listening rhythm should render when sample_count >= 3");
    AssertTrue(dashboard.Blocks.Any(block => block.Title == "Mood mix" && block.Items.Count == 4), "mood mix distribution should render");
    AssertTrue(titles.Contains("Repeat magnets"), "eligible repeat magnets should render");
    AssertTrue(titles.Contains("Explicit positives"), "eligible explicit positives should render");
    AssertTrue(titles.Contains("Taste anchors"), "eligible taste anchors should render");
    AssertTrue(titles.Contains("Recommendation signals"), "recommendation signals should render when non-empty");
    AssertTrue(!titles.Contains("Blocked artists"), "blocked artists must not render as a dashboard card");
}

static void MusicDnaSettingsAndClearPayloadsIncludeIdentityContext()
{
    var settingsHttp = new FakeHttpHandler("""{"success":true,"enabled":true}""");
    var settingsClient = NewClientWithFastPath(new FakeFastPath([]), settingsHttp);
    settingsClient.Configure("http://homeassistant.local:8123", "device-token-123");

    var settings = new MusicDnaSettingsRequest(
        "djconnect-windows-ABC123DEF456",
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        true,
        "nl-NL",
        "nl-NL",
        72,
        "dna-studio");
    _ = settingsClient.UpdateMusicDnaSettingsAsync(settings, CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("api/djconnect/v1/music_dna/settings", settingsHttp.LastPath.TrimStart('/'));
    AssertTrue(settingsHttp.LastBody.Contains("\"enabled\":true"), "settings payload must include enabled:true");
    AssertTrue(settingsHttp.LastBody.Contains("\"client_type\":\"windows\""), "settings payload must include windows client_type");
    AssertTrue(settingsHttp.LastBody.Contains("\"music_dna_key\":\"dna-studio\""), "settings payload must include music_dna_key");

    var clearHttp = new FakeHttpHandler("""{"success":true,"enabled":true}""");
    var clearClient = NewClientWithFastPath(new FakeFastPath([]), clearHttp);
    clearClient.Configure("http://homeassistant.local:8123", "device-token-123");
    _ = clearClient.ClearMusicDnaAsync(new MusicDnaClearRequest(
        "djconnect-windows-ABC123DEF456",
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "nl-NL",
        "nl-NL",
        72,
        "dna-studio"), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("api/djconnect/v1/music_dna/clear", clearHttp.LastPath.TrimStart('/'));
    AssertTrue(clearHttp.LastBody.Contains("\"client_type\":\"windows\""), "clear payload must include windows client_type");
    AssertTrue(clearHttp.LastBody.Contains("\"music_dna_key\":\"dna-studio\""), "clear payload must include music_dna_key");
}

static void WebSocketMusicDnaProfileSuccessSkipsHttp()
{
    var profile = new MusicDnaProfileResponse(true, true, new MusicDnaProfile(
        "Server summary",
        FavoriteGenres: null,
        FavoriteArtists: null,
        RecentTracks: null,
        RecentFavoriteTracks: null,
        EnergyProfile: null,
        MoodProfile: null,
        Mood: null,
        TasteDirection: null,
        BasedOn: null,
        UpdatedAt: null));
    var fastPath = new FakeFastPath(["djconnect/music_dna/profile"]).WithResponse("djconnect/music_dna/profile", profile);
    var http = new FakeHttpHandler("""{"success":false,"error":"http_should_not_run"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.GetMusicDnaProfileAsync(TestMusicDnaProfileRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "websocket Music DNA profile should return success");
    AssertEqual("Server summary", response.Profile!.Summary);
    AssertEqual(0, http.RequestCount);
    AssertEqual("dna-studio", fastPath.LastPayload!["music_dna_key"]);
}

static void WebSocketMusicDnaUnsupportedFallsBackToHttp()
{
    var fastPath = new FakeFastPath(["djconnect/command"]);
    var http = new FakeHttpHandler("""{"success":true,"enabled":false,"profile":{}}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.GetMusicDnaProfileAsync(TestMusicDnaProfileRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "HTTP fallback should run when websocket route is unsupported");
    AssertEqual(1, http.RequestCount);
    AssertEqual("api/djconnect/v1/music_dna/profile", http.LastPath.TrimStart('/'));
}

static void DiscoverNavAppearsInAppleOrder()
{
    var xaml = File.ReadAllText(Path.Combine("src", "DJConnect.Windows", "MainPage.xaml"));
    var now = xaml.IndexOf("NowPlayingNavButton", StringComparison.Ordinal);
    var ask = xaml.IndexOf("AskDJNavButton", StringComparison.Ordinal);
    var insight = xaml.IndexOf("TrackInsightNavButton", StringComparison.Ordinal);
    var discover = xaml.IndexOf("DiscoverNavButton", StringComparison.Ordinal);
    var dna = xaml.IndexOf("MusicDnaNavButton", StringComparison.Ordinal);
    var settings = xaml.IndexOf("SettingsNavButton", StringComparison.Ordinal);

    AssertTrue(now >= 0 && ask > now && insight > ask && discover > insight && dna > discover && settings > dna, "main nav must order Playback, Ask DJ, Track Insight, Ontdek, Music DNA, Settings");
    AssertTrue(xaml.Contains("Text=\"Ontdek\"", StringComparison.Ordinal), "Ontdek nav item should be visible");
}

static void DiscoverConsentUiIsWiredToMusicDnaCommands()
{
    var xaml = File.ReadAllText(Path.Combine("src", "DJConnect.Windows", "MainPage.xaml"));

    AssertTrue(xaml.Contains("IsDiscoverConsentVisible", StringComparison.Ordinal), "Discover consent panel should be visible while Music DNA is disabled");
    AssertTrue(xaml.Contains("EnableDiscoverMusicDnaCommand", StringComparison.Ordinal), "Discover consent accept action should enable Music DNA");
    AssertTrue(xaml.Contains("RejectDiscoverConsentCommand", StringComparison.Ordinal), "Discover consent reject action should be available");
    AssertTrue(xaml.Contains("RefreshDiscoverCommand", StringComparison.Ordinal), "Discover page should expose refresh");
    AssertTrue(xaml.Contains("Ontdek werkt alleen als Music DNA is geactiveerd.", StringComparison.Ordinal), "Discover disabled copy should match the contract");
}

static void MusicDiscoveryFeedRendersSupportedItemsAndReasons()
{
    const string json = """
    {
      "success": true,
      "enabled": true,
      "items": [
        { "id": "t1", "kind": "track", "title": "Midnight City", "artist": "M83", "artwork_url": "https://example.com/m.jpg", "confidence": "high", "reason": "You like neon synths." },
        { "id": "a1", "kind": "album", "title": "Bloom", "artist": "RÜFÜS DU SOL" },
        { "id": "ar1", "kind": "artist", "title": "ODESZA" },
        { "id": "p1", "kind": "playlist", "title": "Night drive" },
        { "id": "x1", "kind": "podcast", "title": "Hidden" }
      ]
    }
    """;

    var response = JsonSerializer.Deserialize<MusicDiscoveryResponse>(json, JsonOptions())!;
    var items = response.DisplayItems;

    AssertEqual(4, items.Count);
    AssertEqual("Midnight City", items[0].DisplayTitle);
    AssertEqual("M83", items[0].DisplaySubtitle);
    AssertTrue(items[0].HasArtwork, "artwork should render when backend provides it");
    AssertTrue(items[0].HasReason, "reason UI should be available when backend reason exists");
    AssertTrue(!items[1].HasReason, "reason UI should be hidden when backend reason is missing");
}

static void MusicDiscoveryDisabledResponseDoesNotRenderFeed()
{
    const string json = """
    {
      "success": true,
      "enabled": false,
      "items": [
        { "id": "t1", "kind": "track", "title": "Hidden recommendation" }
      ],
      "empty_state": "Enable Music DNA first."
    }
    """;

    var response = JsonSerializer.Deserialize<MusicDiscoveryResponse>(json, JsonOptions())!;

    AssertTrue(!response.CanRenderFeed, "disabled Music Discovery responses must not render as a feed");
    AssertEqual("Enable Music DNA first.", response.EmptyState);
}

static void MusicDiscoveryFeedRequestCarriesIdentityQuery()
{
    var http = new FakeHttpHandler("""{"success":true,"enabled":true,"items":[]}""");
    var client = NewClientWithFastPath(new FakeFastPath([]), http);
    client.Configure("http://homeassistant.local:8123", "device-token-123");

    _ = client.GetMusicDiscoveryAsync(TestMusicDiscoveryRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("GET", http.LastMethod);
    AssertTrue(http.LastPath.StartsWith("/api/djconnect/v1/music_discovery?", StringComparison.Ordinal), "feed must use GET query parameters");
    AssertTrue(http.LastPath.Contains("client_type=windows", StringComparison.Ordinal), "feed query must include windows client type");
    AssertTrue(http.LastPath.Contains("device_id=djconnect-windows-ABC123DEF456", StringComparison.Ordinal), "feed query must include device id");
    AssertTrue(http.LastPath.Contains("music_dna_key=dna-studio", StringComparison.Ordinal), "feed query must include Music DNA key");
    AssertEqual("", http.LastBody);
}

static void WebSocketMusicDiscoveryFeedSuccessSkipsHttp()
{
    var wsResponse = new MusicDiscoveryResponse(true, true, [new MusicDiscoveryItem("ws-feed-1", null, "playlist", null, "Studio warmup", null, null, null, null, null, "spotify:playlist:1", null, "medium", "Because this fits your recent plays.", null)]);
    var fastPath = new FakeFastPath(["djconnect/music_discovery/feed"]).WithResponse("djconnect/music_discovery/feed", wsResponse);
    var http = new FakeHttpHandler("""{"success":false,"error":"http_should_not_run"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var result = client.GetMusicDiscoveryAsync(TestMusicDiscoveryRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(result.Success, "websocket discovery feed should succeed");
    AssertEqual(0, http.RequestCount);
    AssertEqual("windows", fastPath.LastPayload!["client_type"]);
    AssertEqual("dna-studio", fastPath.LastPayload!["music_dna_key"]);
}

static void MusicDiscoveryRefreshUsesEndpointAndWebSocket()
{
    var wsResponse = new MusicDiscoveryResponse(true, true, [new MusicDiscoveryItem("ws-1", null, "track", null, "Strobe", null, "deadmau5", null, null, null, "spotify:track:1", null, "high", null, null)]);
    var fastPath = new FakeFastPath(["djconnect/music_discovery/refresh"]).WithResponse("djconnect/music_discovery/refresh", wsResponse);
    var wsHttp = new FakeHttpHandler("""{"success":false,"error":"http_should_not_run"}""");
    var wsClient = NewClientWithFastPath(fastPath, wsHttp);
    wsClient.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var wsResult = wsClient.RefreshMusicDiscoveryAsync(TestMusicDiscoveryRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(wsResult.Success, "websocket discovery refresh should succeed");
    AssertEqual(0, wsHttp.RequestCount);
    AssertEqual("windows", fastPath.LastPayload!["client_type"]);

    var http = new FakeHttpHandler("""{"success":true,"enabled":true,"items":[]}""");
    var client = NewClientWithFastPath(new FakeFastPath([]), http);
    client.Configure("http://homeassistant.local:8123", "device-token-123");

    _ = client.RefreshMusicDiscoveryAsync(TestMusicDiscoveryRequest(), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("POST", http.LastMethod);
    AssertEqual("api/djconnect/v1/music_discovery/refresh", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "refresh payload must include client_type");
}

static void MusicDiscoveryPlaySendsSourceAndIdentity()
{
    var http = new FakeHttpHandler("""{"success":true,"message":"started"}""");
    var client = NewClientWithFastPath(new FakeFastPath([]), http);
    client.Configure("http://homeassistant.local:8123", "device-token-123");
    var request = new MusicDiscoveryPlayRequest(
        "djconnect-windows-ABC123DEF456",
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "rec-1",
        "item-1",
        "track",
        "spotify:track:abc",
        "spotify:track:abc",
        "music_discovery",
        "en",
        "en",
        72,
        "dna-studio");

    _ = client.PlayMusicDiscoveryAsync(request, CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("api/djconnect/v1/music_discovery/play", http.LastPath.TrimStart('/'));
    AssertTrue(http.LastBody.Contains("\"source\":\"music_discovery\""), "play payload must identify music_discovery source");
    AssertTrue(http.LastBody.Contains("\"client_type\":\"windows\""), "play payload must include windows client type");
    AssertTrue(http.LastBody.Contains("\"recommendation_id\":\"rec-1\""), "play payload must include recommendation id");
}

static void WebSocketTimeoutFallsBackToHttpExactlyOnce()
{
    var fastPath = new FakeFastPath(["djconnect/command"]) { Error = "timeout" };
    var http = new FakeHttpHandler("""{"success":true,"message":"http fallback"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.RunCommandAsync(TestIdentity(), "next", CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "HTTP fallback should run after timeout");
    AssertEqual("http fallback", response.Message);
    AssertEqual(1, fastPath.Attempts);
    AssertEqual(1, http.RequestCount);
}

static void WebSocketAuthErrorFallsBackToHttp()
{
    var fastPath = new FakeFastPath(["djconnect/ask_dj/message"]) { Error = "auth" };
    var http = new FakeHttpHandler("""{"success":true,"message":"http ask"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("http://homeassistant.local:8123", "device-token-123", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    var response = client.SendAskDJMessageAsync(TestAskRequest("Analyze this track"), CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "HTTP fallback should run after websocket auth error");
    AssertEqual("http ask", response.Message);
    AssertEqual(1, http.RequestCount);
}

static void RemoteConnectionStaysHttp()
{
    var fastPath = new FakeFastPath(["djconnect/command"]);
    var http = new FakeHttpHandler("""{"success":true,"message":"remote http"}""");
    var client = NewClientWithFastPath(fastPath, http);
    client.Configure("https://example.ui.nabu.casa", "device-token-123", enableLocalWebSocketFastPath: true);

    var response = client.RunCommandAsync(TestIdentity(), "previous", CancellationToken.None).GetAwaiter().GetResult();

    AssertTrue(response.Success, "remote sessions must keep HTTP transport");
    AssertEqual("remote http", response.Message);
    AssertEqual(0, fastPath.Attempts);
    AssertEqual(1, http.RequestCount);
}

static void WebSocketPayloadIncludesIdentityAndTokenWithoutDiagnosticLeaks()
{
    var askResponse = JsonSerializer.Deserialize<AskDJMessageResponse>("""
    { "success": true, "message": "ok" }
    """, JsonOptions())!;
    var fastPath = new FakeFastPath(["djconnect/ask_dj/message"])
        .WithResponse("djconnect/ask_dj/message", askResponse);
    var client = NewClientWithFastPath(fastPath, new FakeHttpHandler("""{"success":true}"""));
    client.Configure("http://192.168.1.2:8123", "secret-device-token-xyz", enableLocalWebSocketFastPath: true, haWebSocketAuthToken: "ha-ws-token");

    _ = client.SendAskDJMessageAsync(TestAskRequest("raw prompt should not be logged"), CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("windows", fastPath.LastPayload!["client_type"]);
    AssertEqual("djconnect-windows-ABC123DEF456", fastPath.LastPayload["device_id"]);
    AssertEqual("secret-device-token-xyz", fastPath.LastPayload["device_token"]);
    AssertEqual("ha-ws-token", fastPath.HaWebSocketAuthToken);
    var diagnostics = client.FastPathDiagnostics;
    AssertTrue(!diagnostics.LastWebSocketError.Contains("secret-device-token", StringComparison.Ordinal), "diagnostics must not include device token");
    AssertTrue(!diagnostics.LastWebSocketError.Contains("raw prompt", StringComparison.OrdinalIgnoreCase), "diagnostics must not include raw prompt");
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

static void LocalizationSupportsRequiredLocales()
{
    AssertSequenceEqual(new[] { "en", "nl", "de", "fr", "es" }, AppStrings.SupportedLanguages);
    foreach (var locale in AppStrings.SupportedLanguages)
    {
        AppStrings.UseLanguage(locale);
        AssertTrue(!string.IsNullOrWhiteSpace(AppStrings.Get("ApiError_InvalidPairCode")), $"{locale} must localize invalid pair code guidance");
        AssertTrue(!string.IsNullOrWhiteSpace(AppStrings.Get("Status_UpdateRequired")), $"{locale} must localize update required");
    }
}

static void SettingsLocalizationAvoidsDiagnosticJargon()
{
    var keys = new[]
    {
        "Xaml_Configuratie_pairing_permissions_en_diagnost",
        "Xaml_Runtime",
        "Xaml_Music_backend",
        "Xaml_Backend_status",
        "Xaml_Backend_revision",
        "Format_SettingsRuntimeSummary",
        "Vm_Music_backend_unavailable",
        "ApiError_UnsupportedBackendCapability"
    };
    var banned = new[] { "backend", "runtime", "fast path" };

    foreach (var locale in AppStrings.SupportedLanguages)
    {
        AppStrings.UseLanguage(locale);
        foreach (var key in keys)
        {
            var value = AppStrings.Get(key);
            foreach (var term in banned)
            {
                AssertTrue(!value.Contains(term, StringComparison.OrdinalIgnoreCase), $"{locale}:{key} should not expose diagnostic term '{term}' in ordinary UI");
            }
        }
    }
}

static void ApiErrorLocalizerMapsUserFacingGuidance()
{
    AppStrings.UseLanguage("en");

    AssertEqual("The pairing code is not correct. Check the code in Home Assistant.", ApiErrorLocalizer.Pairing("invalid_pair_code"));
    AssertEqual("The pairing code is not correct. Check the code in Home Assistant.", ApiErrorLocalizer.Pairing("not_configured"));
    AssertEqual("The wrong app type was selected in Home Assistant. Choose the DJConnect Windows setup flow and use the new pairing code.", ApiErrorLocalizer.Pairing("invalid_client_type"));
    AssertEqual("The app type selected in Home Assistant does not match this app. Choose the DJConnect Windows setup flow in Home Assistant and try again.", ApiErrorLocalizer.Pairing("client_type_mismatch"));
    AssertEqual("Your pairing has expired. Pair DJConnect again to continue.", ApiErrorLocalizer.BackendAction("unauthorized"));
    AssertEqual("This pairing is no longer valid. Generate a new pairing code in Home Assistant and try again.", ApiErrorLocalizer.StaleAuth());
    AssertEqual("This action is from an older music session. Ask DJConnect for a fresh recommendation.", ApiErrorLocalizer.BackendAction("stale_backend_action"));
    AssertEqual("This music service does not support that action.", ApiErrorLocalizer.BackendAction("unsupported_backend_capability"));
}

static void ApiErrorLocalizationPreservesProtocolValues()
{
    foreach (var locale in AppStrings.SupportedLanguages)
    {
        AppStrings.UseLanguage(locale);
        var payload = DJConnectApiClient.BuildStatusPayload(TestIdentity());
        var serialized = JsonSerializer.Serialize(payload, JsonOptions());

        AssertTrue(serialized.Contains("\"client_type\":\"windows\""), $"{locale} must not localize client_type");
        AssertTrue(serialized.Contains("\"device_id\":\"djconnect-windows-ABC123DEF456\""), $"{locale} must not localize device_id");
        AssertTrue(serialized.Contains("\"protocol_version\":\"3.2\""), $"{locale} must not localize protocol version");
    }
}

static void Protocol32AdvertisesNoClientCallbackEndpoint()
{
    AssertEqual("3.2", DJConnectContract.ProtocolLine);
    AssertTrue(!DJConnectApiClient.BuildCommandPayload(ClientIdentity.CreateOrLoad("abc123def4567890"), "status").ContainsKey("local_url"), "Windows command payload must not expose a client-hosted local URL");
}

static void WindowsClientCodeAvoidsRemovedDjconnectHaPlaybackEntities()
{
    var banned = new[]
    {
        "djconnect_volume",
        "djconnect_shuffle",
        "djconnect_repeat_state",
        "djconnect_sound_output",
        "djconnect_spotify_status",
        "djconnect_playback_available",
        "djconnect_queue",
        "djconnect_playlists",
        "djconnect_outputs",
        "sensor.djconnect_",
        "number.djconnect_volume",
        "select.djconnect_sound_output",
        "switch.djconnect_shuffle"
    };
    var sourceRoot = Path.Combine(ProjectRoot(), "src", "DJConnect.Windows");
    var source = string.Join('\n', Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
        .Select(File.ReadAllText));

    foreach (var entity in banned)
    {
        AssertTrue(!source.Contains(entity, StringComparison.OrdinalIgnoreCase), $"Windows client code must not reference removed HA playback entity '{entity}'");
    }
}

static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);

static void ReplaceOutputList(List<PlaybackOutput> outputDevices, IReadOnlyList<PlaybackOutput> outputs)
{
    outputDevices.Clear();
    outputDevices.AddRange(outputs.Where(output => !string.IsNullOrWhiteSpace(output.DisplayName)));
}

static string NormalizeJson(string json)
{
    using var document = JsonDocument.Parse(json);
    return JsonSerializer.Serialize(document.RootElement, JsonOptions());
}

static ClientIdentity TestIdentity() => ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");

static AskDJRequest TestAskRequest(string text) => new(
    "msg-ws-1",
    "djconnect-windows-ABC123DEF456",
    "djconnect-windows-ABC123DEF456",
    "Studio PC",
    "windows",
    text,
    Mood: 72);

static TrackInsightRequest TestTrackInsightRequest() => new(
    "djconnect-windows-ABC123DEF456",
    "Studio PC",
    "windows",
    new TrackInsightRequestTrack("Strobe", "deadmau5", "For Lack of a Better Name"),
    MusicBackend: "music_assistant",
    Language: "en",
    Locale: "en",
    Mood: 72,
    IncludeVisualProfile: true,
    ClientId: "djconnect-windows-ABC123DEF456",
    MusicDnaKey: "dna-studio");

static void ProfileAwareRequestsGenerateCanonicalWindowsContext()
{
    var ask = TestAskRequest("What should I play?");
    ask = ask with
    {
        ProfileId = "profile-peter",
        SessionId = "session-windows-1",
        PrivateSession = true
    };

    using var askDocument = JsonSerializer.SerializeToDocument(ask, JsonOptions());
    var askRoot = askDocument.RootElement;
    AssertEqual("profile-peter", askRoot.GetProperty("profile_id").GetString());
    AssertEqual("session-windows-1", askRoot.GetProperty("session_id").GetString());
    AssertTrue(askRoot.GetProperty("private_session").GetBoolean(), "Ask DJ should carry private_session");
    AssertEqual("ask_dj", askRoot.GetProperty("request_source").GetString());

    var discovery = TestMusicDiscoveryRequest() with
    {
        ProfileId = "profile-peter",
        PrivateSession = true
    };
    var query = typeof(DJConnectApiClient)
        .GetMethod("MusicDiscoveryQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .Invoke(null, [discovery])!
        .ToString();

    AssertTrue(query!.Contains("profile_id=profile-peter", StringComparison.Ordinal), "Discover query should include profile_id");
    AssertTrue(query.Contains("private_session=true", StringComparison.Ordinal), "Discover query should include private_session");
    AssertTrue(query.Contains("request_source=discover", StringComparison.Ordinal), "Discover query should include request_source");
}

static void ProfileResponseMetadataDecodesCanonicalEnvelope()
{
    const string json = """
    {
      "success": true,
      "enabled": true,
      "profile_id": "profile-peter",
      "music_dna_key": "profile:profile-peter",
      "resolved_profile": {
        "id": "profile-peter",
        "name": "Peter",
        "type": "personal",
        "privacy_mode": "normal"
      },
      "resolution": {
        "source": "device_mapping",
        "fallback_used": false
      },
      "profile": {
        "summary": "Backend-owned Music DNA"
      }
    }
    """;

    var response = JsonSerializer.Deserialize<MusicDnaProfileResponse>(json, JsonOptions())!;

    AssertEqual("profile-peter", response.ProfileId);
    AssertEqual("profile:profile-peter", response.MusicDnaKey);
    AssertEqual("Peter", response.ResolvedProfile!.Name);
    AssertEqual("normal", response.ResolvedProfile.PrivacyMode);
    AssertEqual("device_mapping", response.Resolution!.Source);
    AssertTrue(response.Resolution.FallbackUsed == false, "resolution should preserve fallback flag");
}

static void ProfilePlatformErrorsLocalizeAsRepairStates()
{
    AssertTrue(ApiErrorLocalizer.FromApiCode("device_not_mapped")!.Contains("Profile", StringComparison.Ordinal), "device_not_mapped should be profile guidance");
    var privateSession = ApiErrorLocalizer.FromApiCode("private_session_restriction")!;
    AssertTrue(!privateSession.Contains("pair", StringComparison.OrdinalIgnoreCase), "private session restriction should not look like auth or pairing");
    var accountMissing = ApiErrorLocalizer.FromApiCode("profile_music_account_missing")!;
    AssertTrue(!accountMissing.Contains("pair", StringComparison.OrdinalIgnoreCase), "music account error should not look like auth or pairing");
}

static MusicDnaProfileRequest TestMusicDnaProfileRequest() => new(
    "djconnect-windows-ABC123DEF456",
    "djconnect-windows-ABC123DEF456",
    "Studio PC",
    "windows",
    "en",
    "en",
    72,
    "dna-studio");

static MusicDiscoveryRequest TestMusicDiscoveryRequest() => new(
    "djconnect-windows-ABC123DEF456",
    "djconnect-windows-ABC123DEF456",
    "Studio PC",
    "windows",
    "en",
    "en",
    72,
    "dna-studio");

static DJConnectApiClient NewClientWithFastPath(FakeFastPath fastPath, FakeHttpHandler http)
{
    return new DJConnectApiClient(new HttpClient(http), fastPath);
}

static IEnumerable<string> RouteScanFiles(string path)
{
    if (File.Exists(path))
    {
        return [path];
    }

    return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
        .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
}

static bool HasLegacyDjConnectHttpRoute(string line)
{
    var absolutePrefix = "/" + "api/" + "djconnect";
    var relativePrefix = "api/" + "djconnect";
    return ContainsLegacyRoute(line, absolutePrefix)
        || ContainsLegacyRoute(line, relativePrefix);
}

static bool ContainsLegacyRoute(string line, string prefix)
{
    var index = line.IndexOf(prefix, StringComparison.Ordinal);
    while (index >= 0)
    {
        var next = index + prefix.Length;
        if (line.Length == next || line[next] is '/' or '"' or '\'' or '`' or '?' or '&' or '<')
        {
            if (!line.Substring(index).StartsWith(prefix + "/v1", StringComparison.Ordinal))
            {
                return true;
            }
        }

        index = line.IndexOf(prefix, index + prefix.Length, StringComparison.Ordinal);
    }

    return false;
}

static bool ContainsRawAskDJRoute(string line)
{
    var absoluteRawRoute = "/" + "api/djconnect/v1/" + "ask_dj";
    var relativeRawRoute = "api/djconnect/v1/" + "ask_dj";
    return ContainsExactProtocolValue(line, absoluteRawRoute)
        || ContainsExactProtocolValue(line, relativeRawRoute);
}

static bool ContainsExactProtocolValue(string line, string value)
{
    var index = line.IndexOf(value, StringComparison.Ordinal);
    while (index >= 0)
    {
        var next = index + value.Length;
        if (line.Length == next || line[next] is '"' or '\'' or '`' or '<' or ')' or ']' or ',' or ';')
        {
            return true;
        }

        index = line.IndexOf(value, next, StringComparison.Ordinal);
    }

    return false;
}

static string ProjectRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DJConnect.Windows.sln")))
    {
        directory = directory.Parent;
    }

    if (directory is null)
    {
        throw new InvalidOperationException("Could not locate project root.");
    }

    return directory.FullName;
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
{
    if (expected.Count != actual.Count)
    {
        throw new InvalidOperationException($"Expected {expected.Count} items, got {actual.Count}.");
    }

    for (var i = 0; i < expected.Count; i++)
    {
        if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
        {
            throw new InvalidOperationException($"Expected item {i} to be '{expected[i]}', got '{actual[i]}'.");
        }
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

sealed class FakeFastPath : IDJConnectWebSocketFastPath
{
    private readonly HashSet<string> _commands;
    private readonly Dictionary<string, object> _responses = new(StringComparer.OrdinalIgnoreCase);

    public FakeFastPath(IEnumerable<string> commands)
    {
        _commands = new HashSet<string>(commands, StringComparer.OrdinalIgnoreCase);
    }

    public string Error { get; set; } = "";
    public int Attempts { get; private set; }
    public List<string> Routes { get; } = [];
    public Dictionary<string, object?>? LastPayload { get; private set; }
    public string? HaWebSocketAuthToken { get; private set; }
    public bool Enabled { get; private set; }
    public FastPathDiagnostics Diagnostics => new(Enabled ? "websocket" : "http", Enabled, Error, Enabled ? DateTimeOffset.UtcNow : null, Enabled ? _commands.ToArray() : []);

    public FakeFastPath WithResponse<T>(string route, T response)
    {
        _responses[route] = response!;
        return this;
    }

    public void Configure(string homeAssistantUrl, string? token, bool enabled)
    {
        HaWebSocketAuthToken = token;
        Enabled = enabled;
    }

    public Task<FastPathResult<T>> TrySendAsync<T>(
        string route,
        Dictionary<string, object?> payload,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Attempts++;
        Routes.Add(route);
        LastPayload = new Dictionary<string, object?>(payload);

        if (!Enabled)
        {
            return Task.FromResult(FastPathResult<T>.Miss("disabled"));
        }

        if (!string.IsNullOrWhiteSpace(Error))
        {
            return Task.FromResult(FastPathResult<T>.Miss(Error));
        }

        if (!_commands.Contains(route))
        {
            return Task.FromResult(FastPathResult<T>.Miss("missing capability"));
        }

        if (_responses.TryGetValue(route, out var response) && response is T typed)
        {
            return Task.FromResult(FastPathResult<T>.Hit(typed));
        }

        return Task.FromResult(FastPathResult<T>.Miss("no response"));
    }
}

sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _json;

    public FakeHttpHandler(string json)
    {
        _json = json;
    }

    public int RequestCount { get; private set; }
    public string LastPath { get; private set; } = "";
    public string LastMethod { get; private set; } = "";
    public string LastAuthorization { get; private set; } = "";
    public string LastDeviceIdHeader { get; private set; } = "";
    public string LastClientTypeHeader { get; private set; } = "";
    public string LastLanguageHeader { get; private set; } = "";
    public string LastLocaleHeader { get; private set; } = "";
    public string LastAcceptLanguageHeader { get; private set; } = "";
    public string LastMoodHeader { get; private set; } = "";
    public string LastContentType { get; private set; } = "";
    public string LastBody { get; private set; } = "";
    public List<string> RequestPaths { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastPath = request.RequestUri?.PathAndQuery ?? "";
        RequestPaths.Add(LastPath);
        LastMethod = request.Method.Method;
        LastAuthorization = request.Headers.Authorization?.ToString() ?? "";
        LastDeviceIdHeader = request.Headers.TryGetValues("X-DJConnect-Device-ID", out var values)
            ? values.FirstOrDefault() ?? ""
            : "";
        LastClientTypeHeader = request.Headers.TryGetValues("X-DJConnect-Client-Type", out var clientTypeValues)
            ? clientTypeValues.FirstOrDefault() ?? ""
            : "";
        LastLanguageHeader = request.Headers.TryGetValues("X-DJConnect-Language", out var languageValues)
            ? languageValues.FirstOrDefault() ?? ""
            : "";
        LastLocaleHeader = request.Headers.TryGetValues("X-DJConnect-Locale", out var localeValues)
            ? localeValues.FirstOrDefault() ?? ""
            : "";
        LastAcceptLanguageHeader = request.Headers.TryGetValues("Accept-Language", out var acceptLanguageValues)
            ? acceptLanguageValues.FirstOrDefault() ?? ""
            : "";
        LastMoodHeader = request.Headers.TryGetValues("X-DJConnect-Mood", out var moodValues)
            ? moodValues.FirstOrDefault() ?? ""
            : "";
        LastContentType = request.Content?.Headers.ContentType?.MediaType ?? "";
        LastBody = request.Content is null
            ? ""
            : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_json)
        });
    }
}
