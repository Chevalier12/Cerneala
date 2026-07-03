using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public class TextBlock : Control
{
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;
    private TextMeasureResult? lastMeasurement;
    private readonly TextLayoutCache resourceTextLayoutCache = new();
    private IResourceProvider? resourceProvider;
    private ResourceDependencyTracker? resourceDependencyTracker;
    private ResourceStore? subscribedStore;
    private ResourceId<FontResource>? fontResourceId;

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

    public ResourceId<FontResource>? FontResourceId
    {
        get => fontResourceId;
        set
        {
            if (fontResourceId == value)
            {
                return;
            }

            fontResourceId = value;
            resourceTextLayoutCache.Clear();
            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Font resource id changed");
        }
    }

    public IResourceProvider? ResourceProvider
    {
        get => resourceProvider;
        set
        {
            if (ReferenceEquals(resourceProvider, value))
            {
                return;
            }

            if (subscribedStore is not null)
            {
                subscribedStore.ResourceChanged -= OnResourceChanged;
                subscribedStore = null;
            }

            resourceProvider = value;
            if (value is ResourceStore store)
            {
                subscribedStore = store;
                subscribedStore.ResourceChanged += OnResourceChanged;
            }

            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Text resource provider changed");
        }
    }

    public ResourceDependencyTracker? ResourceDependencyTracker
    {
        get => resourceDependencyTracker;
        set => resourceDependencyTracker = value;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        TextRunStyle style = CreateTextStyle();
        RecordFontResourceDependency();
        TextMeasureResult measurement = GetTextMeasurer().Measure(Text, style, context.AvailableSize.Width);
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
        RecordFontResourceDependency();
        TextMeasureResult measurement = GetTextRenderer().Render(
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
        return new TextRunStyle(FontFamily, FontSize, color: Foreground, fontResourceId: FontResourceId);
    }

    private TextMeasurer GetTextMeasurer()
    {
        if (FontResourceId is not null && ResourceProvider is not null)
        {
            return new TextMeasurer(new FontResolver(ResourceProvider), LineBreakService.Default, resourceTextLayoutCache);
        }

        return TextMeasurer;
    }

    private TextRenderer GetTextRenderer()
    {
        if (FontResourceId is not null && ResourceProvider is not null)
        {
            TextMeasurer measurer = new(new FontResolver(ResourceProvider), LineBreakService.Default, resourceTextLayoutCache);
            return new TextRenderer(new FontResolver(ResourceProvider), measurer);
        }

        return TextRenderer;
    }

    private void RecordFontResourceDependency()
    {
        if (FontResourceId is not ResourceId<FontResource> id)
        {
            return;
        }

        ResourceDependencyTracker?.RecordDependency(this, id);
        long version = ResourceDependencyTracker?.GetDependencyVersion(this) ??
            (ResourceProvider is ResourceStore store ? store.GetVersion(id) : 0);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(id.ToString())
            .WithResourceVersion(version));
    }

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        if (FontResourceId is not ResourceId<FontResource> id || !args.Matches(id))
        {
            return;
        }

        ResourceDependencyTracker?.NotifyResourceChanged(args);
        resourceTextLayoutCache.Clear();
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Font resource changed");
    }
}
