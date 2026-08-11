using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class ColorPickerTests
{
    [Fact]
    public void DefaultTemplateProvidesSpectrumHueAlphaAndPreviewParts()
    {
        ColorPicker picker = ArrangePicker();

        Assert.IsType<ColorSpectrum>(Part(picker, "PART_Spectrum"));
        Assert.IsType<Slider>(Part(picker, "PART_HueSlider"));
        Assert.IsType<Slider>(Part(picker, "PART_AlphaSlider"));
        Assert.IsType<Border>(Part(picker, "PART_PreviewSwatch"));
    }

    [Fact]
    public void ProgrammaticColorSynchronizesAllPickerChannels()
    {
        ColorPicker picker = ArrangePicker();

        picker.SelectedColor = new Color(0, 255, 0, 128);

        Assert.Equal(120, picker.Hue, 3);
        Assert.Equal(1, picker.Saturation, 3);
        Assert.Equal(1, picker.Value, 3);
        Assert.Equal(128 / 255f, picker.Alpha, 3);
        Assert.Equal(120, Part<Slider>(picker, "PART_HueSlider").Value, 3);
        Assert.Equal(128 / 255f, Part<Slider>(picker, "PART_AlphaSlider").Value, 3);
    }

    [Fact]
    public void HueSliderUpdatesColorWithoutChangingAlpha()
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = new Color(255, 0, 0, 96);

        Part<Slider>(picker, "PART_HueSlider").Value = 120;

        Assert.Equal(new Color(0, 255, 0, 96), picker.SelectedColor);
    }

    [Fact]
    public void AlphaSliderUpdatesSelectedColorAlpha()
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = new Color(24, 48, 72, 255);

        Part<Slider>(picker, "PART_AlphaSlider").Value = 0.5f;

        Assert.Equal(new Color(24, 48, 72, 128), picker.SelectedColor);
    }

    [Fact]
    public void SpectrumSelectionUpdatesSelectedColorAndRaisesOneChange()
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = Color.Red;
        int changes = 0;
        picker.SelectedColorChanged += (_, _) => changes++;

        Part<ColorSpectrum>(picker, "PART_Spectrum").SetSelection(0.5f, 0.5f);

        Assert.Equal(new Color(128, 64, 64), picker.SelectedColor);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void SpectrumAndSliderGradientsUseControlLocalCoordinates()
    {
        ColorPicker picker = ArrangePicker();
        ColorSpectrum spectrum = Part<ColorSpectrum>(picker, "PART_Spectrum");
        ElementRenderCache cache = new();
        cache.Ensure(spectrum, new RenderCounters(), forceRebuild: true);

        LinearGradientBrush[] spectrumBrushes = cache.Commands
            .Where(command => command.Kind == DrawCommandKind.FillRectangle)
            .Select(command => Assert.IsType<LinearGradientBrush>(command.Brush))
            .ToArray();
        Assert.Equal(2, spectrumBrushes.Length);
        Assert.Equal(new DrawPoint(0, 0), spectrumBrushes[0].StartPoint);
        Assert.Equal(new DrawPoint(spectrum.ArrangedBounds.Width, 0), spectrumBrushes[0].EndPoint);
        Assert.Equal(new DrawPoint(0, 0), spectrumBrushes[1].StartPoint);
        Assert.Equal(new DrawPoint(0, spectrum.ArrangedBounds.Height), spectrumBrushes[1].EndPoint);

        Slider hueSlider = Part<Slider>(picker, "PART_HueSlider");
        LinearGradientBrush hueBrush = Assert.IsType<LinearGradientBrush>(hueSlider.Track.Background);
        Assert.Equal(new DrawPoint(0, 0), hueBrush.StartPoint);
        Assert.Equal(new DrawPoint(hueSlider.Track.ArrangedBounds.Width, 0), hueBrush.EndPoint);
    }

    [Fact]
    public void AlphaRampComposesTransparentColorGradientOverCheckerboard()
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = new Color(64, 128, 240);
        Slider alphaSlider = Part<Slider>(picker, "PART_AlphaSlider");

        LinearGradientBrush alphaGradient = Assert.IsType<LinearGradientBrush>(alphaSlider.Track.Background);
        Assert.Equal(0, alphaGradient.Stops[0].Color.A);
        Assert.Equal(255, alphaGradient.Stops[^1].Color.A);

        DrawingBrush checkerboard = Assert.Single(DescendantsAndSelf(picker)
            .OfType<Control>()
            .Select(control => control.Background)
            .OfType<DrawingBrush>());
        Assert.True(checkerboard.Commands.Count > 2);
        Assert.True(checkerboard.Commands
            .Select(command => command.Color)
            .Distinct()
            .Count() >= 2);
    }

    private static ColorPicker ArrangePicker()
    {
        ColorPicker picker = new();
        picker.Measure(new MeasureContext(new LayoutSize(260, 240)));
        picker.Arrange(new ArrangeContext(new LayoutRect(0, 0, 260, 240)));
        return picker;
    }

    private static object Part(ColorPicker picker, string name)
    {
        return picker.ComponentTemplateInstance!.Parts[name];
    }

    private static T Part<T>(ColorPicker picker, string name)
        where T : class
    {
        return Assert.IsType<T>(Part(picker, name));
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        yield return element;
        foreach (UIElement child in element.VisualChildren)
        {
            foreach (UIElement descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }
}
