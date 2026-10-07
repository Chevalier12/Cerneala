using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

namespace Cerneala.Language.Timbre;

internal sealed class SoundValueSyntax
{
    public SoundValueSyntax(string name, TextSpan nameSpan, string value, TextSpan valueSpan)
    {
        Name = name;
        NameSpan = nameSpan;
        Value = value;
        ValueSpan = valueSpan;
    }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public string Value { get; }

    public TextSpan ValueSpan { get; }
}

internal sealed class SoundParameterSyntax
{
    public SoundParameterSyntax(TextSpan keywordSpan, string name, TextSpan nameSpan, string typeName, TextSpan typeSpan, string value, TextSpan valueSpan)
    {
        KeywordSpan = keywordSpan;
        Name = name;
        NameSpan = nameSpan;
        TypeName = typeName;
        TypeSpan = typeSpan;
        Value = value;
        ValueSpan = valueSpan;
    }

    public TextSpan KeywordSpan { get; }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public string TypeName { get; }

    public TextSpan TypeSpan { get; }

    public string Value { get; }

    public TextSpan ValueSpan { get; }
}

internal sealed class SoundModifierSyntax
{
    public SoundModifierSyntax(TextSpan keywordSpan, string kind, TextSpan kindSpan, IReadOnlyList<SoundValueSyntax> inputs)
    {
        KeywordSpan = keywordSpan;
        Kind = kind;
        KindSpan = kindSpan;
        Inputs = inputs;
    }

    public TextSpan KeywordSpan { get; }

    public string Kind { get; }

    public TextSpan KindSpan { get; }

    public IReadOnlyList<SoundValueSyntax> Inputs { get; }
}

internal sealed class SoundDirectiveReference
{
    public SoundDirectiveReference(string keyword, TextSpan span)
    {
        Keyword = keyword;
        Span = span;
    }

    public string Keyword { get; }

    public TextSpan Span { get; }
}

// Statements of a SoundClip body in source order: SoundValueSyntax
// (property assignment), SoundParameterSyntax and SoundModifierSyntax.
internal sealed class SoundClipBodySyntax
{
    public SoundClipBodySyntax(
        IReadOnlyList<object> statements,
        IReadOnlyList<SoundDirectiveReference> foreignDirectives,
        IReadOnlyList<EmbeddedDiagnostic> diagnostics)
    {
        Statements = statements;
        ForeignDirectives = foreignDirectives;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<object> Statements { get; }

    public IReadOnlyList<SoundDirectiveReference> ForeignDirectives { get; }

    public IReadOnlyList<EmbeddedDiagnostic> Diagnostics { get; }
}

internal enum SoundActionKind
{
    Play,
    Cancel,
    Pause,
    Resume,
    Seek
}

internal sealed class SoundActionSyntax
{
    public SoundActionSyntax(
        SoundActionKind kind,
        TextSpan keywordSpan,
        string? clipName,
        TextSpan clipSpan,
        IReadOnlyList<SoundValueSyntax> arguments,
        string? handleName,
        TextSpan handleSpan,
        string? seekValue,
        TextSpan seekSpan)
    {
        Kind = kind;
        KeywordSpan = keywordSpan;
        ClipName = clipName;
        ClipSpan = clipSpan;
        Arguments = arguments;
        HandleName = handleName;
        HandleSpan = handleSpan;
        SeekValue = seekValue;
        SeekSpan = seekSpan;
    }

    public SoundActionKind Kind { get; }

    public TextSpan KeywordSpan { get; }

    public string? ClipName { get; }

    public TextSpan ClipSpan { get; }

    public IReadOnlyList<SoundValueSyntax> Arguments { get; }

    public string? HandleName { get; }

    public TextSpan HandleSpan { get; }

    public string? SeekValue { get; }

    public TextSpan SeekSpan { get; }
}

internal static class SoundMarkupSyntax
{
    public const string SyntaxId = "CERNEALAUI030";

    public static IReadOnlyList<string> ActionKeywords { get; } = ["@sound", "@pause", "@resume", "@seek"];

    public static IReadOnlyList<string> ClipKeywords { get; } = ["@parameter", "@modifier"];

    public static bool IsActionKeyword(string keyword) => ActionKeywords.Contains(keyword, StringComparer.Ordinal);

