using System.Collections.Immutable;
using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal sealed class PrismCurveLut
{
    public const int SampleCount = 1024;

    private readonly Vector4[] values;

    private PrismCurveLut(Vector4[] values)
    {
        this.values = values;
    }

    public ReadOnlySpan<Vector4> Values => values;

    public static PrismCurveLut Create(
        PrismCurvesResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        PchipCurve composite = new(resource.Composite);
        PchipCurve red = new(resource.Red);
        PchipCurve green = new(resource.Green);
        PchipCurve blue = new(resource.Blue);
        Vector4[] values = new Vector4[SampleCount];
        for (int index = 0; index < values.Length; index++)
        {
            float input = index / (SampleCount - 1f);
            values[index] = new Vector4(
                Compose(red, composite, input),
                Compose(green, composite, input),
                Compose(blue, composite, input),
                1);
        }

        return new PrismCurveLut(values);
    }

    public Vector3 Sample(Vector3 color) =>
        new(
            SampleChannel(color.X, 0),
            SampleChannel(color.Y, 1),
            SampleChannel(color.Z, 2));

    private static float Compose(
        PchipCurve channel,
        PchipCurve composite,
        float input) =>
        Math.Clamp(
            composite.Evaluate(
                Math.Clamp(channel.Evaluate(input), 0, 1)),
            0,
            1);

    private float SampleChannel(float input, int channel)
    {
        float coordinate =
            Math.Clamp(input, 0, 1) * (SampleCount - 1);
        int lower = (int)coordinate;
        int upper = Math.Min(lower + 1, SampleCount - 1);
        float amount = coordinate - lower;
        return channel switch
        {
            0 => values[lower].X +
                ((values[upper].X - values[lower].X) * amount),
            1 => values[lower].Y +
                ((values[upper].Y - values[lower].Y) * amount),
            _ => values[lower].Z +
                ((values[upper].Z - values[lower].Z) * amount)
        };
    }

    private sealed class PchipCurve
    {
        private readonly ImmutableArray<PrismCurvePoint> points;
        private readonly float[] derivatives;

        public PchipCurve(
            ImmutableArray<PrismCurvePoint> points)
        {
            this.points = points;
            derivatives = FindDerivatives(points);
        }

        public float Evaluate(float input)
        {
            int segment = FindSegment(input);
            PrismCurvePoint left = points[segment];
            PrismCurvePoint right = points[segment + 1];
            float width = right.Input - left.Input;
            float position = (input - left.Input) / width;
            float squared = position * position;
            float cubed = squared * position;
            float leftBasis =
                (2 * cubed) - (3 * squared) + 1;
            float leftDerivativeBasis =
                cubed - (2 * squared) + position;
            float rightBasis =
                (-2 * cubed) + (3 * squared);
            float rightDerivativeBasis =
                cubed - squared;
            return
                (leftBasis * left.Output) +
                (leftDerivativeBasis * width *
                    derivatives[segment]) +
                (rightBasis * right.Output) +
                (rightDerivativeBasis * width *
                    derivatives[segment + 1]);
        }

        private int FindSegment(float input)
        {
            int low = 0;
            int high = points.Length - 2;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (points[middle].Input <= input)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            return low;
        }

        private static float[] FindDerivatives(
            ImmutableArray<PrismCurvePoint> points)
        {
            int count = points.Length;
            float[] derivatives = new float[count];
            float[] widths = new float[count - 1];
            float[] slopes = new float[count - 1];
            for (int index = 0; index < count - 1; index++)
            {
                widths[index] =
                    points[index + 1].Input -
                    points[index].Input;
                slopes[index] =
                    (points[index + 1].Output -
                        points[index].Output) /
                    widths[index];
            }

            if (count == 2)
            {
                derivatives[0] = slopes[0];
                derivatives[1] = slopes[0];
                return derivatives;
            }

            for (int index = 1; index < count - 1; index++)
            {
                float previous = slopes[index - 1];
                float next = slopes[index];
                if (previous == 0 ||
                    next == 0 ||
                    MathF.Sign(previous) != MathF.Sign(next))
                {
                    derivatives[index] = 0;
                    continue;
                }

                float firstWeight =
                    (2 * widths[index]) + widths[index - 1];
                float secondWeight =
                    widths[index] + (2 * widths[index - 1]);
                derivatives[index] =
                    (firstWeight + secondWeight) /
                    ((firstWeight / previous) +
                        (secondWeight / next));
            }

            derivatives[0] = EndpointDerivative(
                widths[0],
                widths[1],
                slopes[0],
                slopes[1]);
            derivatives[^1] = EndpointDerivative(
                widths[^1],
                widths[^2],
                slopes[^1],
                slopes[^2]);
            return derivatives;
        }

        private static float EndpointDerivative(
            float edgeWidth,
            float adjacentWidth,
            float edgeSlope,
            float adjacentSlope)
        {
            float derivative =
                (((2 * edgeWidth) + adjacentWidth) *
                    edgeSlope -
                    (edgeWidth * adjacentSlope)) /
                (edgeWidth + adjacentWidth);
            if (MathF.Sign(derivative) !=
                MathF.Sign(edgeSlope))
            {
                return 0;
            }
            if (MathF.Sign(edgeSlope) !=
                    MathF.Sign(adjacentSlope) &&
                MathF.Abs(derivative) >
                    MathF.Abs(3 * edgeSlope))
            {
                return 3 * edgeSlope;
            }
            return derivative;
        }
    }
}
