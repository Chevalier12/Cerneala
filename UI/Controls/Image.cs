using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class Image : Control
{
    public static readonly UiProperty<IDrawImage?> SourceProperty = UiProperty<IDrawImage?>.Register(
        nameof(Source),
        typeof(Image),
        new UiPropertyMetadata<IDrawImage?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ReferenceImageComparer.Instance));

    public IDrawImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return Source is null ? LayoutSize.Zero : new LayoutSize(Source.Width, Source.Height);
    }

    protected override void OnRender(RenderContext context)
    {
        if (Source is null || context.Bounds.Width <= 0 || context.Bounds.Height <= 0)
        {
            return;
        }

        context.DrawingContext.DrawImage(Source, Border.ToDrawRect(context.Bounds), Foreground);
    }

    private sealed class ReferenceImageComparer : IEqualityComparer<IDrawImage?>
    {
        public static readonly ReferenceImageComparer Instance = new();

        public bool Equals(IDrawImage? x, IDrawImage? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(IDrawImage? obj)
        {
            return obj is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
