using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class TextBlock : Control
{
    private TextMeasurer textMeasurer = TextMeasurer.Default;

    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(TextBlock),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            coerceValue: (_, value) => value ?? string.Empty));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public TextMeasurer TextMeasurer
    {
        get => textMeasurer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            textMeasurer = value;
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        TextMeasurement measurement = TextMeasurer.Measure(Text, FontFamily, FontSize);
        return new LayoutSize(measurement.Width, measurement.Height);
    }

    protected override void OnRender(RenderContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        ControlTextFont font = new(FontFamily, FontSize);
        DrawTextRun run = new(font, Text, FontSize);
        context.DrawingContext.DrawText(run, new DrawPoint(context.Bounds.X, context.Bounds.Y), Foreground);
    }
}
