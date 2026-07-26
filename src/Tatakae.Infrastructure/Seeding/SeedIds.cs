using System.Security.Cryptography;
using System.Text;

namespace Tatakae.Infrastructure.Seeding;

internal static class SeedIds
{
    public static Guid From(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"tatakae-phase14:{key.Trim().ToLowerInvariant()}"));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);

        // Mark the value as an RFC 4122-compatible, name-based UUID while keeping it deterministic.
        bytes[7] = (byte)((bytes[7] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }
}
