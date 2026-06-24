using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Models;

public sealed class AppSettings
{
    public string HomeAssistantUrl { get; set; } = DJConnectContract.DefaultHomeAssistantUrl;
    public string InstallId { get; set; } = "";
    public string DeviceName { get; set; } = Environment.MachineName;
    public long HistoryRevision { get; set; }
    public long ClearRevision { get; set; }
}
