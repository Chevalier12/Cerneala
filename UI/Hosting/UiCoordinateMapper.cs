namespace Cerneala.UI.Hosting;

public static class UiCoordinateMapper
{
    public static float LogicalToPhysical(float logical, float scale)
    {
        ValidateCoordinate(logical, nameof(logical));
        ValidateScale(scale);
        return logical * scale;
    }

    public static float PhysicalToLogical(float physical, float scale)
    {
        ValidateCoordinate(physical, nameof(physical));
        ValidateScale(scale);
        return physical / scale;
    }

    public static int LogicalToPhysicalPixel(float logical, float scale)
    {
        return (int)MathF.Round(LogicalToPhysical(logical, scale), MidpointRounding.AwayFromZero);
    }

    internal static void ValidateScale(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Coordinate scale must be finite and greater than zero.");
        }
    }

    private static void ValidateCoordinate(float coordinate, string paramName)
    {
        if (!float.IsFinite(coordinate))
        {
            throw new ArgumentOutOfRangeException(paramName, "Coordinate must be finite.");
        }
    }
}
