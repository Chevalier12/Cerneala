using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal sealed class PrismGradientMapLut
{
    public const int SampleCount = 256;
    private readonly Vector3[] values;

    private PrismGradientMapLut(Vector3[] values) =>
        this.values = values;

    public ReadOnlySpan<Vector3> Values => values;

    public static PrismGradientMapLut Create(
        PrismGradientMapResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Vector3[] values = new Vector3[SampleCount];
        int segment = 0;
        for (int index = 0; index < values.Length; index++)
        {
            float coordinate = index / (SampleCount - 1f);
            while (segment + 1 < resource.Points.Length &&
                coordinate >= resource.Points[segment + 1].Offset)
            {
                segment++;
            }
            PrismGradientMapPoint left = resource.Points[segment];
            if (segment == resource.Points.Length - 1)
            {
                values[index] = left.LinearSrgb;
                continue;
            }
            PrismGradientMapPoint right = resource.Points[segment + 1];
            float amount = (coordinate - left.Offset) /
                (right.Offset - left.Offset);
            values[index] = Vector3.Lerp(
                left.LinearSrgb,
                right.LinearSrgb,
                Math.Clamp(amount, 0, 1));
        }
        return new PrismGradientMapLut(values);
    }

    public Vector3 Sample(float coordinate)
    {
        float scaled = Math.Clamp(coordinate, 0, 1) *
            (SampleCount - 1);
        int first = (int)MathF.Floor(scaled);
        int second = Math.Min(first + 1, SampleCount - 1);
        return Vector3.Lerp(values[first], values[second], scaled - first);
    }
}
