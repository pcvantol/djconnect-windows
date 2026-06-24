using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed class MdnsAdvertiser : IDisposable
{
    private static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.0.251");
    private readonly Action<string> _diagnosticLogger;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private LocalPairingSnapshot? _snapshot;

    public MdnsAdvertiser(Action<string> diagnosticLogger)
    {
        _diagnosticLogger = diagnosticLogger;
    }

    public bool IsAdvertising => _udpClient is not null;

    public static IReadOnlyDictionary<string, string> BuildTxtRecord(LocalPairingSnapshot snapshot)
    {
        var txt = new Dictionary<string, string>
        {
            ["client_type"] = snapshot.Identity.ClientType,
            ["platform"] = "windows",
            ["device_id"] = snapshot.Identity.DeviceId,
            ["device_name"] = snapshot.Identity.DeviceName,
            ["firmware"] = snapshot.Firmware,
            ["version"] = snapshot.Firmware,
            ["app_version"] = snapshot.AppVersion,
            ["pairing_status"] = snapshot.PairingStatus,
            ["paired"] = snapshot.Paired ? "true" : "false",
            ["local_url"] = snapshot.LocalUrl,
            ["api"] = "device",
            ["path"] = "/api/device/info",
            ["pairing_path"] = "/api/device/pairing-info",
            ["pair_path"] = "/api/device/pair",
            ["model"] = "windows-app"
        };

        if (snapshot.Pairable)
        {
            txt["pair_code"] = snapshot.PairCode;
            txt["pairing_code"] = snapshot.PairCode;
            txt["pairing_token"] = snapshot.PairCode;
        }

        return txt;
    }

    public async Task StartOrUpdateAsync(LocalPairingSnapshot snapshot)
    {
        _snapshot = snapshot;
        if (_udpClient is null)
        {
            try
            {
                _cts = new CancellationTokenSource();
                _udpClient = new UdpClient(AddressFamily.InterNetwork);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
                _udpClient.JoinMulticastGroup(MulticastAddress);
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                _diagnosticLogger($"INF mDNS advertising enabled service=_djconnect._tcp client_type=windows port={GetPort(snapshot.LocalUrl)} pairing_status={snapshot.PairingStatus}");
            }
            catch (Exception ex)
            {
                _udpClient?.Dispose();
                _udpClient = null;
                _cts?.Cancel();
                _diagnosticLogger($"WRN mDNS advertising unavailable: {ex.GetType().Name}");
                return;
            }
        }

        await AnnounceAsync();
    }

    public async Task StopAsync()
    {
        if (_udpClient is null)
        {
            return;
        }

        try
        {
            await AnnounceAsync(ttl: 0);
        }
        catch
        {
        }

        _cts?.Cancel();
        _udpClient?.DropMulticastGroup(MulticastAddress);
        _udpClient?.Dispose();
        _udpClient = null;
        _snapshot = null;
        _diagnosticLogger("INF mDNS advertising disabled service=_djconnect._tcp");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _udpClient?.Dispose();
        _cts?.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpClient is not null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                if (IsDjConnectQuery(result.Buffer))
                {
                    await AnnounceAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
            }
        }
    }

    private async Task AnnounceAsync(uint ttl = 120)
    {
        if (_udpClient is null || _snapshot is null)
        {
            return;
        }

        var packet = BuildResponsePacket(_snapshot, ttl);
        await _udpClient.SendAsync(packet, packet.Length, new IPEndPoint(MulticastAddress, 5353));
    }

    private static bool IsDjConnectQuery(byte[] packet)
    {
        var needle = Encoding.ASCII.GetBytes("_djconnect");
        return packet.AsSpan().IndexOf(needle) >= 0;
    }

    private static byte[] BuildResponsePacket(LocalPairingSnapshot snapshot, uint ttl)
    {
        using var stream = new MemoryStream();
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0x8400);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 4);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);

        var serviceType = "_djconnect._tcp.local";
        var instance = $"{SanitizeServiceLabel(snapshot.Identity.DeviceId)}.{serviceType}";
        var host = $"{SanitizeServiceLabel(snapshot.Identity.DeviceId)}.local";
        var localIp = GetAddress(snapshot.LocalUrl);

        WriteName(stream, serviceType);
        WriteUInt16(stream, 12);
        WriteUInt16(stream, 1);
        WriteUInt32(stream, ttl);
        WriteRData(stream, data => WriteName(data, instance));

        WriteName(stream, instance);
        WriteUInt16(stream, 33);
        WriteUInt16(stream, 1);
        WriteUInt32(stream, ttl);
        WriteRData(stream, data =>
        {
            WriteUInt16(data, 0);
            WriteUInt16(data, 0);
            WriteUInt16(data, (ushort)GetPort(snapshot.LocalUrl));
            WriteName(data, host);
        });

        WriteName(stream, instance);
        WriteUInt16(stream, 16);
        WriteUInt16(stream, 1);
        WriteUInt32(stream, ttl);
        WriteRData(stream, data =>
        {
            foreach (var item in BuildTxtRecord(snapshot))
            {
                var bytes = Encoding.UTF8.GetBytes($"{item.Key}={item.Value}");
                if (bytes.Length > 255)
                {
                    continue;
                }

                data.WriteByte((byte)bytes.Length);
                data.Write(bytes);
            }
        });

        WriteName(stream, host);
        WriteUInt16(stream, 1);
        WriteUInt16(stream, 1);
        WriteUInt32(stream, ttl);
        WriteRData(stream, data => data.Write(localIp.GetAddressBytes()));

        return stream.ToArray();
    }

    private static void WriteRData(Stream stream, Action<Stream> write)
    {
        using var data = new MemoryStream();
        write(data);
        WriteUInt16(stream, (ushort)data.Length);
        data.Position = 0;
        data.CopyTo(stream);
    }

    private static void WriteName(Stream stream, string name)
    {
        foreach (var label in name.TrimEnd('.').Split('.'))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            stream.WriteByte((byte)bytes.Length);
            stream.Write(bytes);
        }

        stream.WriteByte(0);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static string SanitizeServiceLabel(string value)
    {
        var chars = value.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        var sanitized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "djconnect-windows" : sanitized;
    }

    private static IPAddress GetAddress(string localUrl)
    {
        return Uri.TryCreate(localUrl, UriKind.Absolute, out var uri)
            && IPAddress.TryParse(uri.Host, out var address)
            ? address
            : IPAddress.Loopback;
    }

    private static int GetPort(string localUrl)
    {
        return Uri.TryCreate(localUrl, UriKind.Absolute, out var uri) ? uri.Port : 0;
    }
}
