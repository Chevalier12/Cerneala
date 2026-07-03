using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public class TextBlock : Control
{
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;
    private TextMeasureResult? lastMeasurement;

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
            if (ReferenceEquals(textMeasurer, value))
            {
                return;
            }

            textMeasurer = value;
            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Text measurer changed");
        }
    }

    public TextRenderer TextRenderer
    {
        get => textRenderer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(textRenderer, value))
            {
                return;
            }

            textRenderer = value;
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Render, "Text renderer changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        TextMeasureResult measurement = TextMeasurer.Measure(Text, CreateTextStyle(), context.AvailableSize.Width);
        lastMeasurement = measurement;
        SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(measurement.CacheKey.ToString()));
        return measurement.Size;
    }

    protected override void OnRender(RenderContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        TextRunStyle style = CreateTextStyle();
        TextMeasureResult measurement = TextRenderer.Render(
            context.DrawingContext,
            Text,
            style,
            context.Bounds.Width,
            new DrawPoint(context.Bounds.X, context.Bounds.Y),
            Foreground);

        if (lastMeasurement is null || lastMeasurement.CacheKey != measurement.CacheKey)
        {
            lastMeasurement = measurement;
            SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(measurement.CacheKey.ToString()));
        }
    }

    private TextRunStyle CreateTextStyle()
    {
        return new TextRunStyle(FontFamily, FontSize, color: Foreground);
    }
}
