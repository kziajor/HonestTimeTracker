using HonestTimeTracker.Application.Tfs;
using System.Security.Cryptography;
using System.Text;

namespace HonestTimeTracker.Infrastructure.Security;

/// Encrypts secrets with Windows DPAPI, bound to the current Windows user account.
public class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("HonestTimeTracker.Secrets.v1");

    public string Protect(string plainText)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Secret protection requires Windows (DPAPI).");

        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    public string Unprotect(string protectedText)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Secret protection requires Windows (DPAPI).");

        var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedText), Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
