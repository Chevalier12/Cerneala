namespace Cerneala.Drawing.Paths;

internal static class DrawEllipseRowTessellator
{
    public static DrawPixelSpan[] Build(DrawRect bounds, float coordinateScale)
    {
        if (!float.IsFinite(coordinateScale) || coordinateScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinateScale));
        }

        int left = ToPhysicalPixel(bounds.X, coordinateScale);
        int top = ToPhysicalPixel(bounds.Y, coordinateScale);
        int right = ToPhysicalPixel(bounds.Right, coordinateScale);
        int bottom = ToPhysicalPixel(bounds.Bottom, coordinateScale);
        int width = Math.Max(0, right - left);
        int height = Math.Max(0, bottom - top);
        if (width == 0 || height == 0)
        {
            return [];
        }

        DrawPixelSpan[] rows = new DrawPixelSpan[height];
        float radiusX = width / 2f;
        float radiusY = height / 2f;
        float centerY = top + radiusY;
        for (int y = 0; y < height; y++)
        {
            float normalizedY = ((top + y + 0.5f) - centerY) / radiusY;
            float span = MathF.Sqrt(
                MathF.Max(0, 1 - (normalizedY * normalizedY))) * radiusX;
            int rowLeft = (int)MathF.Round(left + radiusX - span);
            int rowRight = (int)MathF.Round(left + radiusX + span);
            rows[y] = new DrawPixelSpan(
                rowLeft,
                top + y,
                Math.Max(1, rowRight - rowLeft));
        }
        return rows;
    }

    private static int ToPhysicalPixel(float logical, float scale) =>
        (int)MathF.Round(logical * scale, MidpointRounding.AwayFromZero);
}

internal readonly record struct DrawPixelSpan(int X, int Y, int Width);
