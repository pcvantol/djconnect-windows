using System.Text.Json;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;

var tests = new (string Name, Action Run)[]
{
    ("Client identity uses windows contract", ClientIdentityUsesWindowsContract),
    ("Client identity pads short install IDs", ClientIdentityPadsShortInstallIds),
    ("Pairing payload serializes HA compatibility fields", PairingPayloadSerializesCompatibilityFields),
    ("Ask DJ request serializes server-side message contract", AskDJRequestSerializesServerSideContract),
    ("Ask DJ history deserializes revisions trim metadata and recent items", AskDJHistoryDeserializesRevisionsTrimMetadataAndRecentItems),
    ("Playback action deserializes confirmation command", PlaybackActionDeserializesConfirmationCommand)
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
        "Welke nummers hoorde ik net?");

    using var document = JsonSerializer.SerializeToDocument(request);
    var root = document.RootElement;

    AssertEqual("msg-1", root.GetProperty("client_message_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("client_id").GetString());
    AssertEqual("djconnect-windows-ABC123DEF456", root.GetProperty("device_id").GetString());
    AssertEqual("windows", root.GetProperty("client_type").GetString());
    AssertEqual("Welke nummers hoorde ik net?", root.GetProperty("text").GetString());
    AssertEqual("auto", root.GetProperty("audio_response").GetString());
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
