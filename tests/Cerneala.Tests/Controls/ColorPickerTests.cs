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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedColorHandlerThatReassignsTheColorLeavesChannelsAndPartsInSync(bool changeThroughAlphaSlider)
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = new Color(0, 255, 0, 255);
        picker.SelectedColorChanged += (_, args) =>
        {
            if (args.NewValue.A != 255)
            {
                picker.SelectedColor = new Color(args.NewValue.R, args.NewValue.G, args.NewValue.B, 255);
            }
        };

        if (changeThroughAlphaSlider)
        {
            Part<Slider>(picker, "PART_AlphaSlider").Value = 0.5f;
        }
        else
        {
            picker.SelectedColor = new Color(0, 255, 0, 128);
        }

        Assert.Equal(255, picker.SelectedColor.A);
        Assert.Equal(1f, picker.Alpha, 3);
        Assert.Equal(1f, Part<Slider>(picker, "PART_AlphaSlider").Value, 3);
    }

    // A handler that replaces a hue-slider color with dark blue (0, 0, 128) leaves the HSV channels, the hue slider and the
    // spectrum on that dark blue, and the picker raises exactly the two changes: the slider's green and the handler's blue.
    [Fact]
    public void SelectedColorHandlerThatReassignsTheColorDuringAHueChangeLeavesHsvAndPartsInSync()
    {
        ColorPicker picker = ArrangePicker();
        picker.SelectedColor = new Color(255, 0, 0, 255);
        Color darkBlue = new(0, 0, 128, 255);
        int changes = 0;
        picker.SelectedColorChanged += (_, args) =>
        {
            changes++;
            if (args.NewValue != darkBlue)
            {
                picker.SelectedColor = darkBlue;
            }
        };

        Part<Slider>(picker, "PART_HueSlider").Value = 120;

        Assert.Equal(darkBlue, picker.SelectedColor);
        Assert.Equal(2, changes);
        Assert.Equal(240, picker.Hue, 3);
        Assert.Equal(1, picker.Saturation, 3);
        Assert.Equal(128 / 255f, picker.Value, 3);
        Assert.Equal(240, Part<Slider>(picker, "PART_HueSlider").Value, 3);
        ColorSpectrum spectrum = Part<ColorSpectrum>(picker, "PART_Spectrum");
        Assert.Equal(240, spectrum.Hue, 3);
        Assert.Equal(1, spectrum.Saturation, 3);
        Assert.Equal(128 / 255f, spectrum.Value, 3);
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

        UIElement alphaRamp = Assert.IsAssignableFrom<UIElement>(alphaSlider.VisualParent);
        UIElement checkerboard = Assert.Single(alphaRamp.VisualChildren
            .Where(child => !ReferenceEquals(child, alphaSlider)));
        ElementRenderCache checkerboardCache = new();
        checkerboardCache.Ensure(checkerboard, new RenderCounters(), forceRebuild: true);
        DrawCommand[] checkerboardCommands = checkerboardCache.Commands.ToArray();
        Assert.True(checkerboardCommands.Length > 2);
        Assert.Equal(Border.ToDrawRect(checkerboard.ArrangedBounds), checkerboardCommands[0].Rect);
        Assert.Equal(new Color(232, 234, 238), checkerboardCommands[0].Color);
        Assert.All(checkerboardCommands, command =>
        {
            Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
            Assert.Null(command.Brush);
        });
        Assert.True(checkerboardCommands
            .Select(command => command.Color)
            .Distinct()
            .Count() >= 2);
    }

    // A color ramp draws only its gradient, its (70, 76, 86) border and its white thumb: no slider track bar under the
    // gradient, which would hide the checkerboard behind the transparent end of the alpha ramp.
    [Fact]
    public void ColorRampsDrawNoSliderTrackBarUnderTheirGradient()
    {
        ColorPicker picker = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = new(400, 400);
        root.VisualChildren.Add(picker);
        root.ProcessFrame();
        root.ProcessFrame();
        DrawCommand[] commands = root.RetainedRenderer.Commit(root).ToArray();

        foreach (string name in new[] { "PART_HueSlider", "PART_AlphaSlider" })
        {
            Slider slider = Part<Slider>(picker, name);
            LayoutRect track = slider.Track.ArrangedBounds;
            LayoutRect thumb = slider.Track.Thumb.ArrangedBounds;
            DrawCommand[] inside = commands
                .Where(command => command.Kind is DrawCommandKind.FillRectangle or DrawCommandKind.DrawRectangle)

                // The checkerboard behind the alpha ramp fills plain colors without a brush.
                .Where(command => command.Brush is not null)

                // The ramp's 1 pixel border (the default positive BorderThickness) is painted outside the track, as a
                // pen centred 0.5 pixel outside it, so the containment box is the track grown by 1 pixel on each side.
                .Where(command =>
                    command.Rect.X >= track.X - 1 && command.Rect.Y >= track.Y - 1 &&
                    command.Rect.X + command.Rect.Width <= track.X + track.Width + 1 &&
                    command.Rect.Y + command.Rect.Height <= track.Y + track.Height + 1)
                .ToArray();

            Assert.Equal(4, inside.Length);
            Assert.Equal(Border.ToDrawRect(track), inside[0].Rect);
            Assert.IsType<LinearGradientBrush>(inside[0].Brush);
            Assert.Equal(new Color(70, 76, 86), Assert.IsType<SolidColorBrush>(inside[1].Brush).Color);
            Assert.Equal(Border.ToDrawRect(thumb), inside[2].Rect);
            Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(inside[2].Brush).Color);
            Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(inside[3].Brush).Color);
        }
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
