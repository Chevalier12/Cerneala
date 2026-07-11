using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public abstract record Brush : IDrawBrush
{
    protected Brush(float opacity = 1)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Brush opacity must be between 0 and 1.");
        }

        Opacity = opacity;
    }

    public abstract DrawBrushKind Kind { get; }

    public float Opacity { get; }

    public virtual Color? SolidColor => null;

    DrawBrushDescriptor IDrawBrush.CreateDescriptor() => CreateDescriptor();

    protected abstract DrawBrushDescriptor CreateDescriptor();
}

internal static class GradientStopCollection
{
    public static IReadOnlyList<GradientStop> CreateOrdered(IEnumerable<GradientStop> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);

        GradientStop[] orderedStops = stops.OrderBy(stop => stop.Offset).ToArray();
        if (orderedStops.Length == 0)
        {
            throw new ArgumentException("A gradient brush requires at least one stop.", nameof(stops));
        }

        return Array.AsReadOnly(orderedStops);
    }

    public static IReadOnlyList<DrawGradientStop> ToDrawStops(IReadOnlyList<GradientStop> stops)
    {
        return Array.AsReadOnly(stops.Select(stop => new DrawGradientStop(stop.Offset, stop.Color)).ToArray());
    }

    public static bool SequenceEquals(IReadOnlyList<GradientStop> left, IReadOnlyList<GradientStop> right)
    {
        return left.Count == right.Count && left.SequenceEqual(right);
    }

    public static int GetSequenceHashCode(IReadOnlyList<GradientStop> stops)
    {
        HashCode hashCode = new();
        foreach (GradientStop stop in stops)
        {
            hashCode.Add(stop);
        }

        return hashCode.ToHashCode();
    }
}
