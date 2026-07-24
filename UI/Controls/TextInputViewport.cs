using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

internal sealed class TextInputViewport
{
    private static readonly TimeSpan CaretBlinkPeriod = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan CaretBlinkVisibleDuration = TimeSpan.FromMilliseconds(500);
    private readonly ITextInputHost host;
    private readonly TextEditor editor;
    private readonly TextCaretLayout caretLayout = TextCaretLayout.Default;
    private readonly TextLayoutCache resourceTextLayoutCache = new();
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;
    private IResourceProvider? resourceProvider;
    private ResourceId<FontResource>? fontResourceId;
    private float horizontalTextOffset;
    private TimeSpan caretBlinkClock;
    private TimeSpan caretBlinkAnchor;
    private bool caretBlinkVisible = true;

    public TextInputViewport(ITextInputHost host, TextEditor editor)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    private Control Control => host.Control;

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
            InvalidateTextMetrics("TextBox text measurer changed");
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
            Control.Invalidate(InvalidationFlags.Render, "TextBox text renderer changed");
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
            InvalidateTextMetrics("TextBox font resource id changed");
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
            InvalidateTextMetrics("TextBox resource provider changed");
        }
    }

    public LayoutSize Measure(MeasureContext context)
    {
        Thickness insets = host.Insets;
        LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
        TextAspect aspect = CreateTextAspect();
        TextMeasureResult result = GetTextMeasurer().Measure(host.DisplayText, aspect, available.Width);
        TextCaretVerticalMetrics caretMetrics = caretLayout.GetCaretVerticalMetrics(aspect, CreateFontResolver());
        float textEditingHeight = caretMetrics.OffsetY + caretMetrics.Height;
        LayoutSize contentSize = new(result.Size.Width, MathF.Max(result.Size.Height, textEditingHeight));
        return ContentControl.Inflate(contentSize, insets);
    }

    public void Render(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Control.Background is { } background && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        Thickness borderThickness = Control.BorderThickness;
        float thickness = MathF.Max(
            MathF.Max(borderThickness.Left, borderThickness.Top),
            MathF.Max(borderThickness.Right, borderThickness.Bottom));
        if (Control.BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }

        LayoutRect content = ContentControl.Deflate(context.Bounds, host.Insets);
        if (content.Width <= 0 || content.Height <= 0)
        {
            return;
        }

        DrawRect clip = Border.ToDrawRect(content);
        context.DrawingContext.PushClip(clip);

        DrawRect? selectionBounds = GetSelectionBounds(content);
        if (host.DisplayText.Length > 0)
        {
            DrawText(context, content, Control.Foreground);
        }

        if (!editor.Selection.IsEmpty && host.SelectionBackground.A != 0)
        {
            DrawSelection(context, selectionBounds);
        }

        if (host.DisplayText.Length > 0 && selectionBounds is DrawRect selectedTextClip)
        {
            context.DrawingContext.PushClip(selectedTextClip);
            DrawText(context, content, new SolidColorBrush(Color.White));
            context.DrawingContext.PopClip();
        }

        if (ShouldRenderCaret())
        {
            DrawCaret(context, content);
        }

        context.DrawingContext.PopClip();
    }

    public bool UpdateRenderTime(TimeSpan frameTime)
    {
        if (frameTime > TimeSpan.Zero)
        {
            caretBlinkClock += frameTime;
        }

        if (!IsCaretRenderEligible())
        {
            caretBlinkVisible = true;
            return false;
        }

        TimeSpan elapsed = caretBlinkClock - caretBlinkAnchor;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        long phaseTicks = elapsed.Ticks % CaretBlinkPeriod.Ticks;
        bool visible = phaseTicks < CaretBlinkVisibleDuration.Ticks;
        if (visible == caretBlinkVisible)
        {
            return false;
        }

        caretBlinkVisible = visible;
        Control.Invalidate(InvalidationFlags.Render, "TextBox caret blink phase changed");
        return true;
    }

    public void EnsureCaretVisible()
    {
        float contentWidth = MathF.Max(
            0,
            Control.ArrangedBounds.Width - host.Insets.Left - host.Insets.Right);
        if (contentWidth <= 0)
        {
            horizontalTextOffset = 0;
            return;
        }

        float caretX = GetCaretTextX(editor.Caret.Position);
        float oldOffset = horizontalTextOffset;
        if (caretX - horizontalTextOffset > contentWidth)
        {
            horizontalTextOffset = caretX - contentWidth;
        }
        else if (caretX < horizontalTextOffset)
        {
            horizontalTextOffset = caretX;
        }

        horizontalTextOffset = MathF.Max(0, horizontalTextOffset);
        if (Math.Abs(oldOffset - horizontalTextOffset) > float.Epsilon)
        {
            Control.IncrementRenderVersion();
            Control.Invalidate(InvalidationFlags.Render, "TextBox horizontal viewport changed");
        }
    }

    public int GetCaretIndexAtMouseX(float mouseX, LayoutRect content)
    {
        float textX = mouseX - content.X + horizontalTextOffset;
        return caretLayout.GetCaretIndexAtX(
            host.DisplayText,
            textX,
            CreateTextAspect(),
            CreateFontResolver());
    }

    public void ResetCaretBlink()
    {
        caretBlinkAnchor = caretBlinkClock;
        if (caretBlinkVisible)
        {
            return;
        }

        caretBlinkVisible = true;
        Control.Invalidate(InvalidationFlags.Render, "TextBox caret blink reset");
    }

    public void InvalidateTextMetrics(string reason)
    {
        Control.IncrementLayoutVersion();
        Control.IncrementRenderVersion();
        Control.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, reason);
    }

    private void DrawSelection(RenderContext context, DrawRect? bounds)
    {
        if (bounds is DrawRect rect)
        {
            context.DrawingContext.FillRectangle(rect, host.SelectionBackground);
        }
    }

    private DrawRect? GetSelectionBounds(LayoutRect content)
    {
        float start = content.X + GetCaretTextX(editor.Selection.Start) - horizontalTextOffset;
        float end = content.X + GetCaretTextX(editor.Selection.End) - horizontalTextOffset;
        float x = Math.Clamp(start, content.X, content.X + content.Width);
        float right = Math.Clamp(end, content.X, content.X + content.Width);
        if (right <= x)
        {
            return null;
        }

        DrawRect verticalBounds = GetCaretVerticalBounds(content);
        return new DrawRect(x, verticalBounds.Y, right - x, verticalBounds.Height);
    }

    private void DrawText(RenderContext context, LayoutRect content, Brush? foreground)
    {
        GetTextRenderer().Render(
            context.DrawingContext,
            host.DisplayText,
            CreateTextAspect(foreground),
            content.Width + horizontalTextOffset,
            new DrawPoint(content.X - horizontalTextOffset, content.Y));
    }

    private void DrawCaret(RenderContext context, LayoutRect content)
    {
        float x = content.X + GetCaretTextX(editor.Caret.Position) - horizontalTextOffset;
        x = Math.Clamp(x, content.X, content.X + content.Width);
        float caretWidth = 1 / (Control.Root?.Scale ?? 1);
        DrawRect verticalBounds = GetCaretVerticalBounds(content);
        context.DrawingContext.FillRectangle(
            new DrawRect(x, verticalBounds.Y, caretWidth, verticalBounds.Height),
            host.CaretBrush);
    }

    private DrawRect GetCaretVerticalBounds(LayoutRect content)
    {
        TextAspect aspect = CreateTextAspect();
        TextCaretVerticalMetrics metrics = caretLayout.GetCaretVerticalMetrics(aspect, CreateFontResolver());
        float offsetY = Math.Clamp(metrics.OffsetY, 0, MathF.Max(0, content.Height - 1));
        float availableHeight = MathF.Max(1, content.Height - offsetY);
        float height = MathF.Min(MathF.Max(1, metrics.Height), availableHeight);
        return new DrawRect(content.X, content.Y + offsetY, content.Width, height);
    }

    private bool ShouldRenderCaret()
    {
        return IsCaretRenderEligible() && caretBlinkVisible;
    }

    private bool IsCaretRenderEligible()
    {
        return Control.IsKeyboardFocused &&
            Control.IsEnabled &&
            UIElementVisibility.ParticipatesInRendering(Control) &&
            host.CaretBrush.Opacity > 0;
    }

    private float GetCaretTextX(int position)
    {
        return caretLayout.GetCaretX(
            host.DisplayText,
            position,
            CreateTextAspect(),
            CreateFontResolver());
    }

    private TextAspect CreateTextAspect()
    {
        return CreateTextAspect(Control.Foreground);
    }

    private TextAspect CreateTextAspect(Brush? foreground)
    {
        return new TextAspect(
            Control.FontFamily,
            Control.FontSize,
            foreground: foreground,
            fontResourceId: FontResourceId);
    }

    private TextMeasurer GetTextMeasurer()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        return FontResourceId is not null && provider is not null
            ? new TextMeasurer(CreateFontResolver(), LineBreakService.Default, resourceTextLayoutCache)
            : TextMeasurer;
    }

    private TextRenderer GetTextRenderer()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        if (FontResourceId is not null && provider is not null)
        {
            FontResolver resolver = CreateFontResolver();
            return new TextRenderer(
                resolver,
                new TextMeasurer(resolver, LineBreakService.Default, resourceTextLayoutCache));
        }

        return TextRenderer;
    }

    private FontResolver CreateFontResolver()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        return FontResourceId is not null && provider is not null
            ? new FontResolver(provider)
            : new FontResolver();
    }

    private IResourceProvider? ResolveResourceProvider()
    {
        return ResourceProvider ?? Control.Root?.ResourceProvider;
    }
}
