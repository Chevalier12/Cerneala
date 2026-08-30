using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

internal static class ColorPickerTemplates
{
    public static readonly ComponentTemplate<ColorPicker> Default = new("ColorPicker.Default", context =>
    {
        ColorSpectrum spectrum = new()
        {
            Width = 220,
            Height = 140,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Slider hueSlider = CreateRampSlider();
        hueSlider.Margin = new Thickness(0, 0, 0, 8);
        Slider alphaSlider = CreateRampSlider();
        AlphaCheckerboard alphaCheckerboard = new();
        Grid alphaRamp = new()
        {
            Width = 220,
            Height = 16,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Add(alphaRamp, alphaCheckerboard);
        Add(alphaRamp, alphaSlider);
        Border preview = new()
        {
            Height = 22,
            BorderBrush = new SolidColorBrush(new Color(70, 76, 86)),
            BorderThickness = new Thickness(1)
        };

        StackPanel stack = new();
        Add(stack, spectrum);
        Add(stack, hueSlider);
        Add(stack, alphaRamp);
        Add(stack, preview);

        Border root = new() { Child = stack };
        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.TemplateOwnerBinding;
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, OwnerBindingSource);
        context.Bind(Control.PaddingProperty, root, Control.PaddingProperty, OwnerBindingSource);
        context.RequirePart("PART_Spectrum", spectrum);
        context.RequirePart("PART_HueSlider", hueSlider);
        context.RequirePart("PART_AlphaSlider", alphaSlider);
        context.RequirePart("PART_PreviewSwatch", preview);
        return root;
    });

    private static Slider CreateRampSlider()
    {
        Slider slider = new()
        {
            Width = 220,
            Height = 16,
            Cursor = Cerneala.UI.Input.Cursor.Hand
        };
        Track track = slider.Track;
        track.BorderBrush = new SolidColorBrush(new Color(70, 76, 86));
        track.BorderThickness = new Thickness(1);
        track.Thumb.Background = new SolidColorBrush(Color.White);
        track.Thumb.BorderBrush = new SolidColorBrush(Color.Black);
        track.Thumb.BorderThickness = new Thickness(1);
        return slider;
    }

    private sealed class AlphaCheckerboard : Control
    {
        protected override void OnRender(RenderContext context)
        {
            const float CheckerSize = 6;
            Color checkerLight = new(232, 234, 238);
            Color checkerDark = new(166, 171, 180);
            DrawRect bounds = Border.ToDrawRect(context.Bounds);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            context.DrawingContext.FillRectangle(bounds, checkerLight);
            int rows = (int)MathF.Ceiling(bounds.Height / CheckerSize);
            int columns = (int)MathF.Ceiling(bounds.Width / CheckerSize);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (((row + column) & 1) == 0)
                    {
                        continue;
                    }

                    float x = column * CheckerSize;
                    float y = row * CheckerSize;
                    context.DrawingContext.FillRectangle(
                        new DrawRect(
                            bounds.X + x,
                            bounds.Y + y,
                            MathF.Min(CheckerSize, bounds.Width - x),
                            MathF.Min(CheckerSize, bounds.Height - y)),
                        checkerDark);
                }
            }
        }
    }

    private static void Add(StackPanel panel, UIElement child)
    {
        panel.LogicalChildren.Add(child);
        panel.VisualChildren.Add(child);
    }

    private static void Add(Grid panel, UIElement child)
    {
        panel.LogicalChildren.Add(child);
        panel.VisualChildren.Add(child);
    }
}
