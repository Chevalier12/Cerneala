using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public abstract record Brush
{
    public virtual DrawColor? SolidColor => null;
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
