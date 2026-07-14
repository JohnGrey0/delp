using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

/// <summary>Shared hash-algorithm name handling for the hashing tool family.</summary>
internal static class HashAlgorithms
{
    /// <summary>The five digest algorithms offered by hash-generator and file-checksum.</summary>
    public static readonly IReadOnlyList<string> All = new[] { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" };

    /// <summary>Normalizes user-facing names like "SHA-256" or "sha256" to the canonical "SHA256" form.</summary>
    /// <exception cref="ArgumentException">The algorithm name is not recognized.</exception>
    public static string Normalize(string algorithm)
    {
        var key = (algorithm ?? "").Replace("-", "").Trim().ToUpperInvariant();
        return key switch
        {
            "MD5" => "MD5",
            "SHA1" => "SHA1",
            "SHA256" => "SHA256",
            "SHA384" => "SHA384",
            "SHA512" => "SHA512",
            _ => throw new ArgumentException($"Unknown hash algorithm '{algorithm}'.", nameof(algorithm)),
        };
    }

    public static HashAlgorithmName ToHashAlgorithmName(string algorithm) => Normalize(algorithm) switch
    {
        "MD5" => HashAlgorithmName.MD5,
        "SHA1" => HashAlgorithmName.SHA1,
        "SHA256" => HashAlgorithmName.SHA256,
        "SHA384" => HashAlgorithmName.SHA384,
        "SHA512" => HashAlgorithmName.SHA512,
        _ => throw new ArgumentException($"Unknown hash algorithm '{algorithm}'.", nameof(algorithm)),
    };

    /// <summary>Creates a disposable incremental hasher for the given algorithm (used for stream hashing).</summary>
    public static System.Security.Cryptography.HashAlgorithm Create(string algorithm) => Normalize(algorithm) switch
    {
        "MD5" => MD5.Create(),
        "SHA1" => SHA1.Create(),
        "SHA256" => SHA256.Create(),
        "SHA384" => SHA384.Create(),
        "SHA512" => SHA512.Create(),
        _ => throw new ArgumentException($"Unknown hash algorithm '{algorithm}'.", nameof(algorithm)),
    };

    public static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
