using System.Collections.ObjectModel;

namespace Cerneala.Drawing;

public enum DrawLineCap
{
    Flat,
    Square,
    Round,
    Triangle
}

public enum DrawLineJoin
{
    Miter,
    Bevel,
    Round
}

public enum DrawStrokeAlignment
{
    Inside,
    Center,
    Outside
}

public sealed class DrawStrokeStyle
{
    private static readonly IReadOnlyList<float> EmptyDashPattern =
        Array.AsReadOnly(Array.Empty<float>());

    public static DrawStrokeStyle Default { get; } = new();

    public DrawStrokeStyle(
        DrawLineCap startCap = DrawLineCap.Flat,
        DrawLineCap endCap = DrawLineCap.Flat,
        DrawLineJoin join = DrawLineJoin.Miter,
        float miterLimit = 10,
        IEnumerable<float>? dashPattern = null,
        float dashOffset = 0,
        DrawStrokeAlignment alignment = DrawStrokeAlignment.Center)
    {
        if (!Enum.IsDefined(startCap))
        {
            throw new ArgumentOutOfRangeException(nameof(startCap));
        }
        if (!Enum.IsDefined(endCap))
        {
            throw new ArgumentOutOfRangeException(nameof(endCap));
        }
        if (!Enum.IsDefined(join))
        {
            throw new ArgumentOutOfRangeException(nameof(join));
        }
        if (!float.IsFinite(miterLimit) || miterLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(miterLimit));
        }
        if (!float.IsFinite(dashOffset))
        {
            throw new ArgumentOutOfRangeException(nameof(dashOffset));
        }
        if (!Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        float[] dashes = dashPattern?.ToArray() ?? [];
        if (dashes.Any(value => !float.IsFinite(value) || value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(dashPattern));
        }

        StartCap = startCap;
        EndCap = endCap;
        Join = join;
        MiterLimit = miterLimit;
        DashPattern = dashes.Length == 0
            ? EmptyDashPattern
            : new ReadOnlyCollection<float>(dashes);
        DashOffset = dashOffset;
        Alignment = alignment;
    }

    public DrawLineCap StartCap { get; }
    public DrawLineCap EndCap { get; }
    public DrawLineJoin Join { get; }
    public float MiterLimit { get; }
    public IReadOnlyList<float> DashPattern { get; }
    public float DashOffset { get; }
    public DrawStrokeAlignment Alignment { get; }
}

public sealed record DrawPen
{
    public DrawPen(
        IDrawBrush brush,
        float thickness,
        DrawStrokeStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        Brush = brush;
        Thickness = thickness;
        Style = style ?? DrawStrokeStyle.Default;
    }

    public IDrawBrush Brush { get; }
    public float Thickness { get; }
    public DrawStrokeStyle Style { get; }
}
