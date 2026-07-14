using System.Security.Cryptography;

namespace Delp.Core.Tools.Hashing;

/// <summary>A single named digest, e.g. ("SHA256", "ba7816bf...").</summary>
public sealed record HashResult(string Algorithm, string Hex);

public static class HashTool
{
    /// <summary>MD5, SHA-1, SHA-256, SHA-384, SHA-512 in that order.</summary>
    public static readonly IReadOnlyList<string> Algorithms = HashAlgorithms.All;

    /// <summary>Computes MD5, SHA-1, SHA-256, SHA-384, and SHA-512 digests of the given bytes.</summary>
    public static IReadOnlyList<HashResult> ComputeAll(byte[] data)
    {
        var results = new List<HashResult>(Algorithms.Count);
        foreach (var algo in Algorithms)
            results.Add(new HashResult(algo, HashAlgorithms.ToHex(ComputeBytes(algo, data))));
        return results;
    }

    /// <summary>Computes a single digest of the given bytes, returned as raw bytes.</summary>
    /// <exception cref="ArgumentException">The algorithm name is not recognized.</exception>
    public static byte[] ComputeBytes(string algorithm, byte[] data) => HashAlgorithms.Normalize(algorithm) switch
    {
        "MD5" => MD5.HashData(data),
        "SHA1" => SHA1.HashData(data),
        "SHA256" => SHA256.HashData(data),
        "SHA384" => SHA384.HashData(data),
        "SHA512" => SHA512.HashData(data),
        _ => throw new ArgumentException($"Unknown hash algorithm '{algorithm}'.", nameof(algorithm)),
    };

    /// <summary>
    /// Computes a single digest incrementally over a stream (e.g. a file), so the whole
    /// input never needs to be resident in memory. Returns lowercase hex.
    /// </summary>
    /// <exception cref="ArgumentException">The algorithm name is not recognized.</exception>
    public static string Compute(string algorithm, Stream stream)
    {
        using var hasher = HashAlgorithms.Create(algorithm);
        return HashAlgorithms.ToHex(hasher.ComputeHash(stream));
    }

    /// <summary>
    /// Computes all five digests in a single incremental pass over a stream, so a large
    /// file is only read once regardless of how many algorithms are requested.
    /// </summary>
    public static IReadOnlyList<HashResult> ComputeAllFromStream(Stream stream)
    {
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var sha384 = IncrementalHash.CreateHash(HashAlgorithmName.SHA384);
        using var sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        var incrementals = new[] { md5, sha1, sha256, sha384, sha512 };

        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            foreach (var hasher in incrementals)
                hasher.AppendData(buffer, 0, read);
        }

        var results = new List<HashResult>(Algorithms.Count);
        for (var i = 0; i < Algorithms.Count; i++)
            results.Add(new HashResult(Algorithms[i], HashAlgorithms.ToHex(incrementals[i].GetHashAndReset())));
        return results;
    }
}
