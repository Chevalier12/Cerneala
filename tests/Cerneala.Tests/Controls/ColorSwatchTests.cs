using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ColorSwatchTests
{
    [Fact]
    public void DefaultTemplateProvidesButtonOverlayAndPicker()
    {
        ColorSwatch swatch = ArrangeSwatch();

        Assert.IsType<Button>(Part(swatch, "PART_SwatchButton"));
        Assert.IsType<Overlay>(Part(swatch, "PART_PickerOverlay"));
        Assert.Same(swatch.Picker, Part(swatch, "PART_ColorPicker"));
    }

    [Fact]
    public void ClickingSwatchOpensPickerOverlay()
    {
        ColorSwatch swatch = ArrangeSwatch();
        Button button = Part<Button>(swatch, "PART_SwatchButton");

        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

        Assert.True(swatch.IsPickerOpen);
        Assert.True(Part<Overlay>(swatch, "PART_PickerOverlay").IsOpen);
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

    private static ColorSwatch ArrangeSwatch()
    {
        ColorSwatch swatch = new();
        swatch.Measure(new MeasureContext(new LayoutSize(20, 20)));
        swatch.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        return swatch;
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
