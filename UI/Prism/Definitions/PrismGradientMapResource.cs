using System.Collections.Immutable;
using System.Numerics;

namespace Cerneala.UI.Prism.Definitions;


public sealed class PrismGradientMapResource
{
    public PrismGradientMapResource(
        IEnumerable<PrismGradientMapPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        Points = points.ToImmutableArray();
        if (Points.Length < 2 ||
            Points[0].Offset != 0 ||
            Points[^1].Offset != 1)
        {
            throw new ArgumentException(
                "A gradient map must contain at least two points and span [0, 1].",
                nameof(points));
        }
        for (int index = 1; index < Points.Length; index++)
        {
            if (Points[index].Offset < Points[index - 1].Offset)
            {
                throw new ArgumentException(
                    "Gradient map offsets must be nondecreasing.",
                    nameof(points));
            }
        }
    }

    public ImmutableArray<PrismGradientMapPoint> Points { get; }
}


public readonly record struct PrismGradientMapPoint
{
    public PrismGradientMapPoint(float offset, Vector3 linearSrgb)
        : this(offset, linearSrgb, 1)
    {
    }

    public PrismGradientMapPoint(
        float offset,
        Vector3 linearSrgb,
        float alpha)
    {
        Offset = PrismDefinitionValidation.UnitInterval(offset, nameof(offset));
        if (!float.IsFinite(linearSrgb.X) ||
            !float.IsFinite(linearSrgb.Y) ||
            !float.IsFinite(linearSrgb.Z) ||
            linearSrgb.X is < 0 or > 1 ||
            linearSrgb.Y is < 0 or > 1 ||
            linearSrgb.Z is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(linearSrgb),
                "Gradient map colors must be finite linear-sRGB values in [0, 1].");
        }
        if (!float.IsFinite(alpha) || alpha is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alpha),
                "Gradient map alpha must be finite and in [0, 1].");
        }
        LinearSrgb = linearSrgb;
        Alpha = alpha;
    }

    public float Offset { get; }

    public Vector3 LinearSrgb { get; }

    public float Alpha { get; }
}
