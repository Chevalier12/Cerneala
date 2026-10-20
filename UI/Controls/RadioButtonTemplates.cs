using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

internal static class RadioButtonTemplates
{
    public static readonly ComponentTemplate<RadioButton> Default = new("RadioButton.Default", context =>
    {
        Ellipse ring = new();
        Ellipse dot = new()
        {
            Width = 6,
            Height = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Fill = new SolidColorBrush(Color.Black),
            Visibility = Visibility.Hidden
        };
        RoundIndicator indicator = new(ring, dot) { VerticalAlignment = VerticalAlignment.Center };
        ContentPresenter presenter = new() { Margin = new Thickness(6, 0, 0, 0) };
        StackPanel content = new() { Orientation = Orientation.Horizontal };
        Border root = new() { Child = content };

        content.VisualChildren.Add(indicator);
        content.VisualChildren.Add(presenter);

        context.RequirePart("PART_CheckMark", dot);
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, ring, Shape.StrokeProperty, UiPropertyValueSource.Local);
        context.Bind(Control.PaddingProperty, root, Control.PaddingProperty, UiPropertyValueSource.Local);
        context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
        context.Bind(Control.ForegroundProperty, presenter, Control.ForegroundProperty);
        context.Bind(Control.FontFamilyProperty, presenter, Control.FontFamilyProperty);
        context.Bind(Control.FontSizeProperty, presenter, Control.FontSizeProperty);

        return root;
    });

    // The ring and the dot share one cell of the grid, so the dot is drawn on top of the ring. RadioButton reaches the
    // ring through this class to give it the stroke thickness of BorderThickness, which a template binding cannot do
    // because the two properties have different types.
    internal sealed class RoundIndicator : Grid
    {
        private const float DefaultSide = 12;

        public RoundIndicator(Ellipse ring, Ellipse dot)
        {
            Ring = ring;
            VisualChildren.Add(ring);
            VisualChildren.Add(dot);
        }

        public Ellipse Ring { get; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            _ = base.MeasureCore(context);
            float side = MathF.Min(DefaultSide, MathF.Min(context.AvailableSize.Width, context.AvailableSize.Height));
            return new LayoutSize(side, side);
        }
    }
}
