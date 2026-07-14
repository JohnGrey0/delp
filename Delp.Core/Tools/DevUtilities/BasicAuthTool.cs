namespace Delp.Core.Tools.DevUtilities;

/// <summary>An encoded HTTP Basic Authentication credential, presented in the common forms.</summary>
public sealed record BasicAuthResult(string Base64, string Header, string CurlHeaderFlag, string CurlUserFlag);

/// <summary>A decoded username/password pair.</summary>
public sealed record BasicAuthCredentials(string Username, string Password);

/// <summary>Builds and decodes RFC 7617 HTTP Basic Authentication headers.</summary>
public static class BasicAuthTool
{
    public static BasicAuthResult Encode(string user, string password)
    {
        if (user.Contains(':'))
            throw new FormatException("Username may not contain a colon ':' — it is the user:password separator.");

        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{password}"));
        var header = $"Authorization: Basic {base64}";
        return new BasicAuthResult(
            base64,
            header,
            $"curl -H \"{header}\"",
            $"curl -u \"{user}:{password}\"");
    }

    /// <summary>Accepts a full <c>Authorization: Basic xxx</c> header, a bare <c>Basic xxx</c>
    /// value, or a raw Base64 payload.</summary>
    /// <exception cref="FormatException">The input is not valid Base64 or is not a <c>user:password</c> pair.</exception>
    public static BasicAuthCredentials Decode(string headerOrBase64)
    {
        var value = headerOrBase64.Trim();

        const string headerPrefix = "authorization:";
        if (value.Length >= headerPrefix.Length && value[..headerPrefix.Length].Equals(headerPrefix, StringComparison.OrdinalIgnoreCase))
            value = value[headerPrefix.Length..].Trim();

        const string schemePrefix = "basic ";
        if (value.Length >= schemePrefix.Length && value[..schemePrefix.Length].Equals(schemePrefix, StringComparison.OrdinalIgnoreCase))
            value = value[schemePrefix.Length..].Trim();

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new FormatException("Not valid Base64 — expected an 'Authorization: Basic <base64>' header or a bare Base64 payload.");
        }

        var decoded = System.Text.Encoding.UTF8.GetString(bytes);
        var sep = decoded.IndexOf(':');
        if (sep < 0)
            throw new FormatException("Decoded value is not a 'user:password' pair (no ':' found).");

        return new BasicAuthCredentials(decoded[..sep], decoded[(sep + 1)..]);
    }
}
