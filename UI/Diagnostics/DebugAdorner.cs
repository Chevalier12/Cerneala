using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Diagnostics;

public sealed class DebugAdorner : Decorator
{
    public static readonly Cerneala.UI.Core.UiProperty<Color> AdornerColorProperty =
        Cerneala.UI.Core.UiProperty<Color>.Register(
            nameof(AdornerColor),
            typeof(DebugAdorner),
            new Cerneala.UI.Core.UiPropertyMetadata<Color>(new Color(255, 64, 64), Cerneala.UI.Core.UiPropertyOptions.AffectsRender));

    public Color AdornerColor
    {
        get => GetValue(AdornerColorProperty);
        set => SetValue(AdornerColorProperty, value);
    }

    protected override void OnRender(Cerneala.UI.Rendering.RenderContext context)
    {
        LayoutRect bounds = context.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        context.DrawingContext.DrawRectangle(
            new DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            AdornerColor,
            1);
    }
}
