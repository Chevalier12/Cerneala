using System.Numerics;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public class MouseEventArgs : RoutedEventArgs
{
    public MouseEventArgs(RoutedEvent routedEvent, object originalSource, int x, int y)
        : this(routedEvent, originalSource, (float)x, (float)y)
    {
    }

    internal MouseEventArgs(
        RoutedEvent routedEvent,
        object originalSource,
        float rootX,
        float rootY)
        : base(routedEvent, originalSource)
    {
        RootX = rootX;
        RootY = rootY;
        X = (int)MathF.Round(rootX);
        Y = (int)MathF.Round(rootY);
    }

    public int X { get; }

    public int Y { get; }

    internal float RootX { get; }

    internal float RootY { get; }

    public Vector2 GetPosition(UIElement relativeTo)
    {
        ArgumentNullException.ThrowIfNull(relativeTo);
        if (!InputCoordinateConverter.TryRootToElement(
            relativeTo,
            new Vector2(RootX, RootY),
            out Vector2 position))
        {
            throw new InvalidOperationException(
                "The input position cannot be converted through a non-invertible or detached element transform.");
        }

        return position;
    }
}
