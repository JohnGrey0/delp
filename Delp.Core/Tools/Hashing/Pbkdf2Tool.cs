using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

public static class Pbkdf2Tool
{
    /// <summary>Iteration counts below this are flagged as insecure by the UI (OWASP floor).</summary>
    public const int OwaspMinimumIterations = 1000;

    /// <summary>Hash algorithms PBKDF2 supports in .NET: SHA-1, SHA-256, SHA-384, SHA-512.</summary>
    public static readonly IReadOnlyList<string> Algorithms = ["SHA1", "SHA256", "SHA384", "SHA512"];

    /// <summary>Derives a key from a password using PBKDF2 (RFC 2898 / PKCS#5 v2).</summary>
    /// <exception cref="ArgumentException">The algorithm is unsupported, or iterations/length are out of range.</exception>
    public static byte[] Derive(string password, byte[] salt, int iterations, string hashAlgo, int lengthBytes)
    {
        if (iterations < 1)
            throw new ArgumentException("Iterations must be at least 1.", nameof(iterations));
        if (lengthBytes < 1)
            throw new ArgumentException("Output length must be at least 1 byte.", nameof(lengthBytes));

        var normalized = HashAlgorithms.Normalize(hashAlgo);
        if (!Algorithms.Contains(normalized))
            throw new ArgumentException($"PBKDF2 does not support hash algorithm '{hashAlgo}'.", nameof(hashAlgo));

        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password ?? "");
        return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt ?? [], iterations, HashAlgorithms.ToHashAlgorithmName(normalized), lengthBytes);
    }

    /// <summary>Generates a cryptographically random salt of the given length.</summary>
    public static byte[] GenerateSalt(int bytes)
    {
        if (bytes < 1)
            throw new ArgumentException("Salt length must be at least 1 byte.", nameof(bytes));
        return RandomNumberGenerator.GetBytes(bytes);
    }

    /// <summary>
    /// Formats a PHC-style string: <c>$pbkdf2-&lt;algo&gt;$i=&lt;iterations&gt;$&lt;b64 salt&gt;$&lt;b64 hash&gt;</c>.
    /// </summary>
    public static string FormatPhc(string hashAlgo, int iterations, byte[] salt, byte[] hash)
    {
        var tag = HashAlgorithms.Normalize(hashAlgo).ToLowerInvariant();
        return $"$pbkdf2-{tag}$i={iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
