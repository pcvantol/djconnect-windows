using System.Text.Json;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Services;

var tests = new (string Name, Action Run)[]
{
    ("Client identity uses windows contract", ClientIdentityUsesWindowsContract),
    ("Client identity pads short install IDs", ClientIdentityPadsShortInstallIds),
    ("Pairing payload serializes HA compatibility fields", PairingPayloadSerializesCompatibilityFields),
    ("Ask DJ request serializes server-side message contract", AskDJRequestSerializesServerSideContract),
    ("Ask DJ response deserializes exchange messages", AskDJResponseDeserializesExchangeMessages),
    ("Ask DJ message presentation supports system audio and confirmations", AskDJMessagePresentationSupportsSystemAudioAndConfirmations),
    ("Ask DJ history deserializes revisions trim metadata and recent items", AskDJHistoryDeserializesRevisionsTrimMetadataAndRecentItems),
    ("Playback action deserializes confirmation command", PlaybackActionDeserializesConfirmationCommand),
    ("Playback command payload stays generic", PlaybackCommandPayloadStaysGeneric),
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
    ("Diagnostic log preferences are bounded by default", DiagnosticLogPreferencesAreBoundedByDefault),
    ("Permission explanation flags default to unseen", PermissionExplanationFlagsDefaultToUnseen),
    ("mDNS TXT includes pair code only while pairable", MdnsTxtIncludesPairCodeOnlyWhilePairable),
    ("mDNS lifecycle snapshots only advertise when pairable", MdnsLifecycleSnapshotsOnlyAdvertiseWhenPairable)
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

static void AskDJRequestSerializesServerSideContract()
{
    var request = new AskDJRequest(
        "msg-1",
        "djconnect-windows-ABC123DEF456",
        "djconnect-windows-ABC123DEF456",
        "Studio PC",
        "windows",
        "Welke nummers hoorde ik net?",
        "Welke nummers hoorde ik net?");

    using var document = JsonSerializer.SerializeToDocument(request);
    var root = document.RootElement;

    AssertEqual("msg-1", root.GetProperty("client_message_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("client_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("device_id").GetString());
    AssertEqual("windows", root.GetProperty("client_type").GetString());
    AssertEqual("Welke nummers hoorde ik net?", root.GetProperty("text").GetString());
    AssertEqual("Welke nummers hoorde ik net?", root.GetProperty("message").GetString());
    AssertEqual("auto", root.GetProperty("audio_response").GetString());
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
    var system = new AskDJMessage("sys-1", "assistant", "History trimmed", null, DateTimeOffset.Now, "system", null, null, null, Origin: "history_retention");
    var audio = new AskDJMessage("a-1", "assistant", "Luister nog eens.", null, DateTimeOffset.Now, "assistant", null, null, null, "http://127.0.0.1/audio.wav");
    var yes = new PlaybackAction("yes", "confirmation", "ask_dj_followup_response", null, null, null, null, null, null, null, "yes", "confirmation", "yes");
    var no = yes with { Id = "no", Value = "no", ResponseValue = "no" };

    AssertTrue(system.IsSystem, "system messages must render as system");
    AssertTrue(!system.IsUser, "system messages must not render as user bubbles");
    AssertTrue(audio.HasAudio, "assistant messages with audio_url must show replay UI");
    AssertEqual("Ja", yes.DisplayLabel);
    AssertEqual("Nee", no.DisplayLabel);
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

static void PlaybackCommandPayloadStaysGeneric()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var payload = DJConnectApiClient.BuildCommandPayload(identity, "volume", new { volume_percent = 42 });
    var json = JsonSerializer.Serialize(payload, JsonOptions());

    AssertTrue(json.Contains("\"command\":\"volume\""), "payload must include the generic command");
    AssertTrue(json.Contains("\"client_type\":\"windows\""), "payload must include the Windows client type");
    AssertTrue(!json.Contains("spotify_source", StringComparison.OrdinalIgnoreCase), "payload must not include Spotify source overrides");
    AssertTrue(!json.Contains("liked_proxy_playlist_uri", StringComparison.OrdinalIgnoreCase), "payload must not include removed playlist override fields");
    AssertTrue(!json.Contains("refresh_token", StringComparison.OrdinalIgnoreCase), "payload must not include Spotify credentials");
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
    var sameMinor = VersionCompatibility.Evaluate("3.1", "3.1.9", null, false, null);
    var olderMinor = VersionCompatibility.Evaluate("3.1", "3.0.12", null, false, null);
    var newerMinor = VersionCompatibility.Evaluate("3.1", null, "3.2", false, null);
    var explicitMismatch = VersionCompatibility.Evaluate("3.1", "3.1.1", null, true, "version_mismatch");
    var devEscapeHatch = VersionCompatibility.Evaluate("3.1", "0.0.0", null, true, "version_mismatch");

    AssertTrue(sameMinor.IsCompatible, "patch versions may differ within the same app protocol minor");
    AssertTrue(!olderMinor.IsCompatible, "older HA minor must require an update");
    AssertTrue(!newerMinor.IsCompatible, "newer HA minor must require an update");
    AssertTrue(!explicitMismatch.IsCompatible, "explicit version_mismatch must require an update");
    AssertTrue(devEscapeHatch.IsCompatible, "0.0.0 is kept as a dev escape hatch");
    AssertEqual("3.1", olderMinor.RequiredMajorMinor);
    AssertEqual("3.0", olderMinor.HomeAssistantMajorMinor);
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

static void MdnsTxtIncludesPairCodeOnlyWhilePairable()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var pairable = new LocalPairingSnapshot(identity, "123456", "pairing", "http://192.168.1.10:56789", true, "3.1", "3.1.1");
    var paired = pairable with { PairingStatus = "paired", Pairable = false };

    var pairableTxt = MdnsAdvertiser.BuildTxtRecord(pairable);
    var pairedTxt = MdnsAdvertiser.BuildTxtRecord(paired);

    AssertEqual("windows", pairableTxt["client_type"]);
    AssertEqual("windows", pairableTxt["platform"]);
    AssertEqual(identity.DeviceId, pairableTxt["device_id"]);
    AssertEqual("false", pairableTxt["paired"]);
    AssertEqual("123456", pairableTxt["pair_code"]);
    AssertEqual("http://192.168.1.10:56789", pairableTxt["local_url"]);
    AssertTrue(!pairableTxt.ContainsKey("token"), "mDNS TXT must not expose tokens");
    AssertTrue(!pairableTxt.ContainsKey("ssid"), "mDNS TXT must not expose Wi-Fi SSID");
    AssertTrue(!pairedTxt.ContainsKey("pair_code"), "paired mDNS TXT must not expose pair codes");
    AssertEqual("true", pairedTxt["paired"]);
}

static void MdnsLifecycleSnapshotsOnlyAdvertiseWhenPairable()
{
    var identity = ClientIdentity.CreateOrLoad("abc123def4567890", "Studio PC");
    var onboarding = new LocalPairingSnapshot(identity, "123456", "pairing", "http://192.168.1.10:56789", false, "3.1", "3.1.1");
    var pairing = onboarding with { Pairable = true };
    var paired = pairing with { PairingStatus = "paired", Pairable = false };
    var demo = pairing with { PairingStatus = "demo", Pairable = false };

    AssertTrue(!onboarding.Pairable, "onboarding must not advertise mDNS");
    AssertTrue(pairing.Pairable, "pairing screen may advertise mDNS");
    AssertTrue(!paired.Pairable, "paired state must not advertise mDNS");
    AssertTrue(!demo.Pairable, "demo mode must not advertise mDNS");
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
