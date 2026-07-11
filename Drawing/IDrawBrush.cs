using System.ComponentModel;

namespace Cerneala.Drawing;

public enum DrawBrushKind
{
    SolidColor,
    LinearGradient,
    RadialGradient,
    Image,
    Drawing,
    Visual
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDrawBrush
{
    DrawBrushKind Kind { get; }

    float Opacity { get; }

    Color? SolidColor { get; }

    DrawBrushDescriptor CreateDescriptor();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract record DrawBrushDescriptor(float Opacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SolidDrawBrushDescriptor(Color Color, float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct DrawGradientStop(float Offset, Color Color);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record LinearGradientDrawBrushDescriptor(
    DrawPoint StartPoint,
    DrawPoint EndPoint,
    IReadOnlyList<DrawGradientStop> Stops,
    float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record RadialGradientDrawBrushDescriptor(
    DrawPoint Center,
    float RadiusX,
    float RadiusY,
    IReadOnlyList<DrawGradientStop> Stops,
    float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity);

public enum DrawBrushStretch
{
    None,
    Fill,
    Uniform,
    UniformToFill
}

public enum DrawBrushAlignmentX
{
    Left,
    Center,
    Right
}

public enum DrawBrushAlignmentY
{
    Top,
    Center,
    Bottom
}

public enum DrawTileMode
{
    None,
    Tile,
    FlipX,
    FlipY,
    FlipXY
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract record TileDrawBrushDescriptor(
    DrawBrushStretch Stretch,
    DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY,
    DrawRect? Viewport,
    DrawRect? Viewbox,
    DrawTileMode TileMode,
    float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ImageDrawBrushDescriptor(
    IDrawImage? Image,
    string? SourceIdentity,
    DrawBrushStretch Stretch,
    DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY,
    DrawRect? Viewport,
    DrawRect? Viewbox,
    DrawTileMode TileMode,
    float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record DrawingDrawBrushDescriptor(
    IReadOnlyList<DrawCommand> Commands,
    DrawRect ContentBounds,
    DrawBrushStretch Stretch,
    DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY,
    DrawRect? Viewport,
    DrawRect? Viewbox,
    DrawTileMode TileMode,
    float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity);

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record VisualDrawBrushDescriptor(
    object VisualIdentity,
    IReadOnlyList<DrawCommand> Commands,
    DrawRect ContentBounds,
    DrawBrushStretch Stretch,
    DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY,
    DrawRect? Viewport,
    DrawRect? Viewbox,
    DrawTileMode TileMode,
    float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity);
