using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework;

namespace Cerneala.Drawing.MonoGame;

internal readonly struct MonoGameDrawMapper
{
    public MonoGameDrawMapper(float coordinateScale)
    {
        UiCoordinateMapper.ValidateScale(coordinateScale);
        CoordinateScale = coordinateScale;
    }

    public float CoordinateScale { get; }

    public Rectangle MapRectangle(DrawRect rect)
    {
        int left = UiCoordinateMapper.LogicalToPhysicalPixel(rect.X, CoordinateScale);
        int top = UiCoordinateMapper.LogicalToPhysicalPixel(rect.Y, CoordinateScale);
        int right = UiCoordinateMapper.LogicalToPhysicalPixel(rect.X + rect.Width, CoordinateScale);
        int bottom = UiCoordinateMapper.LogicalToPhysicalPixel(rect.Y + rect.Height, CoordinateScale);

        return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public Vector2 MapVector(DrawPoint point)
    {
        return new Vector2(
            UiCoordinateMapper.LogicalToPhysical(point.X, CoordinateScale),
            UiCoordinateMapper.LogicalToPhysical(point.Y, CoordinateScale));
    }

    public int MapThickness(float thickness)
    {
        return Math.Max(1, UiCoordinateMapper.LogicalToPhysicalPixel(thickness, CoordinateScale));
    }

    public DrawTextRun MapTextRun(DrawTextRun textRun)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        return CoordinateScale == 1
            ? textRun
            : new DrawTextRun(textRun.Font, textRun.Text, UiCoordinateMapper.LogicalToPhysical(textRun.Size, CoordinateScale));
    }
}
