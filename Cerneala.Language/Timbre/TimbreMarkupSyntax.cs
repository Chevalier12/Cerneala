using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

namespace Cerneala.Language.Timbre;

internal sealed class TimbreValueSyntax
{
    public TimbreValueSyntax(string name, TextSpan nameSpan, string value, TextSpan valueSpan)
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

internal sealed class TimbreParameterSyntax
{
    public TimbreParameterSyntax(TextSpan keywordSpan, string name, TextSpan nameSpan, string typeName, TextSpan typeSpan, string value, TextSpan valueSpan)
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

internal sealed class TimbreModifierSyntax
{
    public TimbreModifierSyntax(TextSpan keywordSpan, string kind, TextSpan kindSpan, IReadOnlyList<TimbreValueSyntax> inputs)
    {
        KeywordSpan = keywordSpan;
        Kind = kind;
        KindSpan = kindSpan;
        Inputs = inputs;
    }

    public TextSpan KeywordSpan { get; }

    public string Kind { get; }

    public TextSpan KindSpan { get; }

    public IReadOnlyList<TimbreValueSyntax> Inputs { get; }
}

internal sealed class TimbreDirectiveReference
{
    public TimbreDirectiveReference(string keyword, TextSpan span)
    {
        Keyword = keyword;
        Span = span;
    }

    public string Keyword { get; }

    public TextSpan Span { get; }
}

// One `@sound Name { … }` node: TimbreValueSyntax (Source, Volume, Loop,
// AutoPlay) and TimbreModifierSyntax statements in source order.
internal sealed class TimbreSoundSyntax
{
    public TimbreSoundSyntax(TextSpan keywordSpan, string name, TextSpan nameSpan, IReadOnlyList<object> statements)
    {
        KeywordSpan = keywordSpan;
        Name = name;
        NameSpan = nameSpan;
        Statements = statements;
    }

    public TextSpan KeywordSpan { get; }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public IReadOnlyList<object> Statements { get; }
}

// Statements of a TimbreClip body (or an inline `@timbre { … }` block) in
// source order: TimbreParameterSyntax and TimbreSoundSyntax. A property or
// @modifier written directly in the body is the removed single-sound form;
// the first one is kept in LegacySpan.
internal sealed class TimbreClipBodySyntax
{
    public TimbreClipBodySyntax(
        IReadOnlyList<object> statements,
        TextSpan? legacySpan,
        IReadOnlyList<TimbreDirectiveReference> foreignDirectives,
        IReadOnlyList<EmbeddedDiagnostic> diagnostics)
    {
        Statements = statements;
        LegacySpan = legacySpan;
        ForeignDirectives = foreignDirectives;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<object> Statements { get; }

    public TextSpan? LegacySpan { get; }

    public IReadOnlyList<TimbreDirectiveReference> ForeignDirectives { get; }

    public IReadOnlyList<EmbeddedDiagnostic> Diagnostics { get; }
}

internal enum TimbreCommandKind
{
    Play,
    Stop,
    Pause,
    Resume,
    Seek
}

// `@play $Owner.timbre.Sound;` and its siblings; Owner is "self", "owner" or
// an element name.
internal sealed class TimbreCommandSyntax
{
    public TimbreCommandSyntax(
        TimbreCommandKind kind,
        TextSpan keywordSpan,
        string owner,
        TextSpan ownerSpan,
        string sound,
        TextSpan soundSpan,
        string? seekValue,
        TextSpan seekSpan)
    {
        Kind = kind;
        KeywordSpan = keywordSpan;
        Owner = owner;
        OwnerSpan = ownerSpan;
        Sound = sound;
        SoundSpan = soundSpan;
        SeekValue = seekValue;
        SeekSpan = seekSpan;
    }

    public TimbreCommandKind Kind { get; }

    public TextSpan KeywordSpan { get; }

    public string Owner { get; }

    // Covers '$' and the owner name.
    public TextSpan OwnerSpan { get; }

    public string Sound { get; }

    public TextSpan SoundSpan { get; }

    public string? SeekValue { get; }

    public TextSpan SeekSpan { get; }
}

// `@timbre $Clip(Parameter = value, …);`
internal sealed class TimbreClipReferenceSyntax
{
    public TimbreClipReferenceSyntax(string clipName, TextSpan clipSpan, IReadOnlyList<TimbreValueSyntax> arguments)
    {
        ClipName = clipName;
        ClipSpan = clipSpan;
        Arguments = arguments;
    }

