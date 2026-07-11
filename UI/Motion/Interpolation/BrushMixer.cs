using Cerneala.Drawing;
using Cerneala.UI.Media;

namespace Cerneala.UI.Motion.Interpolation;

public sealed class BrushMixer : ValueMixer<Brush?>
{
    private readonly ColorMixer colorMixer = new();

    public override Brush? Mix(Brush? from, Brush? to, float progress)
    {
        if (progress <= 0)
        {
            return from;
        }

        if (progress >= 1)
        {
            return to;
        }

        return (from, to) switch
        {
            (SolidColorBrush left, SolidColorBrush right) => new SolidColorBrush(
                colorMixer.Mix(left.Color, right.Color, progress),
                Lerp(left.Opacity, right.Opacity, progress)),
            (LinearGradientBrush left, LinearGradientBrush right) when HasMatchingStops(left.Stops, right.Stops) =>
                new LinearGradientBrush(
                    MixPoint(left.StartPoint, right.StartPoint, progress),
                    MixPoint(left.EndPoint, right.EndPoint, progress),
                    MixStops(left.Stops, right.Stops, progress),
                    Lerp(left.Opacity, right.Opacity, progress)),
            (RadialGradientBrush left, RadialGradientBrush right) when HasMatchingStops(left.Stops, right.Stops) =>
                new RadialGradientBrush(
                    MixPoint(left.Center, right.Center, progress),
                    Lerp(left.RadiusX, right.RadiusX, progress),
                    Lerp(left.RadiusY, right.RadiusY, progress),
                    MixStops(left.Stops, right.Stops, progress),
                    Lerp(left.Opacity, right.Opacity, progress)),
            _ => from
        };
    }

    private static bool HasMatchingStops(IReadOnlyList<GradientStop> left, IReadOnlyList<GradientStop> right)
    {
        return left.Count == right.Count && left.Select(stop => stop.Offset).SequenceEqual(right.Select(stop => stop.Offset));
    }

    private IReadOnlyList<GradientStop> MixStops(IReadOnlyList<GradientStop> left, IReadOnlyList<GradientStop> right, float progress)
    {
        return left.Select((stop, index) => new GradientStop(stop.Offset, colorMixer.Mix(stop.Color, right[index].Color, progress))).ToArray();
    }

    private static DrawPoint MixPoint(DrawPoint left, DrawPoint right, float progress)
    {
        return new DrawPoint(Lerp(left.X, right.X, progress), Lerp(left.Y, right.Y, progress));
    }
}
