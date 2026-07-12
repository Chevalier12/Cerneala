using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using CheckMarkPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.UI.Controls;

internal static class CheckBoxTemplates
{
    internal const string DefaultCheckMarkData = "M88.04,30.319L75.124,17.401c-0.454-0.453-1.067-0.709-1.71-0.709c-0.642,0-1.256,0.256-1.709,0.709L37.392,51.714l-9.094-9.093c-0.945-0.944-2.474-0.944-3.419,0L11.96,55.539c-0.453,0.453-0.709,1.068-0.709,1.709c0,0.641,0.256,1.256,0.709,1.71L35.607,82.6c0.453,0.453,1.067,0.708,1.709,0.708c0.029,0,0.055-0.016,0.083-0.016c0.024,0,0.05,0.014,0.075,0.014c0.621,0,1.236-0.236,1.709-0.708L88.04,33.738C88.985,32.794,88.985,31.264,88.04,30.319z";

    public static readonly ComponentTemplate<CheckBox> Default = new("CheckBox.Default", context =>
    {
        CheckMarkPath checkMark = new()
        {
            Geometry = new SvgGeometry(DefaultCheckMarkData, new DrawRect(0, 0, 100, 100)),
            Fill = new SolidColorBrush(Color.Black),
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
        private const float DefaultSide = 12;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            _ = base.MeasureCore(context);
            float side = MathF.Min(DefaultSide, MathF.Min(context.AvailableSize.Width, context.AvailableSize.Height));
            return new LayoutSize(side, side);
        }
    }
}
