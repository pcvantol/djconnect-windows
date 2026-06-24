using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed class LocalClientApiService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<LocalPairingSnapshot> _snapshotProvider;
    private readonly Func<LocalPairRequest, Task<LocalPairResponse>> _pairHandler;
    private readonly Action<string> _diagnosticLogger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public LocalClientApiService(
        Func<LocalPairingSnapshot> snapshotProvider,
        Func<LocalPairRequest, Task<LocalPairResponse>> pairHandler,
        Action<string> diagnosticLogger)
    {
        _snapshotProvider = snapshotProvider;
        _pairHandler = pairHandler;
        _diagnosticLogger = diagnosticLogger;
    }

    public bool IsRunning => _listener is not null;
    public int Port { get; private set; }
    public string LocalUrl { get; private set; } = "";

    public Task StartAsync()
    {
        if (_listener is not null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        LocalUrl = $"http://{FindLanAddress()}:{Port}";
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        _diagnosticLogger($"INF Local Client API started at {RedactUrl(LocalUrl)}");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;
        Port = 0;
        LocalUrl = "";
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _diagnosticLogger($"WRN Local Client API accept failed: {ex.GetType().Name}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var _ = client;
        try
        {
            using var stream = client.GetStream();
            var request = await ReadRequestAsync(stream, cancellationToken);
            var path = request.Path.TrimEnd('/');
            if (path.Length == 0)
            {
                path = "/";
            }

            switch (request.Method, path)
            {
                case ("GET", "/api/device/pairing-info"):
                case ("GET", "/api/device/info"):
                    await WriteJsonAsync(stream, PairingInfo(includePairingCode: path.EndsWith("pairing-info", StringComparison.Ordinal)), 200, cancellationToken);
                    break;
                case ("POST", "/api/device/pair"):
                    await HandlePairAsync(stream, request.Body, cancellationToken);
                    break;
                default:
                    await WriteJsonAsync(stream, new LocalPairResponse(false, "not_found", "Unsupported local DJConnect endpoint."), 404, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _diagnosticLogger($"WRN Local Client API request failed: {ex.GetType().Name}");
        }
    }

    private async Task HandlePairAsync(Stream stream, string body, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<LocalPairRequest>(body, JsonOptions)
                ?? new LocalPairRequest(null, null, null, null, null, null, null, null, null, null, null);
            var response = await _pairHandler(request);
            await WriteJsonAsync(stream, response, response.Success ? 200 : 500, cancellationToken);
        }
        catch (JsonException)
        {
            await WriteJsonAsync(stream, new LocalPairResponse(false, "bad_request", "Invalid JSON body."), 400, cancellationToken);
        }
    }

    private LocalPairingInfo PairingInfo(bool includePairingCode)
    {
        var snapshot = _snapshotProvider();
        return new LocalPairingInfo(
            snapshot.Identity.DeviceId,
            snapshot.Identity.DeviceName,
            snapshot.Identity.ClientType,
            snapshot.Firmware,
            snapshot.AppVersion,
            "windows",
            snapshot.Paired,
            snapshot.LocalUrl,
            includePairingCode && snapshot.Pairable ? snapshot.PairCode : null);
    }

    private static async Task<HttpRequest> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int headerEnd = -1;
            var contentLength = 0;
            while (headerEnd < 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read <= 0)
                {
                    break;
                }

                memory.Write(buffer, 0, read);
                var bytes = memory.ToArray();
                headerEnd = FindHeaderEnd(bytes);
                if (headerEnd >= 0)
                {
                    var headers = Encoding.UTF8.GetString(bytes, 0, headerEnd);
                    contentLength = ParseContentLength(headers);
                    var total = headerEnd + 4 + contentLength;
                    while (memory.Length < total)
                    {
                        read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                        if (read <= 0)
                        {
                            break;
                        }

                        memory.Write(buffer, 0, read);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var requestBytes = memory.ToArray();
        var split = FindHeaderEnd(requestBytes);
        if (split < 0)
        {
            throw new InvalidOperationException("Invalid HTTP request.");
        }

        var headerText = Encoding.UTF8.GetString(requestBytes, 0, split);
        var firstLine = headerText.Split("\r\n", StringSplitOptions.None)[0].Split(' ');
        var bodyStart = split + 4;
        var bodyLength = Math.Max(0, requestBytes.Length - bodyStart);
        var body = Encoding.UTF8.GetString(requestBytes, bodyStart, bodyLength);
        return new HttpRequest(firstLine[0].ToUpperInvariant(), firstLine.Length > 1 ? firstLine[1] : "/", body);
    }

    private static async Task WriteJsonAsync<T>(Stream stream, T value, int statusCode, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(value, JsonOptions);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var status = statusCode == 200 ? "OK" : statusCode == 400 ? "Bad Request" : statusCode == 404 ? "Not Found" : "Internal Server Error";
        var headers = $"HTTP/1.1 {statusCode} {status}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(headers), cancellationToken);
        await stream.WriteAsync(bodyBytes, cancellationToken);
    }

    private static int FindHeaderEnd(byte[] bytes)
    {
        for (var i = 0; i <= bytes.Length - 4; i++)
        {
            if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    private static int ParseContentLength(string headers)
    {
        foreach (var line in headers.Split("\r\n"))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(line["Content-Length:".Length..].Trim(), out var value))
            {
                return value;
            }
        }

        return 0;
    }

    private static string FindLanAddress()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up
                || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString();
                }
            }
        }

        return "127.0.0.1";
    }

    private static string RedactUrl(string localUrl)
    {
        return Uri.TryCreate(localUrl, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://<local-ip>:{uri.Port}"
            : "<local-url>";
    }

    private sealed record HttpRequest(string Method, string Path, string Body);
}
