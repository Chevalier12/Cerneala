using Cerneala.Drawing;
using Cerneala.UI.Media;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.UI.Controls.Primitives;

internal enum DirectionGlyphKind
{
    Up,
    Down,
    Left,
    Right
}

internal static class DirectionGlyphs
{
    private const string UpData = "M 384,871.92523 346.53984,908.36226 223.99999,789.16924 101.46016,908.36226 64,871.92523 l 37.42566,-36.40349 0.0344,0.0335 L 224,716.36226 l 122.53983,119.19302 0.0344,-0.0335 37.42568,36.4035 z";
    private const string DownData = "M 64,784.79925 L 101.46016,748.36222 L 224.00001,867.55524 L 346.53984,748.36222 L 384,784.79925 L 346.57434,821.20274 L 346.53994,821.16924 L 224,940.36222 L 101.46017,821.1692 L 101.42577,821.2027 L 64.00009,784.7992 z";
    private const string LeftData = "M 267.56299,668.36224 L 304.00002,705.8224 L 184.807,828.36225 L 304.00002,950.90208 L 267.56299,988.36224 L 231.1595,950.93658 L 231.193,950.90218 L 112.00002,828.36224 L 231.19304,705.82241 L 231.15954,705.78801 L 267.56304,668.36233 z";
    private const string RightData = "M 180.43701,988.36224 L 143.99998,950.90208 L 263.193,828.36223 L 143.99998,705.8224 L 180.43701,668.36224 L 216.8405,705.7879 L 216.807,705.8223 L 335.99998,828.36224 L 216.80696,950.90207 L 216.84046,950.93647 L 180.43696,988.36215 z";
    private static readonly DrawRect ViewBox = new(0, 604.36224f, 448, 448);
    private static readonly SvgGeometry UpGeometry = new(UpData, ViewBox);
    private static readonly SvgGeometry DownGeometry = new(DownData, ViewBox);
    private static readonly SvgGeometry LeftGeometry = new(LeftData, ViewBox);
    private static readonly SvgGeometry RightGeometry = new(RightData, ViewBox);

    public static DirectionPath Create(DirectionGlyphKind kind) => new()
    {
        Geometry = GetGeometry(kind)
    };

    public static SvgGeometry GetGeometry(DirectionGlyphKind kind) => kind switch
    {
        DirectionGlyphKind.Up => UpGeometry,
        DirectionGlyphKind.Down => DownGeometry,
        DirectionGlyphKind.Left => LeftGeometry,
        DirectionGlyphKind.Right => RightGeometry,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
