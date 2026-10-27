using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;
using System.Globalization;

namespace Cerneala.UI.Controls;

// The text of a TextBox or PasswordBox: the content of the PART_ContentHost ScrollViewer in their templates, as WPF's
// TextBoxView. It draws only the text, the selection and the caret; the template draws the background and the border,
// and the ScrollViewer scrolls and clips it. Its positions are display indices into the host's DisplayText.
internal sealed class TextInputView : UIElement
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
    private TimeSpan caretBlinkClock;
    private TimeSpan caretBlinkAnchor;
    private bool caretBlinkVisible = true;
    private int metricsVersion;
    private TextInputLineLayout? recentLayout;
    private TextInputLineLayout? olderLayout;
    private bool hasArranged;
    private LayoutPoint arrangedScrollOffset;
    private float arrangedWrapWidth = float.PositiveInfinity;
    private bool layoutPending = true;
    private PendingReveal pendingReveal;
    private int pendingRevealLine;

    public TextInputView(ITextInputHost host, TextEditor editor)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    private enum PendingReveal
    {
        None,
        Caret,
        Line
    }

    private Control Control => host.Control;

    // Whether the caret at the end of a soft-wrapped line shows at the end of that line rather than at the start of the
    // next one, which has the same text position. End and a click right of a wrapped line's end set it.
    public bool CaretAtLineEnd { get; set; }

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
            InvalidateTextRender("TextBox text renderer changed");
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
            InvalidateLineHeight("TextBox font resource id changed");
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
            InvalidateLineHeight("TextBox resource provider changed");
        }
    }

    public float LineHeight => GetLineHeight(CreateTextAspect());

    // The lines at the width of the last arrange; before the first arrange the text does not wrap.
    public int LineCount => CurrentLayout.Lines.Length;

    private TextInputLineLayout CurrentLayout => GetLayout(hasArranged ? arrangedWrapWidth : float.PositiveInfinity);

    private ScrollContentPresenter? Presenter => VisualParent as ScrollContentPresenter;

    public string GetLineText(int lineIndex)
    {
        TextInputLineLayout layout = CurrentLayout;
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lineIndex, layout.Lines.Length);
        TextInputLine line = layout.Lines[lineIndex];
        return layout.Text[line.Start..line.NextStart];
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
        InvalidateTextRender("TextBox caret blink phase changed");
        return true;
    }

    public void ResetCaretBlink()
    {
        caretBlinkAnchor = caretBlinkClock;
        if (caretBlinkVisible)
        {
            return;
        }

        caretBlinkVisible = true;
        InvalidateTextRender("TextBox caret blink reset");
    }

    // Text, font or wrapping changed: the lines, the size and the drawing of this view. The owner is not measured again
    // for that: a change of this view's desired size reaches its ancestors through the layout queue, as for any element,
    // and the owner's own measure inputs (MinLines, MaxLines, the font properties, the padding and the border) are
    // properties that affect its measure. A plain edit therefore measures only this view and the ancestors whose size
    // it changes, as WPF's TextBoxView does.
    public void InvalidateTextMetrics(string reason)
    {
        metricsVersion++;
        layoutPending = true;
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, reason);
    }

    // The font resource and the resource provider set the line height, which TextBox.MeasureCore reads for MinLines and
    // MaxLines, and are not properties of the owner, so the owner measures again here.
    private void InvalidateLineHeight(string reason)
    {
        Control.IncrementLayoutVersion();
        Control.Invalidate(InvalidationFlags.Measure, reason);
    }

    // The caret, the selection, the brushes or the blink phase changed: only the drawing.
    public void InvalidateTextRender(string reason)
    {
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, reason);
    }

    // Scrolls the inner ScrollContentPresenter by the least amount that shows the caret, as WPF's TextBox does after an
    // edit or a caret move; it does not raise RequestBringIntoView. While the lines or the extent are not laid out yet,
    // the owner completes the reveal at its next arrange (CompleteLayout).
    public void RevealCaret()
    {
        RequestReveal(PendingReveal.Caret, 0);
    }

    public void ScrollToLine(int lineIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lineIndex, CurrentLayout.Lines.Length);
        RequestReveal(PendingReveal.Line, lineIndex);
    }

    // The owner moves this view to another ScrollViewer, after a template change. The last arrange was in the old
    // presenter, so a reveal waits for the arrange in the new one (CompleteLayout) instead of scrolling a presenter that
    // has no viewport or extent yet, and the lines are those of the unwrapped text until then, as before the first arrange.
    public void DetachFromPresenter()
    {
        hasArranged = false;
        layoutPending = true;
    }

    // Runs at the end of the owner's arrange, after the template, so this view's lines, the presenter's extent and the
    // places of the last arrange are current. An offset set here applies in the presenter's next arrange.
    public void CompleteLayout()
    {
        layoutPending = false;
        PendingReveal reveal = pendingReveal;
        pendingReveal = PendingReveal.None;
        if (reveal != PendingReveal.None && Presenter is ScrollContentPresenter presenter && hasArranged)
        {
            Reveal(presenter, reveal, pendingRevealLine);
        }
    }

    public int GetDisplayIndexAtRootPoint(float rootX, float rootY, int fallback, out bool atLineEnd)
    {
        atLineEnd = false;
        LayoutPoint? point = GetContentPointAtRootPoint(rootX, rootY);
        if (point is not LayoutPoint contentPoint)
        {
            return fallback;
        }

        TextInputLineLayout layout = CurrentLayout;
        int lineIndex = GetLineIndexAtContentY(layout, contentPoint.Y);
        return GetDisplayIndexAtX(layout, lineIndex, contentPoint.X, out atLineEnd);
    }

    // The display index of the character drawn under a root point, for the word and line selection of a double or
    // triple click: the text element whose horizontal extent on the hit line contains the point; at or after the end of
    // the line's text, the last character before the line end. An empty line gives its start.
    public int GetCharacterIndexAtRootPoint(float rootX, float rootY, int fallback)
    {
        LayoutPoint? point = GetContentPointAtRootPoint(rootX, rootY);
        if (point is not LayoutPoint contentPoint)
        {
            return fallback;
        }

        TextInputLineLayout layout = CurrentLayout;
        int lineIndex = GetLineIndexAtContentY(layout, contentPoint.Y);
        TextInputLine line = layout.Lines[lineIndex];
        // The caret position nearest the point is a boundary of the character under it: the point is in the character
        // that ends at that position when it lies left of it, or when the position is the line end.
        int caretIndex = GetDisplayIndexAtX(layout, lineIndex, contentPoint.X, out _);
        bool inPreviousCharacter = caretIndex > line.Start &&
            (caretIndex >= line.CaretEnd || contentPoint.X < GetCaretX(line, caretIndex));
        if (!inPreviousCharacter)
        {
            return caretIndex;
        }

        int position = caretIndex - line.Start;
        int previousStart = 0;
        foreach (int start in StringInfo.ParseCombiningCharacters(line.CaretText))
        {
            if (start >= position)
            {
                break;
            }

            previousStart = start;
        }

        return line.Start + previousStart;
    }

    public int GetLineIndex(int displayIndex, bool atLineEnd)
    {
        return GetLineIndex(CurrentLayout, displayIndex, atLineEnd);
    }

    public int GetLineStart(int lineIndex) => CurrentLayout.Lines[lineIndex].Start;

    // The caret position at the end of the line: before its line break, or after its trailing spaces when it wraps.
    public int GetLineEnd(int lineIndex, out bool atLineEnd)
    {
        TextInputLineLayout layout = CurrentLayout;
        TextInputLine line = layout.Lines[lineIndex];
        atLineEnd = line.IsSoftWrapped;
        return line.CaretEnd;
    }

    public float GetCaretX(int displayIndex, bool atLineEnd)
    {
        TextInputLineLayout layout = CurrentLayout;
        int lineIndex = GetLineIndex(layout, displayIndex, atLineEnd);
        return GetCaretX(layout.Lines[lineIndex], displayIndex);
    }

    public int GetDisplayIndexAtLineX(int lineIndex, float x, out bool atLineEnd)
    {
        return GetDisplayIndexAtX(CurrentLayout, lineIndex, x, out atLineEnd);
    }

    public int GetLinesPerPage()
    {
        float lineHeight = GetLineHeight(CreateTextAspect());
        float viewportHeight = Presenter?.ViewportHeight ?? ArrangedBounds.Height;
        return lineHeight > 0 ? Math.Max(1, (int)MathF.Floor(viewportHeight / lineHeight)) : 1;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        TextInputLineLayout layout = GetLayout(GetWrapWidth(context.AvailableSize.Width));
        TextAspect aspect = CreateTextAspect();
        TextCaretVerticalMetrics caretMetrics = caretLayout.GetCaretVerticalMetrics(aspect, CreateFontResolver());
        float lineHeight = GetLineHeight(aspect);
        int lineCount = layout.Lines.Length;
        float height = MathF.Max(
            lineCount * lineHeight,
            ((lineCount - 1) * lineHeight) + caretMetrics.OffsetY + caretMetrics.Height);
        return new LayoutSize(layout.Width + CaretWidth, height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (Presenter is ScrollContentPresenter presenter)
        {
            // The text is drawn at the exact scrolled origin, not at the rounded bounds (GetTextOrigin). A scroll by a
            // part of a pixel can leave the rounded bounds unchanged, or move them by another amount than the text, so
            // the retained drawing cannot be reused or moved with the bounds: it is drawn again.
            LayoutPoint offset = new(presenter.HorizontalOffset, presenter.VerticalOffset);
            if (hasArranged && offset != arrangedScrollOffset)
            {
                InvalidateTextRender("TextBox scroll offset changed");
            }

            arrangedScrollOffset = offset;
        }

        // The text wraps at the viewport width, as in the measure: a line that WrapWithOverflow keeps wider makes the
        // presenter arrange this view wider than the viewport, and that width must not join the other lines again.
        float wrapWidth = GetWrapWidth(Presenter?.ViewportWidth ?? context.FinalRect.Width);
        if (hasArranged && wrapWidth != arrangedWrapWidth)
        {
            // Such a line can keep these bounds at another viewport width, and only new bounds draw the view again on
            // their own, so the lines that wrap at the new width are drawn here.
            InvalidateTextRender("TextBox wrap width changed");
        }

        hasArranged = true;
        arrangedWrapWidth = wrapWidth;
        return context.FinalRect;
    }

    // Where the text starts: the unrounded place of the last arrange, which is the presenter's content origin minus its
    // scroll offset. The bounds are rounded to device pixels, and a fractional scroll offset would move the text by the
    // rounding, away from the shaped caret positions that the hit test and the reveal use.
    private LayoutPoint GetTextOrigin(LayoutRect bounds)
    {
        return hasArranged ? UnroundedArrangedLocation : new LayoutPoint(bounds.X, bounds.Y);
    }

    protected override void OnRender(RenderContext context)
    {
        LayoutRect bounds = context.Bounds;
        LayoutRect visible = GetVisibleBounds(bounds);
        if (visible.Width <= 0 || visible.Height <= 0)
        {
            return;
        }

        TextInputLineLayout layout = CurrentLayout;
        TextAspect aspect = CreateTextAspect();
        TextCaretVerticalMetrics caretMetrics = caretLayout.GetCaretVerticalMetrics(aspect, CreateFontResolver());
        float lineHeight = GetLineHeight(aspect);
        LayoutPoint origin = GetTextOrigin(bounds);
        float firstLineTop = origin.Y + GetFirstLineOffset(layout, bounds.Height, lineHeight, caretMetrics);
        float visibleBottom = visible.Y + visible.Height;
        (int first, int last) = GetVisibleLineRange(layout, firstLineTop, lineHeight, visible.Y, visibleBottom);

        for (int i = first; i <= last; i++)
        {
            TextInputLine line = layout.Lines[i];
            if (line.Text.Length > 0)
            {
                DrawLineText(context, line, origin.X, firstLineTop + (i * lineHeight), Control.Foreground);
            }
        }

        if (!editor.Selection.IsEmpty)
        {
            List<(int Line, DrawRect Rect)> selection = GetSelectionRects(
                layout,
                origin.X,
                firstLineTop,
                lineHeight,
                visible,
                caretMetrics,
                first,
                last);
            if (host.SelectionBackground.A != 0)
            {
                foreach ((_, DrawRect rect) in selection)
                {
                    context.DrawingContext.FillRectangle(rect, host.SelectionBackground);
                }
            }

            SolidColorBrush selectedTextBrush = new(Color.White);
            foreach ((int lineIndex, DrawRect rect) in selection)
            {
                TextInputLine line = layout.Lines[lineIndex];
                if (line.Text.Length == 0)
                {
                    continue;
                }

                context.DrawingContext.PushClip(rect);
                DrawLineText(context, line, origin.X, firstLineTop + (lineIndex * lineHeight), selectedTextBrush);
                context.DrawingContext.PopClip();
            }
        }

        if (IsCaretRenderEligible() && caretBlinkVisible)
        {
            DrawCaret(context, layout, origin.X, firstLineTop, lineHeight, visible, caretMetrics);
        }
    }

    private void RequestReveal(PendingReveal reveal, int lineIndex)
    {
        if (!layoutPending && hasArranged && Presenter is ScrollContentPresenter presenter)
        {
            Reveal(presenter, reveal, lineIndex);
            return;
        }

        // A caret reveal requested after a line reveal replaces it, as the later request is what the user did last.
        pendingReveal = reveal;
        pendingRevealLine = lineIndex;
        Control.Invalidate(InvalidationFlags.Arrange, "TextBox reveal pending");
    }

    private void Reveal(ScrollContentPresenter presenter, PendingReveal reveal, int lineIndex)
    {
        TextInputLineLayout layout = CurrentLayout;
        float lineHeight = GetLineHeight(CreateTextAspect());
        float firstLineOffset = GetFirstLineOffset(layout, ArrangedBounds.Height);
        // MakeVisible counts the rectangle from the rounded bounds; the text starts at the exact origin.
        LayoutPoint origin = GetTextOrigin(ArrangedBounds);
        float originX = origin.X - ArrangedBounds.X;
        float originY = origin.Y - ArrangedBounds.Y;
        if (reveal == PendingReveal.Line)
        {
            if (lineIndex >= layout.Lines.Length)
            {
                return;
            }

            // Only the vertical offset moves: the left edge of the visible part stays where it is.
            presenter.MakeVisible(
                this,
                new LayoutRect(
                    presenter.HorizontalOffset + originX,
                    originY + firstLineOffset + (lineIndex * lineHeight),
                    0,
                    lineHeight));
            return;
        }

        int displayIndex = host.GetDisplayIndex(editor.Caret.Position);
        int caretLine = GetLineIndex(layout, displayIndex, CaretAtLineEnd);
        float caretX = GetCaretX(layout.Lines[caretLine], displayIndex);
        // The rectangle the caret is drawn in, so a caret at the right edge is shown whole, inside the reserved width.
        presenter.MakeVisible(
            this,
            new LayoutRect(originX + caretX, originY + firstLineOffset + (caretLine * lineHeight), CaretWidth, lineHeight));
    }

    private LayoutPoint? GetContentPointAtRootPoint(float rootX, float rootY)
    {
        // The presenter's offsets are the newest scroll position, also before the arrange that moves this view to it.
        if (Presenter is ScrollContentPresenter presenter)
        {
            if (!InputCoordinateConverter.TryRootToElement(presenter, new System.Numerics.Vector2(rootX, rootY), out System.Numerics.Vector2 presenterPoint))
            {
                return null;
            }

            return new LayoutPoint(
                presenterPoint.X + presenter.HorizontalOffset,
                presenterPoint.Y + presenter.VerticalOffset);
        }

        if (!InputCoordinateConverter.TryRootToElement(this, new System.Numerics.Vector2(rootX, rootY), out System.Numerics.Vector2 point))
        {
            return null;
        }

        return new LayoutPoint(point.X, point.Y);
    }

    private int GetLineIndexAtContentY(TextInputLineLayout layout, float contentY)
    {
        float lineHeight = GetLineHeight(CreateTextAspect());
        float y = contentY - GetFirstLineOffset(layout, ArrangedBounds.Height);
        return lineHeight > 0
            ? Math.Clamp((int)MathF.Floor(y / lineHeight), 0, layout.Lines.Length - 1)
            : 0;
    }

    private int GetDisplayIndexAtX(TextInputLineLayout layout, int lineIndex, float x, out bool atLineEnd)
    {
        TextInputLine line = layout.Lines[lineIndex];
        int index = line.Start + caretLayout.GetCaretIndexAtX(
            line.CaretText,
            x,
            CreateTextAspect(),
            CreateFontResolver());
        atLineEnd = line.IsSoftWrapped && index == line.CaretEnd;
        return index;
    }

    private static int GetLineIndex(TextInputLineLayout layout, int displayIndex, bool atLineEnd)
    {
        TextInputLine[] lines = layout.Lines;
        int low = 0;
        int high = lines.Length - 1;
        while (low < high)
        {
            int middle = low + ((high - low + 1) / 2);
            if (lines[middle].Start <= displayIndex)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (atLineEnd &&
            low > 0 &&
            lines[low].Start == displayIndex &&
            lines[low - 1].IsSoftWrapped)
        {
            return low - 1;
        }

        return low;
    }

    private float GetCaretX(TextInputLine line, int displayIndex)
    {
        int position = Math.Clamp(displayIndex - line.Start, 0, line.CaretText.Length);
        return caretLayout.GetCaretX(line.CaretText, position, CreateTextAspect(), CreateFontResolver());
    }

    private List<(int Line, DrawRect Rect)> GetSelectionRects(
        TextInputLineLayout layout,
        float originX,
        float firstLineTop,
        float lineHeight,
        LayoutRect visible,
        TextCaretVerticalMetrics caretMetrics,
        int first,
        int last)
    {
        List<(int, DrawRect)> rects = [];
        int start = host.GetDisplayIndex(editor.Selection.Start);
        int end = host.GetDisplayIndex(editor.Selection.End);
        float visibleRight = visible.X + visible.Width;
        for (int i = first; i <= last; i++)
        {
            TextInputLine line = layout.Lines[i];
            int lineStart = Math.Max(start, line.Start);
            int lineEnd = Math.Min(end, line.CaretEnd);
            if (lineEnd < lineStart || end <= line.Start || start > line.CaretEnd)
            {
                continue;
            }

            float x = Math.Clamp(originX + GetCaretX(line, lineStart), visible.X, visibleRight);
            float right = Math.Clamp(originX + GetCaretX(line, lineEnd), visible.X, visibleRight);
            if (right <= x)
            {
                continue;
            }

            DrawRect vertical = GetLineVerticalBounds(firstLineTop + (i * lineHeight), lineHeight, visible, caretMetrics);
            rects.Add((i, new DrawRect(x, vertical.Y, right - x, vertical.Height)));
        }

        return rects;
    }

    private void DrawCaret(
        RenderContext context,
        TextInputLineLayout layout,
        float originX,
        float firstLineTop,
        float lineHeight,
        LayoutRect visible,
        TextCaretVerticalMetrics caretMetrics)
    {
        int displayIndex = host.GetDisplayIndex(editor.Caret.Position);
        int lineIndex = GetLineIndex(layout, displayIndex, CaretAtLineEnd);
        float x = originX + GetCaretX(layout.Lines[lineIndex], displayIndex);
        x = Math.Clamp(x, visible.X, visible.X + visible.Width);
        DrawRect vertical = GetLineVerticalBounds(firstLineTop + (lineIndex * lineHeight), lineHeight, visible, caretMetrics);
        context.DrawingContext.FillRectangle(
            new DrawRect(x, vertical.Y, CaretWidth, vertical.Height),
            host.CaretBrush);
    }

    // The caret and selection band of one line: the caret metrics inside the line box, which ends at the bottom of the
    // visible part, as the single-line viewport clamped them to its content box.
    private static DrawRect GetLineVerticalBounds(
        float lineTop,
        float lineHeight,
        LayoutRect visible,
        TextCaretVerticalMetrics metrics)
    {
        float natural = MathF.Max(lineHeight, metrics.OffsetY + metrics.Height);
        float boxHeight = MathF.Min(MathF.Max(1, natural), visible.Y + visible.Height - lineTop);
        float offsetY = Math.Clamp(metrics.OffsetY, 0, MathF.Max(0, boxHeight - 1));
        float availableHeight = MathF.Max(1, boxHeight - offsetY);
        float height = MathF.Min(MathF.Max(1, metrics.Height), availableHeight);
        return new DrawRect(0, lineTop + offsetY, 0, height);
    }

    private void DrawLineText(RenderContext context, TextInputLine line, float x, float lineTop, Brush? foreground)
    {
        GetTextRenderer().Render(
            context.DrawingContext,
            line.Text,
            CreateTextAspect(foreground),
            float.PositiveInfinity,
            new DrawPoint(x, lineTop));
    }

    private static (int First, int Last) GetVisibleLineRange(
        TextInputLineLayout layout,
        float firstLineTop,
        float lineHeight,
        float visibleTop,
        float visibleBottom)
    {
        int lastLine = layout.Lines.Length - 1;
        if (lineHeight <= 0)
        {
            return (0, lastLine);
        }

        int first = Math.Clamp((int)MathF.Floor((visibleTop - firstLineTop) / lineHeight), 0, lastLine);
        int last = Math.Clamp((int)MathF.Ceiling((visibleBottom - firstLineTop) / lineHeight), 0, lastLine);
        return (first, last);
    }

    // The part of this view that the presenter shows: the presenter clips its content to its own bounds.
    private LayoutRect GetVisibleBounds(LayoutRect bounds)
    {
        if (Presenter is not ScrollContentPresenter presenter)
        {
            return bounds;
        }

        LayoutRect viewport = presenter.ArrangedBounds;
        float left = MathF.Max(bounds.X, viewport.X);
        float top = MathF.Max(bounds.Y, viewport.Y);
        float right = MathF.Min(bounds.X + bounds.Width, viewport.X + viewport.Width);
        float bottom = MathF.Min(bounds.Y + bounds.Height, viewport.Y + viewport.Height);
        return new LayoutRect(left, top, MathF.Max(0, right - left), MathF.Max(0, bottom - top));
    }

    private float GetFirstLineOffset(TextInputLineLayout layout, float viewHeight)
    {
        TextAspect aspect = CreateTextAspect();
        return GetFirstLineOffset(
            layout,
            viewHeight,
            GetLineHeight(aspect),
            caretLayout.GetCaretVerticalMetrics(aspect, CreateFontResolver()));
    }

    // A ComboBox's editable TextBox centers its single line in the arranged height (CenterSingleLineContentVertically).
    private float GetFirstLineOffset(
        TextInputLineLayout layout,
        float viewHeight,
        float lineHeight,
        TextCaretVerticalMetrics caretMetrics)
    {
        if (!host.CentersSingleLineContentVertically || layout.Lines.Length != 1)
        {
            return 0;
        }

        float naturalHeight = MathF.Max(lineHeight, caretMetrics.OffsetY + caretMetrics.Height);
        float height = MathF.Min(viewHeight, MathF.Max(1, naturalHeight));
        return MathF.Max(0, (viewHeight - height) / 2);
    }

    // The caret is one device pixel wide and is drawn right of its position. The view keeps that width free right of the
    // text (MeasureCore and GetWrapWidth), as WPF's TextBoxView keeps a caret-wide margin, so the caret after the last
    // character of a line stays inside the ScrollViewer's clip also when the line fills or overflows the viewport.
    private float CaretWidth => 1 / (Root?.Scale ?? 1);

    private float GetWrapWidth(float availableWidth)
    {
        return host.TextWrapping != TextWrapping.NoWrap && float.IsFinite(availableWidth)
            ? MathF.Max(0, availableWidth - CaretWidth)
            : float.PositiveInfinity;
    }

    // The lines of the display text at one wrap width. The last two layouts are kept, so caret moves, the arrange and
    // the measure with another width do not break the lines again.
    private TextInputLineLayout GetLayout(float wrapWidth)
    {
        string text = host.DisplayText;
        if (recentLayout is not null && recentLayout.Matches(text, wrapWidth, metricsVersion))
        {
            return recentLayout;
        }

        if (olderLayout is not null && olderLayout.Matches(text, wrapWidth, metricsVersion))
        {
            (recentLayout, olderLayout) = (olderLayout, recentLayout);
            return recentLayout;
        }

        TextInputLineLayout layout = BuildLayout(text, wrapWidth);
        olderLayout = recentLayout;
        recentLayout = layout;
        return layout;
    }

    private TextInputLineLayout BuildLayout(string text, float wrapWidth)
    {
        TextAspect aspect = CreateTextAspect();
        TextMeasurer measurer = GetTextMeasurer();
        float Measure(int start, int length) =>
            length == 0 ? 0 : measurer.Measure(text.Substring(start, length), aspect, float.PositiveInfinity).Size.Width;

        IReadOnlyList<UnicodeLineSegment> segments = UnicodeLineBreakEngine.BreakLines(
            text,
            wrapWidth,
            LineBreakService.MapWrapping(host.TextWrapping),
            Measure);
        TextInputLine[] lines = new TextInputLine[segments.Count];
        float width = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            UnicodeLineSegment segment = segments[i];
            int contentEnd = segment.SourceStart + segment.SourceLength;
            int nextStart = i + 1 < segments.Count ? segments[i + 1].SourceStart : text.Length;
            int caretEnd = nextStart;
            bool isSoftWrapped = i + 1 < segments.Count;
            for (int index = contentEnd; index < nextStart; index++)
            {
                if (text[index] is '\r' or '\n')
                {
                    caretEnd = index;
                    isSoftWrapped = false;
                    break;
                }
            }

            lines[i] = new TextInputLine(
                segment.SourceStart,
                caretEnd,
                nextStart,
                isSoftWrapped,
                segment.Text,
                text[segment.SourceStart..caretEnd]);
            width = MathF.Max(width, segment.Width);
        }

        return new TextInputLineLayout(text, wrapWidth, metricsVersion, lines, width);
    }

    private float GetLineHeight(TextAspect aspect)
    {
        return TextLineMetrics.MeasureLineHeight(aspect, CreateFontResolver().Resolve(aspect));
    }

    private bool IsCaretRenderEligible()
    {
        return Control.IsKeyboardFocused &&
            Control.IsEnabled &&
            UIElementVisibility.ParticipatesInRendering(Control) &&
            host.CaretBrush.Opacity > 0;
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
            fontResourceId: FontResourceId,
            fontWeight: Control.FontWeight,
            fontStyle: Control.FontStyle,
            fontStretch: Control.FontStretch);
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

    // One drawn line, in display indices: Start is its first position, CaretEnd the caret position at its end (before a
    // line break, or after the trailing spaces of a wrapped line), and NextStart the first position of the next line.
    // Text is what is drawn; CaretText is the text from Start to CaretEnd that the caret measures.
    private readonly record struct TextInputLine(
        int Start,
        int CaretEnd,
        int NextStart,
        bool IsSoftWrapped,
        string Text,
        string CaretText);

    private sealed class TextInputLineLayout(
        string text,
        float wrapWidth,
        int metricsVersion,
        TextInputLine[] lines,
        float width)
    {
        public string Text { get; } = text;

        public TextInputLine[] Lines { get; } = lines;

        public float Width { get; } = width;

        public bool Matches(string candidateText, float candidateWrapWidth, int candidateMetricsVersion)
        {
            return metricsVersion == candidateMetricsVersion &&
                wrapWidth.Equals(candidateWrapWidth) &&
                string.Equals(Text, candidateText, StringComparison.Ordinal);
        }
    }
}
