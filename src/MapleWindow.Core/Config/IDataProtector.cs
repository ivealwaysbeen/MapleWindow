namespace MapleWindow.Core.Config;

/// <summary>Encrypts small secrets (the Nexon API key) at rest.</summary>
public interface IDataProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}
