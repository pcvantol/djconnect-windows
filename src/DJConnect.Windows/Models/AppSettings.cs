using System.Text.Json.Serialization;
using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Models;

public sealed class AppSettings
{
    public string HomeAssistantUrl { get; set; } = DJConnectContract.DefaultHomeAssistantUrl;
    public string InstallId { get; set; } = "";
    public string DeviceName { get; set; } = Environment.MachineName;
    public string Language { get; set; } = "nl";
    public string LogLevel { get; set; } = "info";
    public bool IsDemoMode { get; set; }
    public bool DJConnectWelcomeSeen { get; set; }
    public bool HasCompletedOnboarding { get; set; }
    public string LastSeenAppVersion { get; set; } = "";
    public bool HasCleanShutdownState { get; set; }
    public bool CleanShutdown { get; set; } = true;
    public bool CrashPromptPending { get; set; }
    public bool WakewordEnabled { get; set; }
    public bool WakewordPromptDismissed { get; set; }
    public string WakePhrase { get; set; } = "Hey DJ";
    [JsonPropertyName("DJConnectPermissionExplanation.microphone.seen")]
    public bool PermissionExplanationMicrophoneSeen { get; set; }

    [JsonPropertyName("DJConnectPermissionExplanation.notifications.seen")]
    public bool PermissionExplanationNotificationsSeen { get; set; }

    [JsonPropertyName("DJConnectPermissionExplanation.localNetwork.seen")]
    public bool PermissionExplanationLocalNetworkSeen { get; set; }
    public string PairingCode { get; set; } = "";
    public long HistoryRevision { get; set; }
    public long ClearRevision { get; set; }
    public List<string> DiagnosticLogLines { get; set; } = [];
}
