using System.Text;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Documents;
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
    private TextParagraphLayout? lastMeasurement;
    private readonly TextLayoutCache resourceTextLayoutCache = new();
    private readonly InlineCollection inlines;
    private IResourceProvider? resourceProvider;
    private ResourceDependencyTracker? resourceDependencyTracker;
    private ResourceId<FontResource>? fontResourceId;
    // True while the Text property is being set from the inlines, so that the Text change does not rebuild the inlines.
    private bool syncingText;
    // True while the inlines are being rebuilt from the Text property, so that their changes do not set Text again.
    private bool syncingInlines;

    public TextBlock()
    {
        inlines = new InlineCollection(this);
    }

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

    public static readonly UiProperty<TextAlignment> TextAlignmentProperty = UiProperty<TextAlignment>.Register(
        nameof(TextAlignment),
        typeof(TextBlock),
        new UiPropertyMetadata<TextAlignment>(
            TextAlignment.Left,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<TextDecorations> TextDecorationsProperty = UiProperty<TextDecorations>.Register(
        nameof(TextDecorations),
        typeof(TextBlock),
        new UiPropertyMetadata<TextDecorations>(
            TextDecorations.None,
            UiPropertyOptions.AffectsRender,
            validateValue: TextDecorationsValidation.IsDefined));

    // NaN keeps the height of the line content.
    public static readonly UiProperty<float> LineHeightProperty = UiProperty<float>.Register(
        nameof(LineHeight),
        typeof(TextBlock),
        new UiPropertyMetadata<float>(
            float.NaN,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => float.IsNaN(value) || (float.IsFinite(value) && value > 0)));

    public static readonly UiProperty<LineStackingStrategy> LineStackingStrategyProperty = UiProperty<LineStackingStrategy>.Register(
        nameof(LineStackingStrategy),
        typeof(TextBlock),
        new UiPropertyMetadata<LineStackingStrategy>(
            LineStackingStrategy.MaxHeight,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    // 0 shows all lines.
    public static readonly UiProperty<int> MaxLinesProperty = UiProperty<int>.Register(
        nameof(MaxLines),
        typeof(TextBlock),
        new UiPropertyMetadata<int>(
            0,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => value >= 0));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    // The inline content. As in WPF, Text and Inlines are two views of the same content: setting Text replaces the
    // inlines with one Run, and a change of the inlines sets Text to their text.
    public InlineCollection Inlines => inlines;

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public TextDecorations TextDecorations
    {
        get => GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }

    public float LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public LineStackingStrategy LineStackingStrategy
    {
        get => GetValue(LineStackingStrategyProperty);
        set => SetValue(LineStackingStrategyProperty, value);
    }

    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
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
        TextParagraphLayout paragraph = GetTextMeasurer().MeasureParagraph(
            CreateTextSpans(aspect),
            aspect,
            CreateParagraphOptions(),
            context.AvailableSize);
        lastMeasurement = paragraph;
        SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(paragraph.RenderIdentity));
        return paragraph.Size;
    }

    protected override void OnRender(RenderContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        TextAspect aspect = CreateTextAspect();
        RecordFontResourceDependency();
        TextParagraphLayout paragraph = GetTextRenderer().RenderParagraph(
            context.DrawingContext,
            CreateTextSpans(aspect),
            aspect,
            CreateParagraphOptions(),
            new LayoutSize(context.Bounds.Width, context.Bounds.Height),
            new DrawPoint(context.Bounds.X, context.Bounds.Y));

        if (lastMeasurement is null || lastMeasurement.RenderIdentity != paragraph.RenderIdentity)
        {
            lastMeasurement = paragraph;
            SetRenderDependencies(RenderDependencies.WithTextLayoutIdentity(paragraph.RenderIdentity));
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, TextProperty) && !syncingText)
        {
            ReplaceInlinesWithText();
        }
    }

    // Called by the inline collections of this TextBlock's inline tree when an inline is added, replaced or removed.
    internal void OnInlinesChanged()
    {
        if (syncingInlines)
        {
            return;
        }

        SyncTextFromInlines();
        InvalidateInlines(UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender, content: true);
    }

    // Called by an inline of this TextBlock's inline tree when one of its properties changes.
    internal void OnInlinePropertyChanged(UiProperty property, UiPropertyOptions options)
    {
        bool content = ReferenceEquals(property, Run.TextProperty);
        if (content)
        {
            SyncTextFromInlines();
        }

        InvalidateInlines(options, content);
    }

    private void ReplaceInlinesWithText()
    {
        syncingInlines = true;
        try
        {
            inlines.Clear();
            if (Text.Length > 0)
            {
                inlines.Add(new Run(Text));
            }
        }
        finally
        {
            syncingInlines = false;
        }
    }

    private void SyncTextFromInlines()
    {
        StringBuilder builder = new();
        foreach (Inline inline in inlines)
        {
            inline.AppendText(builder);
        }

        string text = builder.ToString();
        if (text == Text)
        {
            return;
        }

        syncingText = true;
        try
        {
            Text = text;
        }
        finally
        {
            syncingText = false;
        }
    }

    private void InvalidateInlines(UiPropertyOptions options, bool content)
    {
        InvalidationFlags flags = InvalidationFlags.None;
        if ((options & (UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange)) != UiPropertyOptions.None)
        {
            IncrementLayoutVersion();
            flags |= InvalidationFlags.Measure;
        }

        if ((options & UiPropertyOptions.AffectsRender) != UiPropertyOptions.None)
        {
            IncrementRenderVersion();
            flags |= InvalidationFlags.Render;
        }

        if (content)
        {
            flags |= InvalidationFlags.Semantics;
        }

        if (flags != InvalidationFlags.None)
        {
            Invalidate(flags, "Inlines changed");
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
            fontResourceId: FontResourceId,
            fontWeight: FontWeight,
            fontStyle: FontStyle,
            fontStretch: FontStretch);
    }

    private TextParagraphOptions CreateParagraphOptions()
    {
        return new TextParagraphOptions(TextAlignment, MaxLines, LineHeight, LineStackingStrategy);
    }

    // One span per piece of text of the inline tree, depth first, with the style that the inlines give it. With no text,
    // one empty span with the style of the TextBlock, so that an empty TextBlock still measures one line.
    private List<TextSpan> CreateTextSpans(TextAspect blockAspect)
    {
        InlineStyle blockStyle = new(
            FontFamily,
            FontSize,
            FontWeight,
            FontStyle,
            FontStretch,
            Foreground,
            TextDecorations);
        List<(string Text, InlineStyle Style)> pieces = [];
        foreach (Inline inline in inlines)
        {
            inline.CollectPieces(blockStyle, pieces);
        }

        List<TextSpan> spans = new(pieces.Count);
        foreach ((string text, InlineStyle style) in pieces)
        {
            if (text.Length == 0)
            {
                continue;
            }

            TextAspect spanAspect = style == blockStyle
                ? blockAspect
                : new TextAspect(
                    style.FontFamily,
                    style.FontSize,
                    wrapping: blockAspect.Wrapping,
                    trimming: blockAspect.Trimming,
                    scale: blockAspect.Scale,
                    foreground: style.Foreground,
                    fontResourceId: blockAspect.FontResourceId,
                    fontWeight: style.FontWeight,
                    fontStyle: style.FontStyle,
                    fontStretch: style.FontStretch);
            spans.Add(new TextSpan(text, spanAspect, style.Decorations));
        }

        if (spans.Count == 0)
        {
            spans.Add(new TextSpan(string.Empty, blockAspect));
        }

        return spans;
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
