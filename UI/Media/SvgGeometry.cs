using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record SvgGeometry : Geometry
{
    public SvgGeometry(string data, DrawRect viewBox)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        if (viewBox.Width <= 0 || viewBox.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewBox));
        }

        Data = data;
        Bounds = viewBox;
        Path = DrawPathParser.ParseSvg(data);
    }

    public string Data { get; }

    public DrawPath Path { get; }

    public override DrawRect Bounds { get; }

    public bool Equals(SvgGeometry? other)
    {
        return ReferenceEquals(this, other) ||
            (other is not null &&
             string.Equals(Data, other.Data, StringComparison.Ordinal) &&
             Bounds == other.Bounds);
    }

    public override int GetHashCode() => HashCode.Combine(Data, Bounds);
}
