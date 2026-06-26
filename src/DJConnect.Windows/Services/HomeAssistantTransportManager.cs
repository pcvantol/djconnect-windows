using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Services;

public enum HomeAssistantConnectionMode
{
    Offline,
    Local,
    Remote
}

public sealed record HomeAssistantTransportState(
    string LocalUrl,
    string RemoteUrl,
    bool RemoteSupported,
    HomeAssistantConnectionMode Mode,
    string? ActiveUrl)
{
    public bool IsOnline => Mode is HomeAssistantConnectionMode.Local or HomeAssistantConnectionMode.Remote;
    public string ModeLabel => Mode switch
    {
        HomeAssistantConnectionMode.Local => "Local",
        HomeAssistantConnectionMode.Remote => "Remote",
        _ => "Offline"
    };
}

public sealed class HomeAssistantTransportManager
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    private readonly Func<string, CancellationToken, Task<bool>> _probeAsync;

    public HomeAssistantTransportManager(Func<string, CancellationToken, Task<bool>>? probeAsync = null)
    {
        _probeAsync = probeAsync ?? DefaultProbeAsync;
    }

    public HomeAssistantTransportState Current { get; private set; } = new(
        DJConnectContract.DefaultHomeAssistantUrl,
        "",
        false,
        HomeAssistantConnectionMode.Offline,
        null);

    public void UpdateUrls(string? localUrl, string? remoteUrl, bool? remoteSupported)
    {
        Current = Current with
        {
            LocalUrl = NormalizeUrl(localUrl) ?? Current.LocalUrl,
            RemoteUrl = NormalizeUrl(remoteUrl) ?? Current.RemoteUrl,
            RemoteSupported = remoteSupported ?? Current.RemoteSupported
        };
    }

    public async Task<HomeAssistantTransportState> ResolveRuntimeAsync(CancellationToken cancellationToken)
    {
        var localUrl = NormalizeUrl(Current.LocalUrl);
        if (!string.IsNullOrWhiteSpace(localUrl) && await IsReachableAsync(localUrl, cancellationToken))
        {
            return SetMode(HomeAssistantConnectionMode.Local, localUrl);
        }

        var remoteUrl = NormalizeUrl(Current.RemoteUrl);
        if (Current.RemoteSupported && !string.IsNullOrWhiteSpace(remoteUrl) && await IsReachableAsync(remoteUrl, cancellationToken))
        {
            return SetMode(HomeAssistantConnectionMode.Remote, remoteUrl);
        }

        return SetMode(HomeAssistantConnectionMode.Offline, null);
    }

    public async Task<HomeAssistantTransportState> ResolvePairingAsync(string localUrl, CancellationToken cancellationToken)
    {
        var normalizedLocalUrl = NormalizeUrl(localUrl);
        if (string.IsNullOrWhiteSpace(normalizedLocalUrl))
        {
            return SetMode(HomeAssistantConnectionMode.Offline, null);
        }

        UpdateUrls(normalizedLocalUrl, null, null);
        return await IsReachableAsync(normalizedLocalUrl, cancellationToken)
            ? SetMode(HomeAssistantConnectionMode.Local, normalizedLocalUrl)
            : SetMode(HomeAssistantConnectionMode.Offline, null);
    }

    private HomeAssistantTransportState SetMode(HomeAssistantConnectionMode mode, string? activeUrl)
    {
        Current = Current with { Mode = mode, ActiveUrl = activeUrl };
        return Current;
    }

    private async Task<bool> IsReachableAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);
        try
        {
            return await _probeAsync(url, timeout.Token);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> DefaultProbeAsync(string url, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = ProbeTimeout };
        using var request = new HttpRequestMessage(HttpMethod.Get, NormalizeUrl(url));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return (int)response.StatusCode < 500;
    }

    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Trim().TrimEnd('/');
    }
}
