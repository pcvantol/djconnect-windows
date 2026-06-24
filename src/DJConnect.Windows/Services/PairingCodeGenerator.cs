using System.Security.Cryptography;

namespace DJConnect.Windows.Services;

public static class PairingCodeGenerator
{
    public static string CreateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }
}
