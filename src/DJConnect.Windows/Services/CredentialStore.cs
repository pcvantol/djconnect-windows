using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace DJConnect.Windows.Services;

public sealed class CredentialStore
{
    private const string Target = "DJConnect.Windows.HomeAssistantToken";

    public string? ReadToken()
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return RunSecurity("find-generic-password", "-s", Target, "-w").TrimToNull();
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        if (!CredRead(Target, 1, 0, out var credentialPtr))
        {
            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(credentialPtr);
            if (credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                return null;
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void SaveToken(string token)
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            RunSecurity("delete-generic-password", "-s", Target);
            RunSecurity("add-generic-password", "-s", Target, "-a", Environment.UserName, "-w", token);
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(token);
        var credential = new Credential
        {
            Type = 1,
            TargetName = Target,
            CredentialBlobSize = bytes.Length,
            CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length),
            Persist = 2,
            UserName = Environment.UserName
        };

        try
        {
            Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);
            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException("Windows Credential Manager rejected the DJConnect token.");
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
        }
    }

    public void DeleteToken()
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            RunSecurity("delete-generic-password", "-s", Target);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            CredDelete(Target, 1, 0);
        }
    }

    private static string RunSecurity(params string[] arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/security",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }.WithArguments(arguments));
            process?.WaitForExit(5000);
            return process?.ExitCode == 0 ? process.StandardOutput.ReadToEnd() : "";
        }
        catch
        {
            return "";
        }
    }

    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref Credential userCredential, int flags);

    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("advapi32", SetLastError = true)]
    private static extern void CredFree(IntPtr credential);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}

file static class CredentialStoreExtensions
{
    public static string? TrimToNull(this string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
