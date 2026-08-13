namespace Cerneala.Language.Text;

internal sealed class SourceText
{
    private readonly string text;
    private readonly int[] lineStarts;

    private SourceText(string text, long version)
    {
        this.text = text;
        Version = version;
        lineStarts = BuildLineStarts(text);
    }

    public int Length => text.Length;

    public long Version { get; }

    public int LineCount => lineStarts.Length;

    public char this[int offset] => text[offset];

    public static SourceText From(string text, long version = 0) =>
        new(text ?? throw new ArgumentNullException(nameof(text)), version);

    public SourceText WithChange(TextChange change)
    {
        ValidateSpan(change.Span);
        string updated = text.Substring(0, change.Span.Start) +
            change.NewText +
            text.Substring(change.Span.End);
        return new SourceText(updated, checked(Version + 1));
    }

    public SourceText WithChanges(IEnumerable<TextChange> changes)
    {
        TextChange[] ordered = changes
            .OrderByDescending(change => change.Span.Start)
            .ToArray();
        string updated = text;
        int previousStart = Length + 1;
        foreach (TextChange change in ordered)
        {
            ValidateSpan(change.Span);
            if (change.Span.End > previousStart)
            {
                throw new ArgumentException("Text changes must not overlap.", nameof(changes));
            }

            updated = updated.Substring(0, change.Span.Start) +
                change.NewText +
                updated.Substring(change.Span.End);
            previousStart = change.Span.Start;
        }

        return ordered.Length == 0 ? this : new SourceText(updated, checked(Version + 1));
    }

    public string Substring(TextSpan span)
    {
        ValidateSpan(span);
        return text.Substring(span.Start, span.Length);
    }

    public LinePosition GetLinePosition(int offset)
    {
        if (offset < 0 || offset > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        int line = Array.BinarySearch(lineStarts, offset);
        if (line < 0)
        {
            line = ~line - 1;
        }

        return new LinePosition(line, offset - lineStarts[line]);
    }

    public int GetOffset(LinePosition position)
    {
        if (position.Line >= lineStarts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        int offset = lineStarts[position.Line] + position.Character;
        int lineEnd = position.Line + 1 < lineStarts.Length ? lineStarts[position.Line + 1] : Length;
        if (offset > lineEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        return offset;
    }

    public override string ToString() => text;

    private void ValidateSpan(TextSpan span)
    {
        if (span.End > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(span));
        }
    }

    private static int[] BuildLineStarts(string value)
    {
        List<int> starts = new() { 0 };
        for (int offset = 0; offset < value.Length; offset++)
        {
            char character = value[offset];
            if (character == '\r')
            {
                if (offset + 1 < value.Length && value[offset + 1] == '\n')
                {
                    offset++;
                }

                starts.Add(offset + 1);
            }
            else if (character == '\n')
            {
                starts.Add(offset + 1);
            }
        }

        return starts.ToArray();
    }
}
