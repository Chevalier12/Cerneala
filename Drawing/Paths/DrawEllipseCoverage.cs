namespace Cerneala.Drawing.Paths;

internal static class DrawEllipseCoverage
{
    private const float CompensationPixels = 0.055f;

    public static DrawRect AdjustBounds(
        DrawRect bounds,
        float coordinateScale)
    {
        float compensation = CompensationPixels / coordinateScale;
        return new DrawRect(
            bounds.X - compensation,
            bounds.Y - compensation,
            bounds.Width + (compensation * 2),
            bounds.Height + (compensation * 2));
    }
}
