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
    }

    public string Data { get; }

    public override DrawRect Bounds { get; }
}
