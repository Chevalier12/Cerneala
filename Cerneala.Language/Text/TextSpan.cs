namespace Cerneala.Language.Text;

internal readonly struct TextSpan : IEquatable<TextSpan>
{
    public TextSpan(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public bool Contains(int offset) => offset >= Start && offset < End;

    public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;

    public override bool Equals(object? obj) => obj is TextSpan other && Equals(other);

    public override int GetHashCode() => unchecked((Start * 397) ^ Length);

    public override string ToString() => "[" + Start + ".." + End + ")";
}
