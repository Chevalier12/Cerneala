using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_Spectrum", typeof(ColorSpectrum))]
[TemplatePart("PART_HueSlider", typeof(Slider))]
[TemplatePart("PART_AlphaSlider", typeof(Slider))]
[TemplatePart("PART_PreviewSwatch", typeof(Border))]
public class ColorPicker : Control
{
    public static readonly RoutedEvent SelectedColorChangedEvent = RoutedEventRegistry.Register(
        nameof(SelectedColorChanged),
        typeof(ColorPicker),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventArgs<Color>));

    public static readonly UiProperty<Color> SelectedColorProperty = UiProperty<Color>.Register(
        nameof(SelectedColor),
        typeof(ColorPicker),
        new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics));

    private ColorSpectrum? spectrum;
    private Slider? hueSlider;
    private Slider? alphaSlider;
    private Border? previewSwatch;
    private bool synchronizingParts;
    private bool updatingColorFromParts;
    private float hue;
    private float saturation;
    private float value = 1;
    private float alpha = 1;
    private LinearGradientBrush? hueBrush;
    private float hueBrushWidth = -1;
    private LinearGradientBrush? alphaBrush;
    private float alphaBrushWidth = -1;
    private Color alphaBrushColor;

    public ColorPicker()
    {
        SetFrameworkDefault(BackgroundProperty, new SolidColorBrush(Color.White));
        SetFrameworkDefault(ForegroundProperty, new SolidColorBrush(Color.Black));
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(90, 98, 110)));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        SetFrameworkDefault(PaddingProperty, new Thickness(10));
        SetFrameworkDefault(ComponentTemplateProperty, ColorPickerTemplates.Default);
        ColorPickerColorMath.ToHsv(SelectedColor, out hue, out saturation, out value);
    }

    public event EventHandler<RoutedPropertyChangedEventArgs<Color>> SelectedColorChanged
    {
        add => AddTypedHandler(SelectedColorChangedEvent, value);
        remove => RemoveTypedHandler(SelectedColorChangedEvent, value);
    }

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public float Hue => hue;

    public float Saturation => saturation;

    public float Value => value;

    public float Alpha => alpha;

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        UpdateSliderBrushes();
        return arranged;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        DetachTemplateParts();
        if (instance is null)
        {
            return;
        }

        spectrum = GetRequiredTemplatePart<ColorSpectrum>("PART_Spectrum");
        hueSlider = GetRequiredTemplatePart<Slider>("PART_HueSlider");
        alphaSlider = GetRequiredTemplatePart<Slider>("PART_AlphaSlider");
        previewSwatch = GetRequiredTemplatePart<Border>("PART_PreviewSwatch");
        hueSlider.Minimum = 0;
        hueSlider.Maximum = 360;
        hueSlider.SmallChange = 1;
        hueSlider.LargeChange = 30;
        alphaSlider.Minimum = 0;
        alphaSlider.Maximum = 1;
        alphaSlider.SmallChange = 0.01f;
        alphaSlider.LargeChange = 0.1f;
        spectrum.ValueChanged += OnSpectrumValueChanged;
        hueSlider.ValueChanged += OnHueValueChanged;
        alphaSlider.ValueChanged += OnAlphaValueChanged;
        SynchronizeTemplateParts();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedColorProperty))
        {
            Color oldColor = (Color)args.OldValue!;
            if (!updatingColorFromParts)
            {
                ColorPickerColorMath.ToHsv(SelectedColor, out hue, out saturation, out value);
                alpha = SelectedColor.A / 255f;
            }

            SynchronizeTemplateParts();
            RaiseEvent(new RoutedPropertyChangedEventArgs<Color>(
                SelectedColorChangedEvent,
                this,
                oldColor,
                SelectedColor));
        }

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            HasFrameworkDefault(ComponentTemplateProperty))
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private void DetachTemplateParts()
    {
        if (spectrum is not null)
        {
            spectrum.ValueChanged -= OnSpectrumValueChanged;
        }

        if (hueSlider is not null)
        {
            hueSlider.ValueChanged -= OnHueValueChanged;
        }

        if (alphaSlider is not null)
        {
            alphaSlider.ValueChanged -= OnAlphaValueChanged;
        }

        spectrum = null;
        hueSlider = null;
        alphaSlider = null;
        previewSwatch = null;
        hueBrush = null;
        hueBrushWidth = -1;
        alphaBrush = null;
        alphaBrushWidth = -1;
    }

    private void OnSpectrumValueChanged(UiElementId _, RoutedEventArgs args)
    {
        if (synchronizingParts || spectrum is null)
        {
            return;
        }

        saturation = spectrum.Saturation;
        value = spectrum.Value;
        UpdateSelectedColorFromParts();
    }

    private void OnHueValueChanged(object? sender, RoutedPropertyChangedEventArgs<float> args)
    {
        if (synchronizingParts || !ReferenceEquals(sender, hueSlider))
        {
            return;
        }

        hue = args.NewValue;
        UpdateSelectedColorFromParts();
    }

    private void OnAlphaValueChanged(object? sender, RoutedPropertyChangedEventArgs<float> args)
    {
        if (synchronizingParts || !ReferenceEquals(sender, alphaSlider))
        {
            return;
        }

        alpha = args.NewValue;
        UpdateSelectedColorFromParts();
    }

    private void UpdateSelectedColorFromParts()
    {
        Color next = ColorPickerColorMath.FromHsv(
            hue,
            saturation,
            value,
            (byte)Math.Clamp(MathF.Round(alpha * 255), 0, 255));
        updatingColorFromParts = true;
        Color previous = SelectedColor;
        try
        {
            SelectedColor = next;
        }
        finally
        {
            updatingColorFromParts = false;
        }

        if (SelectedColor == previous)
        {
            SynchronizeTemplateParts();
        }
    }

    private void SynchronizeTemplateParts()
    {
        if (spectrum is null || hueSlider is null || alphaSlider is null || previewSwatch is null)
        {
            return;
        }

        synchronizingParts = true;
        try
        {
            spectrum.Hue = hue;
            spectrum.SetSelection(saturation, value);
            hueSlider.Value = hue;
            alphaSlider.Value = alpha;
            previewSwatch.Background = new SolidColorBrush(SelectedColor);
            UpdateSliderBrushes();
        }
        finally
        {
            synchronizingParts = false;
        }
    }

    private void UpdateSliderBrushes()
    {
        if (hueSlider is null || alphaSlider is null)
        {
            return;
        }

        Track hueTrack = hueSlider.Track;
        Track alphaTrack = alphaSlider.Track;
        float nextHueBrushWidth = MathF.Max(1, hueTrack.ArrangedBounds.Width);
        if (hueBrush is null || hueBrushWidth != nextHueBrushWidth)
        {
            hueBrushWidth = nextHueBrushWidth;
            hueBrush = CreateHueBrush(hueBrushWidth);
            hueTrack.Background = hueBrush;
        }

        Color selected = SelectedColor;
        float nextAlphaBrushWidth = MathF.Max(1, alphaTrack.ArrangedBounds.Width);
        if (alphaBrush is null || alphaBrushWidth != nextAlphaBrushWidth || alphaBrushColor != selected)
        {
            alphaBrushWidth = nextAlphaBrushWidth;
            alphaBrushColor = selected;
            alphaBrush = CreateAlphaBrush(alphaBrushWidth, selected);
            alphaTrack.Background = alphaBrush;
        }
    }

    private static LinearGradientBrush CreateHueBrush(float width)
    {
        DrawPoint start = new(0, 0);
        DrawPoint end = new(width, 0);
        return new LinearGradientBrush(start, end,
        [
            new GradientStop(0, Color.Red),
            new GradientStop(1f / 6, Color.Yellow),
            new GradientStop(2f / 6, Color.Lime),
            new GradientStop(3f / 6, Color.Cyan),
            new GradientStop(4f / 6, Color.Blue),
            new GradientStop(5f / 6, Color.Magenta),
            new GradientStop(1, Color.Red)
        ]);
    }

    private static LinearGradientBrush CreateAlphaBrush(float width, Color color)
    {
        DrawPoint start = new(0, 0);
        DrawPoint end = new(width, 0);
        return new LinearGradientBrush(start, end,
            [
                new GradientStop(0, new Color(color.R, color.G, color.B, 0)),
                new GradientStop(1, new Color(color.R, color.G, color.B, 255))
            ]);
    }
}
