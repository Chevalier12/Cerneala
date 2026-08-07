using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismHaldInterpolation
{
    Trilinear
}





internal sealed class PrismHaldLut
{
    private readonly Vector3[] values;

    public PrismHaldLut(
        int level,
        IReadOnlyList<Vector3> values)
    {
        if (level < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                "A Hald LUT level must be at least 2.");
        }
        ArgumentNullException.ThrowIfNull(values);

        Level = level;
        CubeSize = checked(level * level);
        HaldSide = checked(CubeSize * level);
        int expectedCount = checked(HaldSide * HaldSide);
        if (values.Count != expectedCount)
        {
            throw new ArgumentException(
                $"A level {level} Hald LUT requires {expectedCount} pixels.",
                nameof(values));
        }

        this.values = new Vector3[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            Vector3 value = values[index];
            if (!float.IsFinite(value.X) ||
                !float.IsFinite(value.Y) ||
                !float.IsFinite(value.Z) ||
                value.X is < 0 or > 1 ||
                value.Y is < 0 or > 1 ||
                value.Z is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    "Hald LUT colors must be finite linear RGB values in [0, 1].");
            }
            this.values[index] = value;
        }
    }

    public int Level { get; }

    public int CubeSize { get; }

    public int HaldSide { get; }

    public Vector3 Sample(
        Vector3 color,
        PrismHaldInterpolation interpolation)
    {
        Vector3 coordinate = Vector3.Clamp(
            color,
            Vector3.Zero,
            Vector3.One) * (CubeSize - 1);
        Vector3 baseCoordinate = new(
            MathF.Floor(coordinate.X),
            MathF.Floor(coordinate.Y),
            MathF.Floor(coordinate.Z));
        Vector3 fraction = coordinate - baseCoordinate;
        return SampleTrilinear(baseCoordinate, fraction);
    }

    internal static int GetHaldIndex(
        int cubeSize,
        int red,
        int green,
        int blue)
    {
        if (cubeSize < 2 ||
            red < 0 || red >= cubeSize ||
            green < 0 || green >= cubeSize ||
            blue < 0 || blue >= cubeSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cubeSize));
        }

        return red + (cubeSize * (green + (cubeSize * blue)));
    }

    private Vector3 SampleTrilinear(
        Vector3 baseCoordinate,
        Vector3 fraction)
    {
        Vector3 c000 = SamplePoint(baseCoordinate);
        Vector3 c100 = SamplePoint(baseCoordinate + Vector3.UnitX);
        Vector3 c010 = SamplePoint(baseCoordinate + Vector3.UnitY);
        Vector3 c110 = SamplePoint(
            baseCoordinate + Vector3.UnitX + Vector3.UnitY);
        Vector3 c001 = SamplePoint(baseCoordinate + Vector3.UnitZ);
        Vector3 c101 = SamplePoint(
            baseCoordinate + Vector3.UnitX + Vector3.UnitZ);
        Vector3 c011 = SamplePoint(
            baseCoordinate + Vector3.UnitY + Vector3.UnitZ);
        Vector3 c111 = SamplePoint(baseCoordinate + Vector3.One);
        Vector3 low = Vector3.Lerp(
            Vector3.Lerp(c000, c100, fraction.X),
            Vector3.Lerp(c010, c110, fraction.X),
            fraction.Y);
        Vector3 high = Vector3.Lerp(
            Vector3.Lerp(c001, c101, fraction.X),
            Vector3.Lerp(c011, c111, fraction.X),
            fraction.Y);
        return Vector3.Lerp(low, high, fraction.Z);
    }

    private Vector3 SamplePoint(Vector3 coordinate)
    {
        int red = Math.Clamp((int)coordinate.X, 0, CubeSize - 1);
        int green = Math.Clamp((int)coordinate.Y, 0, CubeSize - 1);
        int blue = Math.Clamp((int)coordinate.Z, 0, CubeSize - 1);
        int linearIndex = GetHaldIndex(CubeSize, red, green, blue);
        int x = linearIndex % HaldSide;
        int y = linearIndex / HaldSide;
        return values[(y * HaldSide) + x];
    }
}
