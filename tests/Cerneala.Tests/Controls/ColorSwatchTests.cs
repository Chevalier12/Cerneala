using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ColorSwatchTests
{
    [Fact]
    public void ClosedSwatchDoesNotMaterializeColorPicker()
    {
        ColorSwatch swatch = ArrangeSwatch();

        Assert.Null(Part<Overlay>(swatch, "PART_PickerOverlay").Content);
    }

    [Fact]
    public void DefaultTemplateProvidesButtonAndOverlay()
    {
        ColorSwatch swatch = ArrangeSwatch();

        Assert.IsType<Button>(Part(swatch, "PART_SwatchButton"));
        Assert.IsType<Overlay>(Part(swatch, "PART_PickerOverlay"));
    }

    [Fact]
    public void ClickingSwatchOpensPickerOverlay()
    {
        ColorSwatch swatch = ArrangeSwatch();
        Button button = Part<Button>(swatch, "PART_SwatchButton");

        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

        Assert.True(swatch.IsPickerOpen);
        Overlay overlay = Part<Overlay>(swatch, "PART_PickerOverlay");
        Assert.True(overlay.IsOpen);
        Assert.Same(swatch.Picker, overlay.Content);
    }

    [Fact]
    public void ProgrammaticColorSynchronizesButtonAndPicker()
    {
        ColorSwatch swatch = ArrangeSwatch();
        Color color = new(12, 34, 56, 78);

        swatch.SelectedColor = color;

        Assert.Equal(color, swatch.Picker.SelectedColor);
        Assert.Equal(color, Assert.IsType<SolidColorBrush>(
            Part<Button>(swatch, "PART_SwatchButton").Background).Color);
    }

    [Fact]
    public void PickerChangeUpdatesSelectedColorAndRaisesOneChange()
    {
        ColorSwatch swatch = ArrangeSwatch();
        int changes = 0;
        swatch.SelectedColorChanged += (_, _) => changes++;
        Color color = new(120, 80, 40, 200);

        swatch.Picker.SelectedColor = color;

        Assert.Equal(color, swatch.SelectedColor);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PickerKeepsItsInstanceAndSettingsWhenTheTemplateIsReapplied(bool replaceTemplate)
    {
        ColorSwatch swatch = ArrangeSwatch();
        ColorPicker first = swatch.Picker;
        first.Padding = new Thickness(7);
        Overlay firstOverlay = Part<Overlay>(swatch, "PART_PickerOverlay");

        if (replaceTemplate)
        {
            ReplaceTemplate(swatch);
        }

        ColorPicker second = swatch.Picker;

        Overlay currentOverlay = Part<Overlay>(swatch, "PART_PickerOverlay");
        Assert.True(
            ReferenceEquals(first, second) && second.Padding == new Thickness(7) &&
                ReferenceEquals(currentOverlay.Content, second),
            $"replaceTemplate={replaceTemplate}: same instance={ReferenceEquals(first, second)}, " +
            $"Padding={second.Padding}, overlay content is the picker={ReferenceEquals(currentOverlay.Content, second)}.");
        if (replaceTemplate)
        {
            Assert.NotSame(firstOverlay, currentOverlay);
            Assert.Null(firstOverlay.Content);
        }
    }

    [Fact]
    public void PickerFromAReplacedTemplateStillDrivesSelectedColor()
    {
        ColorSwatch swatch = ArrangeSwatch();
        ColorPicker picker = swatch.Picker;
        ReplaceTemplate(swatch);
        int changes = 0;
        swatch.SelectedColorChanged += (_, _) => changes++;
        Color color = new(120, 80, 40, 200);

        picker.SelectedColor = color;

        Assert.Equal(color, swatch.SelectedColor);
        Assert.Equal(1, changes);
    }

    private static ColorSwatch ArrangeSwatch()
    {
        ColorSwatch swatch = new();
        swatch.Measure(new MeasureContext(new LayoutSize(20, 20)));
        swatch.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        return swatch;
    }

    private static void ReplaceTemplate(ColorSwatch swatch)
    {
        swatch.ComponentTemplate = new ComponentTemplate<ColorSwatch>("ColorSwatch.Custom", context =>
        {
            Button button = new();
            Overlay overlay = new() { PlacementTarget = button };
            Cerneala.UI.Layout.Panels.Grid root = new();
            root.Children.Add(button);
            root.Children.Add(overlay);
            context.RequirePart("PART_SwatchButton", button);
            context.RequirePart("PART_PickerOverlay", overlay);
            return root;
        });
        swatch.Measure(new MeasureContext(new LayoutSize(20, 20)));
        swatch.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
    }

    private static object Part(ColorSwatch swatch, string name)
    {
        return swatch.ComponentTemplateInstance!.Parts[name];
    }

    private static T Part<T>(ColorSwatch swatch, string name)
        where T : class
    {
        return Assert.IsType<T>(Part(swatch, name));
    }
}