    public string ClipName { get; }

    // Covers '$' and the clip name.
    public TextSpan ClipSpan { get; }

    public IReadOnlyList<TimbreValueSyntax> Arguments { get; }
}

internal static class TimbreMarkupSyntax
{
    public const string SyntaxId = "CERNEALAUI030";

    public static IReadOnlyList<string> CommandKeywords { get; } = ["@play", "@stop", "@pause", "@resume", "@seek"];

    public static IReadOnlyList<string> ClipKeywords { get; } = ["@parameter", "@sound", "@modifier"];

    public static bool IsCommandKeyword(string keyword) => CommandKeywords.Contains(keyword, StringComparer.Ordinal);

    public static bool TryGetCommandKind(string keyword, out TimbreCommandKind kind)
    {
        switch (keyword)
        {
            case "@play":
                kind = TimbreCommandKind.Play;
                return true;
            case "@stop":
                kind = TimbreCommandKind.Stop;
                return true;
            case "@pause":
                kind = TimbreCommandKind.Pause;
                return true;
            case "@resume":
                kind = TimbreCommandKind.Resume;
                return true;
            case "@seek":
                kind = TimbreCommandKind.Seek;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    public static string KeywordText(TimbreCommandKind kind) => kind switch
    {
        TimbreCommandKind.Play => "@play",
        TimbreCommandKind.Stop => "@stop",
        TimbreCommandKind.Pause => "@pause",
        TimbreCommandKind.Resume => "@resume",
        _ => "@seek"
    };

    public static string SoundPathMessage(string keyword) =>
        keyword + " requires a sound path: '$self.timbre.Sound', '$owner.timbre.Sound' or '$Name.timbre.Sound'.";

    // Parses the content of a <TimbreClip> element or of an inline
    // `@timbre { … }` block. `text` has child elements and comments blanked;
    // `offset` is its absolute document position.
    public static TimbreClipBodySyntax ParseClipBody(string text, int offset)
    {
        Scanner scanner = new(text, offset);
        List<object> statements = new();
        List<TimbreDirectiveReference> foreign = new();
        List<EmbeddedDiagnostic> diagnostics = new();
        TextSpan? legacy = null;
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
                switch (keyword)
                {
                    case "@parameter":
                        if (ParseParameter(scanner, keywordSpan, diagnostics) is TimbreParameterSyntax parameter)
                        {
                            statements.Add(parameter);
                        }

                        break;
                    case "@sound":
                        if (ParseSound(scanner, keywordSpan, foreign, diagnostics) is TimbreSoundSyntax sound)
                        {
                            statements.Add(sound);
                        }

                        break;
                    case "@modifier":
                        legacy ??= keywordSpan;
                        _ = ParseModifier(scanner, keywordSpan, new List<EmbeddedDiagnostic>());
                        break;
                    default:
                        foreign.Add(new TimbreDirectiveReference(keyword, keywordSpan));
                        scanner.SkipStatement();
                        break;
                }

                continue;
            }

            if (scanner.Peek == '}')
            {
                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "Unexpected closing '}'.", scanner.Span(start, 1)));
                scanner.Advance();
                continue;
            }

            if (ParseAssignment(scanner, new List<EmbeddedDiagnostic>(), "TimbreClip property assignment") is TimbreValueSyntax assignment)
            {
                legacy ??= assignment.NameSpan;
            }
            else
            {
                legacy ??= scanner.Span(start, Math.Max(1, scanner.Position - start));
            }
        }

        return new TimbreClipBodySyntax(statements, legacy, foreign, diagnostics);
    }

