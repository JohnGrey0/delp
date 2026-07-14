using System.Globalization;
using System.Text.Json;

namespace Delp.Core.Tools.Encoding;

/// <summary>One decoded JWT claim, with a human-readable note for registered time claims.</summary>
public sealed record JwtClaim(string Name, string Value, string? Note);

/// <summary>The three (or two, for unsecured tokens) decoded segments of a JWT.</summary>
public sealed record JwtParts(string HeaderJson, string PayloadJson, string SignatureB64, IReadOnlyList<JwtClaim> Claims);

/// <summary>Decodes JSON Web Tokens. This is a decoder only — signatures are never verified.</summary>
public static class JwtTool
{
    private static readonly string[] TimeClaims = { "exp", "iat", "nbf" };

    /// <exception cref="FormatException">The token does not have 2 or 3 dot-separated parts, or a segment is not valid base64url.</exception>
    public static JwtParts Decode(string token)
    {
        var trimmed = (token ?? "").Trim();
        const string bearerPrefix = "Bearer ";
        if (trimmed.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[bearerPrefix.Length..].Trim();

        var parts = trimmed.Split('.');
        if (parts.Length is not (2 or 3))
            throw new FormatException($"Invalid JWT: expected 2 or 3 '.'-separated parts, found {parts.Length}.");

        var headerRaw = DecodeBase64Url(parts[0], "header");
        var payloadRaw = DecodeBase64Url(parts[1], "payload");
        var signature = parts.Length == 3 ? parts[2] : "";

        var headerJson = PrettyPrintOrRaw(headerRaw);
        var payloadJson = PrettyPrintOrRaw(payloadRaw);
        var claims = ExtractClaims(payloadRaw);

        return new JwtParts(headerJson, payloadJson, signature, claims);
    }

    private static string DecodeBase64Url(string segment, string partName)
    {
        try
        {
            var s = segment.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(s);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new FormatException($"Invalid base64url in JWT {partName}.");
        }
    }

    private static string PrettyPrintOrRaw(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return raw;
        }
    }

    private static IReadOnlyList<JwtClaim> ExtractClaims(string payloadRaw)
    {
        var list = new List<JwtClaim>();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadRaw);
        }
        catch (JsonException)
        {
            return list;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return list;

            var now = DateTimeOffset.UtcNow;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                string? note = null;
                if (Array.IndexOf(TimeClaims, prop.Name) >= 0
                    && prop.Value.ValueKind == JsonValueKind.Number
                    && prop.Value.TryGetInt64(out var seconds))
                {
                    var when = DateTimeOffset.FromUnixTimeSeconds(seconds);
                    var local = when.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    note = prop.Name == "exp"
                        ? when <= now
                            ? $"{local} (expired {Humanize(now - when)} ago)"
                            : $"{local} (expires in {Humanize(when - now)})"
                        : local;
                }

                list.Add(new JwtClaim(prop.Name, FormatValue(prop.Value), note));
            }
        }

        return list;
    }

    private static string FormatValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Null => "null",
        _ => element.GetRawText(),
    };

    private static string Humanize(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        if (span.TotalSeconds < 60)
            return $"{(int)span.TotalSeconds}s";
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes}min";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours}h";
        return $"{(int)span.TotalDays}d";
    }
}
