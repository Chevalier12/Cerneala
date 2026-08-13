namespace Cerneala.Language.Text;

internal readonly struct LinePosition : IEquatable<LinePosition>
{
    public LinePosition(int line, int character)
    {
        if (line < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line));
        }

        if (character < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(character));
        }

        Line = line;
        Character = character;
    }

    public int Line { get; }

    public int Character { get; }

    public bool Equals(LinePosition other) => Line == other.Line && Character == other.Character;

    public override bool Equals(object? obj) => obj is LinePosition other && Equals(other);

    public override int GetHashCode() => unchecked((Line * 397) ^ Character);
}
