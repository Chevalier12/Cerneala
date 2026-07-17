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
    private ResourceId<FontResource>? fontResourceId;

    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(TextBlock),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<TextWrapping> TextWrappingProperty = UiProperty<TextWrapping>.Register(
        nameof(TextWrapping),
        typeof(TextBlock),
        new UiPropertyMetadata<TextWrapping>(
            TextWrapping.NoWrap,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<TextTrimming> TextTrimmingProperty = UiProperty<TextTrimming>.Register(
        nameof(TextTrimming),
        typeof(TextBlock),
        new UiPropertyMetadata<TextTrimming>(
            TextTrimming.None,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
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

            resourceProvider = value;
            resourceTextLayoutCache.Clear();
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
        TextAspect aspect = CreateTextAspect();
        RecordFontResourceDependency();
        TextMeasureResult measurement = GetTextMeasurer().Measure(Text, aspect, context.AvailableSize.Width);
        lastMeasurement = measurement;
        SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(measurement.RenderIdentity));
        return measurement.Size;
    }

    protected override void OnRender(RenderContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        TextAspect aspect = CreateTextAspect();
        RecordFontResourceDependency();
        TextMeasureResult measurement = GetTextRenderer().Render(
            context.DrawingContext,
            Text,
            aspect,
            context.Bounds.Width,
            new DrawPoint(context.Bounds.X, context.Bounds.Y));

        if (lastMeasurement is null || lastMeasurement.CacheKey != measurement.CacheKey)
        {
            lastMeasurement = measurement;
            SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(measurement.RenderIdentity));
        }
    }

    private TextAspect CreateTextAspect()
    {
        return new TextAspect(
            FontFamily,
            FontSize,
            wrapping: TextWrapping,
            trimming: TextTrimming,
            foreground: Foreground,
            fontResourceId: FontResourceId);
    }

    private TextMeasurer GetTextMeasurer()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        if (FontResourceId is not null && provider is not null)
        {
            return new TextMeasurer(new FontResolver(provider), LineBreakService.Default, resourceTextLayoutCache);
        }

        return TextMeasurer;
    }

    private TextRenderer GetTextRenderer()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        if (FontResourceId is not null && provider is not null)
        {
            TextMeasurer measurer = new(new FontResolver(provider), LineBreakService.Default, resourceTextLayoutCache);
            return new TextRenderer(new FontResolver(provider), measurer);
        }

        return TextRenderer;
    }

    private void RecordFontResourceDependency()
    {
        if (FontResourceId is not ResourceId<FontResource> id)
        {
            return;
        }

        ResourceDependencyTracker? tracker = ResolveResourceDependencyTracker();
        tracker?.RecordDependency(
            this,
            id,
            InvalidationFlags.Measure | InvalidationFlags.Render,
            affectsIntrinsicSize: true);
        long version = tracker?.GetDependencyVersion(this) ??
            (ResolveResourceProvider() is ResourceStore store ? store.GetVersion(id) : 0);
        SetRenderDependencies(RenderDependencies
            .WithResourceIdentity(id.ToString())
            .WithResourceVersion(version));
    }

    private IResourceProvider? ResolveResourceProvider()
    {
        return ResourceProvider ?? Root?.ResourceProvider;
    }

    private ResourceDependencyTracker? ResolveResourceDependencyTracker()
    {
        return ResourceDependencyTracker ?? Root?.ResourceDependencyTracker;
    }
}
