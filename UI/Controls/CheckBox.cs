using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using CheckMarkPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_CheckMark", typeof(CheckMarkPath))]
public class CheckBox : ToggleButton
{
    private const float CheckMarkInset = 1.5f;
    private static readonly Brush DefaultBorderBrush = new SolidColorBrush(new Color(100, 110, 125));

    public CheckBox()
    {
        SetValue(ComponentTemplateProperty, CheckBoxTemplates.Default, UiPropertyValueSource.AspectBase);
        SetValue(BorderBrushProperty, DefaultBorderBrush, UiPropertyValueSource.AspectBase);
        SetValue(BorderThicknessProperty, new Thickness(1), UiPropertyValueSource.AspectBase);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        SynchronizeCheckMark();
        return base.MeasureCore(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        SynchronizeCheckMark();
        StretchAndCenterCheckMark();
        return arranged;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsCheckedProperty))
        {
            SynchronizeCheckMark();
        }
    }

    private void SynchronizeCheckMark()
    {
        if (TryGetCheckMark(out CheckMarkPath checkMark))
        {
            checkMark.Visibility = IsChecked ? Visibility.Visible : Visibility.Hidden;
        }
    }

    private void StretchAndCenterCheckMark()
    {
        if (!TryGetCheckMark(out CheckMarkPath checkMark) ||
            checkMark.Data is not { } geometry)
        {
            return;
        }

        LayoutRect bounds = checkMark.ArrangedBounds;
        float availableWidth = MathF.Max(0, bounds.Width - (CheckMarkInset * 2));
        float availableHeight = MathF.Max(0, bounds.Height - (CheckMarkInset * 2));
        if (geometry.Bounds.Width <= 0 || geometry.Bounds.Height <= 0 || availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        float scale = MathF.Min(
            availableWidth / geometry.Bounds.Width,
            availableHeight / geometry.Bounds.Height);
        float scaledWidth = geometry.Bounds.Width * scale;
        float scaledHeight = geometry.Bounds.Height * scale;
        float x = bounds.X + ((bounds.Width - scaledWidth) / 2) - (geometry.Bounds.X * scale);
        float y = bounds.Y + ((bounds.Height - scaledHeight) / 2) - (geometry.Bounds.Y * scale);
        checkMark.RenderTransform = new Transform(new Matrix3x2(scale, 0, 0, scale, x, y));
    }

    private bool TryGetCheckMark(out CheckMarkPath checkMark)
    {
        if (ComponentTemplateInstance?.Parts.TryGetValue("PART_CheckMark", out UI.Elements.UIElement? element) == true &&
            element is CheckMarkPath path)
        {
            checkMark = path;
            return true;
        }

        checkMark = null!;
        return false;
    }
}
