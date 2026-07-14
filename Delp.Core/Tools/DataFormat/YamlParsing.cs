using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Delp.Core.Tools.DataFormat;

/// <summary>Shared YAML loading with friendly, line/col-aware error messages, used by
/// yaml-format and json-yaml.</summary>
internal static class YamlParsing
{
    /// <exception cref="FormatException">The YAML is not well-formed.</exception>
    public static YamlStream ParseOrThrow(string yaml)
    {
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            return stream;
        }
        catch (YamlException ex)
        {
            throw new FormatException($"Line {ex.Start.Line}, Col {ex.Start.Column}: {CleanMessage(ex.Message)}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // A handful of malformed inputs (e.g. an unterminated flow collection) surface as a
            // raw runtime exception from the parser rather than a YamlException.
            throw new FormatException($"YAML parse error: {ex.Message}");
        }
    }

    /// <summary>Returns null when valid, otherwise the error location/message.</summary>
    public static (int Line, int Col, string Message)? TryGetError(string yaml)
    {
        try
        {
            new YamlStream().Load(new StringReader(yaml));
            return null;
        }
        catch (YamlException ex)
        {
            return ((int)ex.Start.Line, (int)ex.Start.Column, CleanMessage(ex.Message));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return (1, 1, $"YAML parse error: {ex.Message}");
        }
    }

    private static string CleanMessage(string message)
    {
        // YamlException.Message often repeats "(Line: X, Col: Y): " — we already surface that.
        var idx = message.IndexOf(") : ", StringComparison.Ordinal);
        return idx >= 0 ? message[(idx + 4)..] : message;
    }
}
