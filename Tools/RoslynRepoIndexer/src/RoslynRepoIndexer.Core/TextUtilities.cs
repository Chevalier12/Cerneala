using System.Text;
using System.Text.RegularExpressions;

namespace RoslynRepoIndexer.Core;

public static partial class Tokenizer
{
    public static IReadOnlyList<TokenValue> Tokenize(string text, bool includeCodeSingleCharacterTokens = false)
    {
        var tokens = new List<TokenValue>();
        var line = 1;
        var lineStart = 0;
        foreach (Match match in TokenRegex().Matches(text))
        {
            while (lineStart < match.Index)
            {
                var nextNewline = text.IndexOf('\n', lineStart);
                if (nextNewline < 0 || nextNewline >= match.Index)
                {
                    break;
                }

                line++;
                lineStart = nextNewline + 1;
            }

            var column = match.Index - lineStart + 1;
            AddToken(tokens, match.Value, line, column, includeCodeSingleCharacterTokens);
        }

        return tokens;
    }

    public static IReadOnlyList<string> NormalizeTerms(string text)
        => Tokenize(text).Select(t => t.Value).Distinct(StringComparer.Ordinal).ToArray();

    public static IReadOnlyList<TokenValue> TokenizePath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        var values = new List<TokenValue>();
        foreach (var segment in normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            values.AddRange(Tokenize(segment, includeCodeSingleCharacterTokens: true));
        }

        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            values.AddRange(Tokenize(fileName, includeCodeSingleCharacterTokens: true));
        }

        return values
            .GroupBy(value => value.Value, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static void AddToken(List<TokenValue> tokens, string raw, int line, int column, bool includeCodeSingleCharacterTokens)
    {
        var normalized = raw.ToLowerInvariant();
        AddIfAllowed(tokens, normalized, line, column, includeCodeSingleCharacterTokens);
        foreach (var part in SplitIdentifier(raw))
        {
            var value = part.ToLowerInvariant();
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
            {
                AddIfAllowed(tokens, value, line, column, includeCodeSingleCharacterTokens);
            }
        }
    }

    private static void AddIfAllowed(List<TokenValue> tokens, string value, int line, int column, bool includeCodeSingleCharacterTokens)
    {
        if (value.Length > 1 || includeCodeSingleCharacterTokens && IsSignificantCodeSingleCharacter(value))
        {
            tokens.Add(new TokenValue(value, line, column));
        }
    }

    private static bool IsSignificantCodeSingleCharacter(string value)
        => value is "i" or "x" or "y" or "t";

    private static IEnumerable<string> SplitIdentifier(string token)
    {
        foreach (var part in token.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = IdentifierPartRegex().Matches(part).Select(match => match.Value).ToArray();
            if (parts.Length > 1 && parts[0].Equals("I", StringComparison.Ordinal) && parts[1].Length > 1 && char.IsUpper(parts[1][0]))
            {
                yield return parts[0] + parts[1];
                yield return parts[1];
            }

            foreach (var value in parts)
            {
                yield return value;
            }
        }
    }

    [GeneratedRegex("[\\p{L}_][\\p{L}\\p{Nd}_\\-.]*|\\p{Nd}+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex("[A-Z]+(?=[A-Z][a-z]|$)|[A-Z]?[a-z]+|[A-Z]+|\\d+")]
    private static partial Regex IdentifierPartRegex();
}

public static class BinaryFileDetector
{
    public static bool IsBinary(byte[] sample)
        => sample.Contains((byte)0);

    public static bool IsBinaryFile(string path)
    {
        Span<byte> buffer = stackalloc byte[8192];
        using var stream = File.OpenRead(path);
        var read = stream.Read(buffer);
        return IsBinary(buffer[..read].ToArray());
    }
}

public static class DocumentHasher
{
    public static string HashFile(string path)
        => ConfigLoader.HashText(File.ReadAllText(path));

    public static string HashText(string text)
        => ConfigLoader.HashText(text);
}

public sealed class SnippetReader
{
    private readonly string repoRoot;

    public SnippetReader(string repoRoot) => this.repoRoot = repoRoot;

    public string ReadSnippet(string relativePath, int line, int radius = 0)
    {
        var path = Path.Combine(repoRoot, relativePath);
        if (!File.Exists(path) || line <= 0)
        {
            return string.Empty;
        }

        using var reader = File.OpenText(path);
        var current = 0;
        while (reader.ReadLine() is { } value)
        {
            current++;
            if (current == line)
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}

public static partial class QueryParser
{
    public static ParsedQuery Parse(string query)
    {
        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var phrases = new List<string>();
        foreach (Match match in PhraseRegex().Matches(query))
        {
            phrases.Add(match.Groups[1].Value);
        }

        var withoutPhrases = PhraseRegex().Replace(query, " ");
        var terms = new List<string>();
        foreach (var part in withoutPhrases.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = part.IndexOf(':', StringComparison.Ordinal);
            if (colon > 0)
            {
                filters[part[..colon]] = part[(colon + 1)..];
            }
            else
            {
                terms.Add(part);
            }
        }

        return new ParsedQuery(terms, phrases, filters);
    }

    [GeneratedRegex("\"([^\"]+)\"")]
    private static partial Regex PhraseRegex();
}
