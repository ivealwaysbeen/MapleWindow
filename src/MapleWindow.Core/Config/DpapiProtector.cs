using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace MapleWindow.Core.Config;

/// <summary>Windows DPAPI (CurrentUser scope) — the API key is only decryptable by the same Windows user account.</summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiProtector : IDataProtector
{
    public string Protect(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string Unprotect(string protectedText)
    {
        var bytes = Convert.FromBase64String(protectedText);
        var decrypted = ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}
