namespace HomeManager.Host.Infrastructure.Security;

using System.Security.Cryptography;

// Per-request nonce shared by the CSP header and the inline scripts rendered by the page
public sealed class CspNonce
{
    public string Value { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
}
