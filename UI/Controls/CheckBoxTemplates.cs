using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using CheckMarkPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.UI.Controls;

internal static class CheckBoxTemplates
{
    public static readonly ComponentTemplate<CheckBox> Default = new("CheckBox.Default", context =>
    {
        CheckMarkPath checkMark = new()
        {
            Data = new PathGeometry(
            [
                new DrawPoint(0, 4),
                new DrawPoint(2.5f, 6.5f),
                new DrawPoint(8, 0)
            ]),
            Stroke = new SolidColorBrush(Color.Black),
            StrokeThickness = 1,
            Visibility = Visibility.Hidden
        };
        SquareBorder indicator = new()
        {
            Child = checkMark,
            Padding = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center
        };
        ContentPresenter presenter = new() { Margin = new Thickness(6, 0, 0, 0) };
        StackPanel content = new() { Orientation = Orientation.Horizontal };
        Border root = new() { Child = content };

        content.VisualChildren.Add(indicator);
        content.VisualChildren.Add(presenter);

        context.RequirePart("PART_CheckMark", checkMark);
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, indicator, Control.BorderBrushProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderThicknessProperty, indicator, Control.BorderThicknessProperty, UiPropertyValueSource.Local);
        context.Bind(Control.PaddingProperty, root, Control.PaddingProperty, UiPropertyValueSource.Local);
        context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
        context.Bind(Control.ForegroundProperty, presenter, Control.ForegroundProperty);
        context.Bind(Control.FontFamilyProperty, presenter, Control.FontFamilyProperty);
        context.Bind(Control.FontSizeProperty, presenter, Control.FontSizeProperty);

        return root;
    });

    private sealed class SquareBorder : Border
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            LayoutSize measured = base.MeasureCore(context);
            float side = MathF.Max(measured.Width, measured.Height);
            return new LayoutSize(side, side);
        }
    }
}
