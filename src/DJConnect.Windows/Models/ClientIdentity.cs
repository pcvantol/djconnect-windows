using System.Security.Cryptography;
using DJConnect.Windows.Contracts;

namespace DJConnect.Windows.Models;

public sealed record ClientIdentity(string InstallId, string DeviceId, string DeviceName, string ClientType)
{
    public static string CreateInstallId()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    public static ClientIdentity CreateOrLoad(string? storedInstallId, string? deviceName = null)
    {
        var installId = string.IsNullOrWhiteSpace(storedInstallId)
            ? CreateInstallId()
            : storedInstallId.Trim();
        var suffix = new string(installId.Where(char.IsLetterOrDigit).Take(12).ToArray()).ToUpperInvariant();
        if (suffix.Length < 12)
        {
            suffix = (suffix + "000000000000")[..12];
        }

        return new ClientIdentity(
            installId,
            $"{DJConnectContract.DeviceIdPrefix}-{suffix}",
            string.IsNullOrWhiteSpace(deviceName) ? Environment.MachineName : deviceName.Trim(),
            DJConnectContract.ClientType);
    }
}
