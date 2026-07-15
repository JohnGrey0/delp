namespace Delp.Core.Tools.Common;

/// <summary>
/// Splits a shell-ish command line into argv-style tokens. Understands POSIX
/// single/double/ANSI-C ($'...') quoting and backslash escapes, plus two
/// line-continuation styles people paste from real terminals: a trailing
/// backslash-newline (bash/zsh/sh) and a trailing caret-newline (Windows
/// <c>cmd.exe</c>, the form "Copy as cURL (cmd)" produces). Never throws — an
/// unterminated quote simply runs to the end of input rather than being
/// rejected, since this is used to parse commands pasted by a human, not to
/// validate shell syntax. Shared by curl-convert and docker-convert (both
/// need argv-style splitting of a single command line into flags/values).
/// </summary>
public static class ShellTokenizer
{
    private enum Mode { Normal, Single, Double, AnsiC }

    public static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(input))
            return tokens;

        var buffer = new System.Text.StringBuilder();
        var inToken = false;
        var mode = Mode.Normal;
        var i = 0;
        var n = input.Length;

        void Flush()
        {
            if (inToken)
                tokens.Add(buffer.ToString());
            buffer.Clear();
            inToken = false;
        }

        while (i < n)
        {
            var c = input[i];

            switch (mode)
            {
                case Mode.Normal:
                    if (c is ' ' or '\t' or '\r' or '\n')
                    {
                        Flush();
                        i++;
                        continue;
                    }
                    if (c == '$' && i + 1 < n && input[i + 1] == '\'')
                    {
                        // ANSI-C quoting: $'...' — bash decodes backslash escapes inside it,
                        // unlike a plain '...' string.
                        mode = Mode.AnsiC;
                        inToken = true;
                        i += 2;
                        continue;
                    }
                    if (c == '\'')
                    {
                        mode = Mode.Single;
                        inToken = true;
                        i++;
                        continue;
                    }
                    if (c == '"')
                    {
                        mode = Mode.Double;
                        inToken = true;
                        i++;
                        continue;
                    }
                    if (c == '\\' && TryConsumeNewline(input, i + 1, out var afterBackslash))
                    {
                        // Backslash-newline: line continuation, splice away — not a separator,
                        // not part of any token.
                        i = afterBackslash;
                        continue;
                    }
                    if (c == '\\' && i + 1 < n)
                    {
                        buffer.Append(input[i + 1]);
                        inToken = true;
                        i += 2;
                        continue;
                    }
                    if (c == '^' && TryConsumeNewline(input, i + 1, out var afterCaret))
                    {
                        // cmd.exe caret-newline continuation.
                        i = afterCaret;
                        continue;
                    }
                    buffer.Append(c);
                    inToken = true;
                    i++;
                    continue;

                case Mode.Single:
                    if (c == '\'')
                    {
                        mode = Mode.Normal;
                        i++;
                        continue;
                    }
                    buffer.Append(c);
                    i++;
                    continue;

                case Mode.Double:
                    if (c == '"')
                    {
                        mode = Mode.Normal;
                        i++;
                        continue;
                    }
                    if (c == '\\' && i + 1 < n && TryConsumeNewline(input, i + 1, out var afterEscNewline))
                    {
                        i = afterEscNewline;
                        continue;
                    }
                    if (c == '\\' && i + 1 < n && input[i + 1] is '"' or '\\' or '$' or '`')
                    {
                        buffer.Append(input[i + 1]);
                        i += 2;
                        continue;
                    }
                    buffer.Append(c);
                    i++;
                    continue;

                case Mode.AnsiC:
                    if (c == '\'')
                    {
                        mode = Mode.Normal;
                        i++;
                        continue;
                    }
                    if (c == '\\' && i + 1 < n)
                    {
                        var (decoded, consumed) = DecodeAnsiCEscape(input, i + 1);
                        buffer.Append(decoded);
                        i += 1 + consumed;
                        continue;
                    }
                    buffer.Append(c);
                    i++;
                    continue;
            }
        }

        Flush();
        return tokens;
    }

    /// <summary>Decodes a single ANSI-C backslash escape (the form used inside <c>$'...'</c>)
    /// starting at <paramref name="start"/>, just past the backslash. Recognizes the common bash
    /// escapes plus <c>\xHH</c> hex bytes; anything else is passed through as a literal backslash
    /// + character rather than rejected, matching this tokenizer's never-throw, best-effort
    /// philosophy for the rest of the input.</summary>
    private static (string Text, int Consumed) DecodeAnsiCEscape(string input, int start)
    {
        var c = input[start];
        switch (c)
        {
            case 'n': return ("\n", 1);
            case 't': return ("\t", 1);
            case 'r': return ("\r", 1);
            case 'a': return ("\a", 1);
            case 'b': return ("\b", 1);
            case 'f': return ("\f", 1);
            case 'v': return ("\v", 1);
            case '\\': return ("\\", 1);
            case '\'': return ("'", 1);
            case '"': return ("\"", 1);
            case '0': return ("\0", 1);
            case 'x':
            {
                var hexLen = 0;
                while (hexLen < 2 && start + 1 + hexLen < input.Length && Uri.IsHexDigit(input[start + 1 + hexLen]))
                    hexLen++;
                if (hexLen == 0)
                    return ("x", 1);
                var value = (char)Convert.ToInt32(input.Substring(start + 1, hexLen), 16);
                return (value.ToString(), 1 + hexLen);
            }
            default:
                return ("\\" + c, 1);
        }
    }

    /// <summary>If the input at <paramref name="start"/> is a newline ("\n" or "\r\n"),
    /// returns true and the index just past it.</summary>
    private static bool TryConsumeNewline(string input, int start, out int end)
    {
        if (start < input.Length && input[start] == '\n')
        {
            end = start + 1;
            return true;
        }
        if (start + 1 < input.Length && input[start] == '\r' && input[start + 1] == '\n')
        {
            end = start + 2;
            return true;
        }
        end = start;
        return false;
    }
}