    // Parses one command. `text` starts right after the keyword and ends
    // before the terminating ';' (which the directive parser checks).
    public static TimbreCommandSyntax? ParseCommand(
        TimbreCommandKind kind,
        TextSpan keywordSpan,
        string text,
        int offset,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        Scanner scanner = new(text, offset);
        scanner.SkipWhitespace();
        int trimmedEnd = text.TrimEnd().Length;
        TextSpan statementSpan = trimmedEnd > scanner.Position
            ? scanner.Span(scanner.Position, trimmedEnd - scanner.Position)
            : keywordSpan;
        string keyword = KeywordText(kind);
        if (!TryReadSoundPath(scanner, out string owner, out TextSpan ownerSpan, out string sound, out TextSpan soundSpan))
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, SoundPathMessage(keyword), statementSpan));
            return null;
        }

        scanner.SkipWhitespace();
        string? seekValue = null;
        TextSpan seekSpan = default;
        if (kind == TimbreCommandKind.Seek)
        {
            string to = scanner.ReadIdentifier();
            scanner.SkipWhitespace();
            int valueStart = scanner.Position;
            seekValue = scanner.ReadToEnd().Trim();
            seekSpan = scanner.Span(valueStart, Math.Max(1, seekValue.Length));
            if (to != "to" || seekValue.Length == 0 || seekValue.IndexOf(' ') >= 0)
            {
                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@seek requires '$element.timbre.Sound to <duration>'.", statementSpan));
                return null;
            }
        }
        else if (!scanner.AtEnd)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, SoundPathMessage(keyword), statementSpan));
            return null;
        }

        return new TimbreCommandSyntax(kind, keywordSpan, owner, ownerSpan, sound, soundSpan, seekValue, seekSpan);
    }

    // Parses `$Clip` with optional `(Name = value, …)`. `text` starts right
    // after `@timbre` and ends before the terminating ';'.
    public static TimbreClipReferenceSyntax? ParseClipReference(
        TextSpan keywordSpan,
        string text,
        int offset,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        const string shape = "@timbre requires '$TimbreClip' with optional '(Parameter = value, …)', or an inline '{ … }' block.";
        Scanner scanner = new(text, offset);
        scanner.SkipWhitespace();
        int trimmedEnd = text.TrimEnd().Length;
        TextSpan statementSpan = trimmedEnd > scanner.Position
            ? scanner.Span(scanner.Position, trimmedEnd - scanner.Position)
            : keywordSpan;
        if (scanner.AtEnd || scanner.Peek != '$')
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, shape, statementSpan));
            return null;
        }

        int dollar = scanner.Position;
        scanner.Advance();
        string clipName = scanner.ReadIdentifier();
        TextSpan clipSpan = scanner.Span(dollar, clipName.Length + 1);
        if (clipName.Length == 0)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, shape, statementSpan));
            return null;
        }

        List<TimbreValueSyntax> arguments = new();
        scanner.SkipWhitespace();
        if (!scanner.AtEnd && scanner.Peek == '(')
        {
            scanner.Advance();
            while (true)
            {
                scanner.SkipWhitespace();
                if (scanner.AtEnd)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, shape, statementSpan));
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
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@timbre arguments must use 'Name = value'.", scanner.Span(nameStart, Math.Max(1, name.Length))));
                    return null;
                }

                scanner.Advance();
                scanner.SkipWhitespace();
                int valueStart = scanner.Position;
                string value = scanner.ReadValue(stopAtComma: true).TrimEnd();
                if (value.Length == 0)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@timbre arguments must use 'Name = value'.", scanner.Span(nameStart, Math.Max(1, name.Length))));
                    return null;
                }

                arguments.Add(new TimbreValueSyntax(name, scanner.Span(nameStart, name.Length), value, scanner.Span(valueStart, value.Length)));
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

                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, shape, statementSpan));
                return null;
            }
        }

        scanner.SkipWhitespace();
        if (!scanner.AtEnd)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, shape, statementSpan));
            return null;
        }

        return new TimbreClipReferenceSyntax(clipName, clipSpan, arguments);
    }

    // `$Owner.timbre.Sound`, nothing else.
    private static bool TryReadSoundPath(Scanner scanner, out string owner, out TextSpan ownerSpan, out string sound, out TextSpan soundSpan)
    {
        owner = sound = string.Empty;
        ownerSpan = soundSpan = default;
        if (scanner.AtEnd || scanner.Peek != '$')
        {
            return false;
        }

        int dollar = scanner.Position;
        scanner.Advance();
        owner = scanner.ReadIdentifier();
        ownerSpan = scanner.Span(dollar, owner.Length + 1);
        if (owner.Length == 0 || scanner.AtEnd || scanner.Peek != '.')
        {
            return false;
        }

        scanner.Advance();
        if (scanner.ReadIdentifier() != "timbre" || scanner.AtEnd || scanner.Peek != '.')
        {
            return false;
        }

        scanner.Advance();
        int soundStart = scanner.Position;
        sound = scanner.ReadIdentifier();
        soundSpan = scanner.Span(soundStart, sound.Length);
        return sound.Length > 0 && (scanner.AtEnd || char.IsWhiteSpace(scanner.Peek));
    }

    private static TimbreSoundSyntax? ParseSound(
        Scanner scanner,
        TextSpan keywordSpan,
        ICollection<TimbreDirectiveReference> foreign,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        scanner.SkipWhitespace();
        int nameStart = scanner.Position;
        string name = scanner.ReadIdentifier();
        TextSpan nameSpan = scanner.Span(nameStart, name.Length);
        scanner.SkipWhitespace();
        if (name.Length == 0 || scanner.AtEnd || scanner.Peek != '{')
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@sound requires 'Name { … }'.", keywordSpan));
            scanner.SkipStatement();
            return null;
        }

        scanner.Advance();
        List<object> statements = new();
        while (true)
        {
            scanner.SkipWhitespace();
            if (scanner.AtEnd)
            {
                diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@sound block is missing its closing '}'.", keywordSpan));
                break;
            }

            if (scanner.Peek == '}')
            {
                scanner.Advance();
                break;
            }

            int start = scanner.Position;
            if (scanner.Peek == '@')
            {
                string keyword = scanner.ReadDirectiveKeyword();
                TextSpan span = scanner.Span(start, keyword.Length);
                if (keyword == "@modifier")
                {
                    if (ParseModifier(scanner, span, diagnostics) is TimbreModifierSyntax modifier)
                    {
                        statements.Add(modifier);
                    }
                }
                else if (keyword == "@parameter")
                {
                    diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "@parameter is declared at TimbreClip level, not inside @sound.", span));
                    scanner.SkipStatement();
                }
                else
                {
                    foreign.Add(new TimbreDirectiveReference(keyword, span));
                    scanner.SkipStatement();
                }

                continue;
            }

            if (ParseAssignment(scanner, diagnostics, "@sound property assignment") is TimbreValueSyntax assignment)
            {
                statements.Add(assignment);
            }
        }

        return new TimbreSoundSyntax(keywordSpan, name, nameSpan, statements);
    }

    private static TimbreParameterSyntax? ParseParameter(Scanner scanner, TextSpan keywordSpan, ICollection<EmbeddedDiagnostic> diagnostics)
    {
        int statementStart = scanner.Position;
        string statement = scanner.ReadStatement(out bool terminated);
        TextSpan span = scanner.Span(statementStart, Math.Max(1, statement.TrimEnd().Length));
        int colon = statement.IndexOf(':');
        int equals = statement.IndexOf('=');
        if (!terminated || colon <= 0 || equals <= colon)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "Timbre parameter requires 'Name: float = default'.", terminated ? span : keywordSpan));
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
            diagnostics.Add(new EmbeddedDiagnostic(SyntaxId, "Timbre parameter requires 'Name: float = default'.", span));
            return null;
        }

        return new TimbreParameterSyntax(
            keywordSpan,
            name,
            Trimmed(scanner, statementStart, rawName),
            type,
            Trimmed(scanner, statementStart + colon + 1, rawType),
            value,
            Trimmed(scanner, statementStart + equals + 1, rawValue));
    }

    private static TimbreModifierSyntax? ParseModifier(Scanner scanner, TextSpan keywordSpan, ICollection<EmbeddedDiagnostic> diagnostics)
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
        List<TimbreValueSyntax> inputs = new();
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

            if (ParseAssignment(scanner, diagnostics, "Timbre modifier input assignment") is TimbreValueSyntax input)
            {
                inputs.Add(input);
            }
        }

        return new TimbreModifierSyntax(keywordSpan, kind, scanner.Span(kindStart, kind.Length), inputs);
    }

    private static TimbreValueSyntax? ParseAssignment(Scanner scanner, ICollection<EmbeddedDiagnostic> diagnostics, string what)
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
        return new TimbreValueSyntax(name, scanner.Span(nameStart, name.Length), value, scanner.Span(valueStart, value.Length));
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