    public static bool TryGetActionKind(string keyword, out SoundActionKind kind)
    {
        switch (keyword)
        {
            case "@sound":
                kind = SoundActionKind.Play;
                return true;
            case "@cancel":
                kind = SoundActionKind.Cancel;
                return true;
            case "@pause":
                kind = SoundActionKind.Pause;
                return true;
            case "@resume":
                kind = SoundActionKind.Resume;
                return true;
            case "@seek":
                kind = SoundActionKind.Seek;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    // Parses the direct text of a <SoundClip> element. `text` is the element
    // content with child elements and comments blanked; `offset` is its
    // absolute document position.
    public static SoundClipBodySyntax ParseClipBody(string text, int offset)
    {
        Scanner scanner = new(text, offset);
        List<object> statements = new();
        List<SoundDirectiveReference> foreign = new();
        List<EmbeddedDiagnostic> diagnostics = new();
        while (true)
        {
            scanner.SkipWhitespace();
            if (scanner.AtEnd)
            {
                break;
            }

            int start = scanner.Position;
            if (scanner.Peek == '@')
            {
                string keyword = scanner.ReadDirectiveKeyword();
                TextSpan keywordSpan = scanner.Span(start, keyword.Length);
                if (keyword == "@parameter")
                {
                    if (ParseParameter(scanner, keywordSpan, diagnostics) is SoundParameterSyntax parameter)
                    {
                        statements.Add(parameter);
                    }
                }
                else if (keyword == "@modifier")
                {
                    if (ParseModifier(scanner, keywordSpan, diagnostics) is SoundModifierSyntax modifier)
                    {
                        statements.Add(modifier);
                    }
                }
                else
                {
                    foreign.Add(new SoundDirectiveReference(keyword, keywordSpan));
                    scanner.SkipStatement();
                }

                continue;
            }

            if (ParseAssignment(scanner, diagnostics, "SoundClip property assignment") is SoundValueSyntax assignment)
            {
                statements.Add(assignment);
            }
        }

        return new SoundClipBodySyntax(statements, foreign, diagnostics);
    }

    // Parses one action statement. `text` starts right after the keyword and
    // ends before the terminating ';' (which the directive parser checks).
    public static SoundActionSyntax? ParseAction(
        SoundActionKind kind,
        TextSpan keywordSpan,
        string text,
        int offset,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        Scanner scanner = new(text, offset);
        scanner.SkipWhitespace();
        TextSpan statementSpan = scanner.Span(scanner.Position, Math.Max(1, text.TrimEnd().Length - scanner.Position));
        if (kind != SoundActionKind.Play)
        {
            int handleStart = scanner.Position;
            string handle = scanner.ReadIdentifier();
            TextSpan handleSpan = scanner.Span(handleStart, handle.Length);
            string? seekValue = null;
            TextSpan seekSpan = default;
            if (kind == SoundActionKind.Seek)
            {
                scanner.SkipWhitespace();
                int toStart = scanner.Position;
                string to = scanner.ReadIdentifier();
                scanner.SkipWhitespace();
                int valueStart = scanner.Position;
                seekValue = scanner.ReadToEnd().Trim();
                seekSpan = scanner.Span(valueStart, Math.Max(1, seekValue.Length));
                if (handle.Length == 0 || to != "to" || seekValue.Length == 0 || seekValue.IndexOf(' ') >= 0)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@seek requires 'Handle to <duration>'.", toStart > handleStart ? statementSpan : keywordSpan));
                    return null;
                }
            }
            else
            {
                scanner.SkipWhitespace();
                if (handle.Length == 0 || !scanner.AtEnd)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, KeywordText(kind) + " requires one declared handle name.", statementSpan));
                    return null;
                }
            }

            return new SoundActionSyntax(kind, keywordSpan, null, default, Array.Empty<SoundValueSyntax>(), handle, handleSpan, seekValue, seekSpan);
        }

        const string playShape = "@sound requires '$SoundClip' followed by optional named arguments and an optional 'as Handle'.";
        if (scanner.AtEnd || scanner.Peek != '$')
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, playShape, statementSpan));
            return null;
        }

        int dollar = scanner.Position;
        scanner.Advance();
        string clipName = scanner.ReadIdentifier();
        TextSpan clipSpan = scanner.Span(dollar, clipName.Length + 1);
        if (clipName.Length == 0)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, playShape, statementSpan));
            return null;
        }

        List<SoundValueSyntax> arguments = new();
        scanner.SkipWhitespace();
        if (!scanner.AtEnd && scanner.Peek == '(')
        {
            scanner.Advance();
            while (true)
            {
                scanner.SkipWhitespace();
                if (scanner.AtEnd)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, playShape, statementSpan));
                    return null;
                }

                if (scanner.Peek == ')' && arguments.Count == 0)
                {
                    scanner.Advance();
                    break;
                }

                int nameStart = scanner.Position;
                string name = scanner.ReadIdentifier();
                scanner.SkipWhitespace();
                if (name.Length == 0 || scanner.AtEnd || scanner.Peek != '=')
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@sound arguments must use 'Name = value'.", scanner.Span(nameStart, Math.Max(1, name.Length))));
                    return null;
                }

                scanner.Advance();
                scanner.SkipWhitespace();
                int valueStart = scanner.Position;
                string value = scanner.ReadValue(stopAtComma: true).TrimEnd();
                if (value.Length == 0)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@sound arguments must use 'Name = value'.", scanner.Span(nameStart, Math.Max(1, name.Length))));
                    return null;
                }

                arguments.Add(new SoundValueSyntax(name, scanner.Span(nameStart, name.Length), value, scanner.Span(valueStart, value.Length)));
                scanner.SkipWhitespace();
                if (!scanner.AtEnd && scanner.Peek == ',')
                {
                    scanner.Advance();
                    continue;
                }

                if (!scanner.AtEnd && scanner.Peek == ')')
                {
                    scanner.Advance();
                    break;
                }

                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, playShape, statementSpan));
                return null;
            }
        }

        string? handleName = null;
        TextSpan handleNameSpan = default;
        scanner.SkipWhitespace();
        if (!scanner.AtEnd)
        {
            string word = scanner.ReadIdentifier();
            scanner.SkipWhitespace();
            int handleStart = scanner.Position;
            string handle = scanner.ReadIdentifier();
            scanner.SkipWhitespace();
            if (word != "as" || handle.Length == 0 || !scanner.AtEnd)
            {
                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, playShape, statementSpan));
                return null;
            }

            handleName = handle;
            handleNameSpan = scanner.Span(handleStart, handle.Length);
        }

        return new SoundActionSyntax(kind, keywordSpan, clipName, clipSpan, arguments, handleName, handleNameSpan, null, default);
    }

    public static string KeywordText(SoundActionKind kind) => kind switch
    {
        SoundActionKind.Play => "@sound",
        SoundActionKind.Cancel => "@cancel",
        SoundActionKind.Pause => "@pause",
        SoundActionKind.Resume => "@resume",
        _ => "@seek"
    };

    private static SoundParameterSyntax? ParseParameter(Scanner scanner, TextSpan keywordSpan, ICollection<EmbeddedDiagnostic> diagnostics)
    {
        int statementStart = scanner.Position;
        string statement = scanner.ReadStatement(out bool terminated);
        TextSpan span = scanner.Span(statementStart, Math.Max(1, statement.TrimEnd().Length));
        int colon = statement.IndexOf(':');
        int equals = statement.IndexOf('=');
        if (!terminated || colon <= 0 || equals <= colon)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "Sound parameter requires 'Name: float = default'.", terminated ? span : keywordSpan));
            return null;
        }

        string rawName = statement.Substring(0, colon);
        string rawType = statement.Substring(colon + 1, equals - colon - 1);
        string rawValue = statement.Substring(equals + 1);
        string name = rawName.Trim();
        string type = rawType.Trim();
        string value = rawValue.Trim();
        if (!IsIdentifier(name) || type.Length == 0 || value.Length == 0)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "Sound parameter requires 'Name: float = default'.", span));
            return null;
        }

        return new SoundParameterSyntax(
            keywordSpan,
            name,
            Trimmed(scanner, statementStart, rawName),
            type,
            Trimmed(scanner, statementStart + colon + 1, rawType),
            value,
            Trimmed(scanner, statementStart + equals + 1, rawValue));
    }

    private static SoundModifierSyntax? ParseModifier(Scanner scanner, TextSpan keywordSpan, ICollection<EmbeddedDiagnostic> diagnostics)
    {
        scanner.SkipWhitespace();
        int kindStart = scanner.Position;
        string kind = scanner.ReadIdentifier();
        scanner.SkipWhitespace();
        if (kind.Length == 0 || scanner.AtEnd || scanner.Peek != '{')
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@modifier requires 'Kind { Input = value; }'.", keywordSpan));
            scanner.SkipStatement();
            return null;
        }

        scanner.Advance();
        List<SoundValueSyntax> inputs = new();
        while (true)
        {
            scanner.SkipWhitespace();
            if (scanner.AtEnd)
            {
                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@modifier block is missing its closing '}'.", keywordSpan));
                return null;
            }

            if (scanner.Peek == '}')
            {
                scanner.Advance();
                break;
            }

            if (ParseAssignment(scanner, diagnostics, "Sound modifier input assignment") is SoundValueSyntax input)
            {
                inputs.Add(input);
            }
        }

        return new SoundModifierSyntax(keywordSpan, kind, scanner.Span(kindStart, kind.Length), inputs);
    }

    private static SoundValueSyntax? ParseAssignment(Scanner scanner, ICollection<EmbeddedDiagnostic> diagnostics, string what)
    {
        int nameStart = scanner.Position;
        string name = scanner.ReadIdentifier();
        scanner.SkipWhitespace();
        if (name.Length == 0 || scanner.AtEnd || scanner.Peek != '=')
        {
            int length = Math.Max(1, name.Length);
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, what + " requires 'Name = value;'.", scanner.Span(nameStart, length)));
            scanner.SkipStatement();
            return null;
        }

        scanner.Advance();
        scanner.SkipWhitespace();
        int valueStart = scanner.Position;
        string value = scanner.ReadValue(stopAtComma: false).TrimEnd();
        if (scanner.AtEnd || scanner.Peek != ';' || value.Length == 0)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, what + " must end with ';'.", scanner.Span(nameStart, Math.Max(1, scanner.Position - nameStart))));
            if (!scanner.AtEnd && scanner.Peek == ';')
            {
                scanner.Advance();
            }

            return null;
        }

        scanner.Advance();
        return new SoundValueSyntax(name, scanner.Span(nameStart, name.Length), value, scanner.Span(valueStart, value.Length));
    }

    private static TextSpan Trimmed(Scanner scanner, int rawStart, string raw)
    {
        int leading = raw.Length - raw.TrimStart().Length;
        return scanner.Span(rawStart + leading, raw.Trim().Length);
    }

    internal static bool IsIdentifier(string text) =>
        text.Length > 0 &&
        (char.IsLetter(text[0]) || text[0] == '_') &&
        text.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');

    private sealed class Scanner
    {
        private readonly string text;
        private readonly int offset;

        public Scanner(string text, int offset)
        {
            this.text = text;
            this.offset = offset;
        }

        // Cursor index into the buffer; Span adds the absolute offset.
        public int Position { get; private set; }

        public bool AtEnd => Position >= text.Length;

        public char Peek => text[Position];

        public TextSpan Span(int relativeStart, int length) => new(offset + relativeStart, length);

        public void Advance() => Position++;

        public void SkipWhitespace()
        {
            while (!AtEnd && char.IsWhiteSpace(Peek))
            {
                Position++;
            }
        }

        public string ReadIdentifier()
        {
            int start = Position;
            while (!AtEnd && (char.IsLetterOrDigit(Peek) || Peek == '_'))
            {
                Position++;
            }

            return text.Substring(start, Position - start);
        }

        public string ReadDirectiveKeyword()
        {
            int start = Position;
            Position++;
            while (!AtEnd && (char.IsLetterOrDigit(Peek) || Peek == '_'))
            {
                Position++;
            }

            return text.Substring(start, Position - start);
        }

        public string ReadToEnd()
        {
            string rest = text.Substring(Position);
            Position = text.Length;
            return rest;
        }

        // Reads a value up to ';', '}', a line break, or (optionally) a
        // top-level ',' / ')'; quoted text may contain any of them.
        public string ReadValue(bool stopAtComma)
        {
            int start = Position;
            bool quoted = false;
            while (!AtEnd)
            {
                char character = Peek;
                if (quoted)
                {
                    if (character == '"' && text[Position - 1] != '\\')
                    {
                        quoted = false;
                    }
                }
                else if (character == '"')
                {
                    quoted = true;
                }
                else if (character is ';' or '}' or '\r' or '\n' || stopAtComma && character is ',' or ')')
                {
                    break;
                }

                Position++;
            }

            return text.Substring(start, Position - start);
        }

        // Reads up to and including the next top-level ';' without crossing a
        // block brace or another directive.
        public string ReadStatement(out bool terminated)
        {
            int start = Position;
            bool quoted = false;
            while (!AtEnd)
            {
                char character = Peek;
                if (quoted)
                {
                    quoted = !(character == '"' && text[Position - 1] != '\\');
                }
                else if (character == '"')
                {
                    quoted = true;
                }
                else if (character == ';')
                {
                    string statement = text.Substring(start, Position - start);
                    Position++;
                    terminated = true;
                    return statement;
                }
                else if (character is '{' or '}' or '@')
                {
                    break;
                }

                Position++;
            }

            terminated = false;
            return text.Substring(start, Position - start);
        }

        public void SkipStatement()
        {
            bool quoted = false;
            int depth = 0;
            while (!AtEnd)
            {
                char character = Peek;
                Position++;
                if (quoted)
                {
                    quoted = character != '"';
                    continue;
                }

                if (character == '"')
                {
                    quoted = true;
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    if (--depth <= 0)
                    {
                        return;
                    }
                }
                else if (character == ';' && depth == 0)
                {
                    return;
                }
            }
        }
    }
}
