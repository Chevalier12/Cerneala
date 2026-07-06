using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Platform;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public abstract class TextBoxBase : Control, ITimeSensitiveRenderElement
{
    private static readonly TimeSpan CaretBlinkPeriod = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan CaretBlinkVisibleDuration = TimeSpan.FromMilliseconds(500);
    private readonly TextEditor editor;
    private readonly TextCaretLayout caretLayout = TextCaretLayout.Default;
    private readonly TextLayoutCache resourceTextLayoutCache = new();
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;
    private IResourceProvider? resourceProvider;
    private ResourceId<FontResource>? fontResourceId;
    private float horizontalTextOffset;
    private TimeSpan caretBlinkAnchor;
    private TimeSpan lastCaretFrameTime;
    private bool caretBlinkVisible = true;

    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(TextBoxBase),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<DrawColor> CaretColorProperty = UiProperty<DrawColor>.Register(
        nameof(CaretColor),
        typeof(TextBoxBase),
        new UiPropertyMetadata<DrawColor>(DrawColor.Black, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawColor> SelectionBackgroundProperty = UiProperty<DrawColor>.Register(
        nameof(SelectionBackground),
        typeof(TextBoxBase),
        new UiPropertyMetadata<DrawColor>(new DrawColor(96, 165, 250, 120), UiPropertyOptions.AffectsRender));

    protected TextBoxBase()
    {
        editor = new TextEditor();
        Focusable = true;
        IsTabStop = true;
        Padding = new Thickness(4, 2, 4, 2);
        BorderThickness = new Thickness(1);
        BorderColor = new DrawColor(120, 130, 145);
        Background = DrawColor.White;
        Cursor = Cerneala.UI.Input.Cursor.IBeam;
        Handlers.AddHandler(InputEvents.TextInputEvent, OnRoutedTextInput);
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnRoutedKeyDown);
        Handlers.AddHandler(InputEvents.MouseDownEvent, OnRoutedMouseDown);
    }

    public TextEditor Editor => editor;

    public virtual string Text
    {
        get => GetValue(TextProperty);
        set => SetTextCore(value ?? string.Empty);
    }

    public TextSelection Selection => editor.Selection;

    public TextCaret Caret => editor.Caret;

    public DrawColor CaretColor
    {
        get => GetValue(CaretColorProperty);
        set => SetValue(CaretColorProperty, value);
    }

    public DrawColor SelectionBackground
    {
        get => GetValue(SelectionBackgroundProperty);
        set => SetValue(SelectionBackgroundProperty, value);
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
            Invalidate(InvalidationFlags.Render, "TextBox text renderer changed");
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

    protected virtual string DisplayText => Text;

    public void ReceiveTextInput(string text)
    {
        editor.InsertText(text ?? string.Empty);
        SyncTextFromEditor("TextBox text input");
        EnsureCaretVisible();
        ResetCaretBlink();
    }

    public void Select(int anchor, int active)
    {
        editor.Select(anchor, active);
        EnsureCaretVisible();
        Invalidate(InvalidationFlags.Render, "TextBox selection changed");
    }

    public void MoveCaret(int position, bool extendSelection = false)
    {
        editor.MoveCaret(position, extendSelection);
        EnsureCaretVisible();
        ResetCaretBlink();
        Invalidate(InvalidationFlags.Render, "TextBox caret changed");
    }

    public bool Undo()
    {
        bool changed = editor.Undo();
        if (changed)
        {
            SyncTextFromEditor("TextBox undo");
        }

        return changed;
    }

    public bool Redo()
    {
        bool changed = editor.Redo();
        if (changed)
        {
            SyncTextFromEditor("TextBox redo");
        }

        return changed;
    }

    protected void SetTextCore(string text)
    {
        string next = text ?? string.Empty;
        if (Text == next && editor.Document.Text == next)
        {
            return;
        }

        SetValue(TextProperty, next);
        editor.SetText(next);
        editor.UndoRedo.Clear();
        EnsureCaretVisible();
        InvalidateTextMetrics("TextBox text changed");
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Thickness insets = Insets;
        LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
        TextRunStyle style = CreateTextStyle();
        TextMeasureResult result = GetTextMeasurer().Measure(DisplayText, style, available.Width);
        TextCaretVerticalMetrics caretMetrics = caretLayout.GetCaretVerticalMetrics(style, CreateFontResolver());
        float textEditingHeight = caretMetrics.OffsetY + caretMetrics.Height;
        LayoutSize contentSize = new(result.Size.Width, MathF.Max(result.Size.Height, textEditingHeight));
        return ContentControl.Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, TextProperty) && editor.Document.Text != Text)
        {
            editor.SetText(Text);
            editor.UndoRedo.Clear();
            EnsureCaretVisible();
        }

        if (ReferenceEquals(args.Property, IsKeyboardFocusedProperty) && IsKeyboardFocused)
        {
            ResetCaretBlink();
        }
    }

    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, Background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderColor.A != 0 && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, BorderColor, thickness);
        }

        LayoutRect content = ContentControl.Deflate(context.Bounds, Insets);
        if (content.Width <= 0 || content.Height <= 0)
        {
            return;
        }

        DrawRect clip = Border.ToDrawRect(content);
        context.DrawingContext.PushClip(clip);

        if (!Selection.IsEmpty && SelectionBackground.A != 0)
        {
            DrawSelection(context, content);
        }

        if (DisplayText.Length > 0)
        {
            GetTextRenderer().Render(
                context.DrawingContext,
                DisplayText,
                CreateTextStyle(),
                content.Width + horizontalTextOffset,
                new DrawPoint(content.X - horizontalTextOffset, content.Y),
                Foreground);
        }

        if (ShouldRenderCaret())
        {
            DrawCaret(context, content);
        }

        context.DrawingContext.PopClip();
    }

    private void OnRoutedTextInput(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not TextCompositionEventArgs textArgs || textArgs.Handled)
        {
            return;
        }

        ReceiveTextInput(textArgs.Text);
        textArgs.Handled = true;
    }

    private void OnRoutedKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || keyArgs.Handled)
        {
            return;
        }

        if (keyArgs.IsControlDown && !keyArgs.IsAltDown && HandleClipboardShortcut(keyArgs.Key))
        {
            keyArgs.Handled = true;
            return;
        }

        bool handled = keyArgs.Key switch
        {
            InputKey.Back => HandleBackspace(),
            InputKey.Delete => HandleDelete(),
            InputKey.Home => HandleMoveTo(0),
            InputKey.End => HandleMoveTo(editor.Document.Length),
            InputKey.Left => HandleMove(-1),
            InputKey.Right => HandleMove(1),
            _ => false
        };

        keyArgs.Handled = handled;
    }

    private void OnRoutedMouseDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs mouseArgs || mouseArgs.Handled || mouseArgs.ChangedButton != InputMouseButton.Left)
        {
            return;
        }

        LayoutRect content = ContentControl.Deflate(ArrangedBounds, Insets);
        float textX = mouseArgs.X - content.X + horizontalTextOffset;
        int index = caretLayout.GetCaretIndexAtX(DisplayText, textX, CreateTextStyle(), CreateFontResolver());
        MoveCaret(index);
        mouseArgs.Handled = true;
    }

    private bool HandleClipboardShortcut(InputKey key)
    {
        return key switch
        {
            InputKey.A => SelectAllText(),
            InputKey.C => CopySelection(),
            InputKey.X => CutSelection(),
            InputKey.V => PasteClipboardText(),
            _ => false
        };
    }

    private bool SelectAllText()
    {
        Select(0, Text.Length);
        return true;
    }

    private bool CopySelection()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard is null || Selection.IsEmpty)
        {
            return false;
        }

        clipboard.SetText(Text.Substring(Selection.Start, Selection.Length));
        return true;
    }

    private bool CutSelection()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard is null || Selection.IsEmpty)
        {
            return false;
        }

        clipboard.SetText(Text.Substring(Selection.Start, Selection.Length));
        editor.ReplaceSelection(string.Empty);
        SyncTextFromEditor("TextBox cut");
        return true;
    }

    private bool PasteClipboardText()
    {
        IClipboard? clipboard = ResolveClipboard();
        if (clipboard?.HasText != true)
        {
            return false;
        }

        string? text = clipboard.GetText();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        editor.InsertText(text);
        SyncTextFromEditor("TextBox paste");
        return true;
    }

    private IClipboard? ResolveClipboard()
    {
        return Root?.PlatformServices.Clipboard ?? Root?.PlatformServices.TextInput?.Clipboard;
    }

    private bool HandleBackspace()
    {
        string before = editor.Document.Text;
        editor.Backspace();
        if (before == editor.Document.Text)
        {
            return false;
        }

        SyncTextFromEditor("TextBox backspace");
        EnsureCaretVisible();
        return true;
    }

    private bool HandleDelete()
    {
        string before = editor.Document.Text;
        editor.Delete();
        if (before == editor.Document.Text)
        {
            return false;
        }

        SyncTextFromEditor("TextBox delete");
        EnsureCaretVisible();
        return true;
    }

    private bool HandleMove(int delta)
    {
        editor.MoveCaret(editor.Caret.Position + delta);
        EnsureCaretVisible();
        ResetCaretBlink();
        Invalidate(InvalidationFlags.Render, "TextBox caret changed");
        return true;
    }

    private bool HandleMoveTo(int position)
    {
        editor.MoveCaret(position);
        EnsureCaretVisible();
        ResetCaretBlink();
        Invalidate(InvalidationFlags.Render, "TextBox caret changed");
        return true;
    }

    private void SyncTextFromEditor(string reason)
    {
        SetValue(TextProperty, editor.Document.Text);
        EnsureCaretVisible();
        ResetCaretBlink();
        InvalidateTextMetrics(reason);
    }

    private void DrawSelection(RenderContext context, LayoutRect content)
    {
        float start = content.X + GetCaretTextX(Selection.Start) - horizontalTextOffset;
        float end = content.X + GetCaretTextX(Selection.End) - horizontalTextOffset;
        float x = Math.Clamp(start, content.X, content.X + content.Width);
        float right = Math.Clamp(end, content.X, content.X + content.Width);
        if (right <= x)
        {
            return;
        }

        context.DrawingContext.FillRectangle(
            new DrawRect(x, content.Y, right - x, MathF.Max(1, content.Height)),
            SelectionBackground);
    }

    private void DrawCaret(RenderContext context, LayoutRect content)
    {
        float x = content.X + GetCaretTextX(Caret.Position) - horizontalTextOffset;
        x = Math.Clamp(x, content.X, content.X + content.Width);
        DrawRect verticalBounds = GetCaretVerticalBounds(content);
        context.DrawingContext.FillRectangle(
            new DrawRect(x, verticalBounds.Y, 1, verticalBounds.Height),
            CaretColor);
    }

    private DrawRect GetCaretVerticalBounds(LayoutRect content)
    {
        TextRunStyle style = CreateTextStyle();
        TextCaretVerticalMetrics metrics = caretLayout.GetCaretVerticalMetrics(style, CreateFontResolver());
        float offsetY = Math.Clamp(metrics.OffsetY, 0, MathF.Max(0, content.Height - 1));
        float availableHeight = MathF.Max(1, content.Height - offsetY);
        float height = MathF.Min(MathF.Max(1, metrics.Height), availableHeight);
        return new DrawRect(content.X, content.Y + offsetY, content.Width, height);
    }

    private bool ShouldRenderCaret()
    {
        return IsCaretRenderEligible() &&
            caretBlinkVisible;
    }

    public bool UpdateRenderTime(TimeSpan frameTime)
    {
        lastCaretFrameTime = frameTime;
        if (!IsCaretRenderEligible())
        {
            caretBlinkVisible = true;
            return false;
        }

        TimeSpan elapsed = frameTime - caretBlinkAnchor;
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
        Invalidate(InvalidationFlags.Render, "TextBox caret blink phase changed");
        return true;
    }

    private bool IsCaretRenderEligible()
    {
        return IsKeyboardFocused &&
            IsEnabled &&
            UIElementVisibility.ParticipatesInRendering(this) &&
            CaretColor.A != 0;
    }

    private void EnsureCaretVisible()
    {
        float contentWidth = MathF.Max(0, ArrangedBounds.Width - Insets.Left - Insets.Right);
        if (contentWidth <= 0)
        {
            horizontalTextOffset = 0;
            return;
        }

        float caretX = GetCaretTextX(Caret.Position);
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
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Render, "TextBox horizontal viewport changed");
        }
    }

    private float MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return GetTextMeasurer().Measure(text, CreateTextStyle(), float.PositiveInfinity).Size.Width;
    }

    private float GetCaretTextX(int position)
    {
        return caretLayout.GetCaretX(DisplayText, position, CreateTextStyle(), CreateFontResolver());
    }

    private void ResetCaretBlink()
    {
        caretBlinkAnchor = lastCaretFrameTime;
        if (caretBlinkVisible)
        {
            return;
        }

        caretBlinkVisible = true;
        Invalidate(InvalidationFlags.Render, "TextBox caret blink reset");
    }

    private void InvalidateTextMetrics(string reason)
    {
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, reason);
    }

    private TextRunStyle CreateTextStyle()
    {
        return new TextRunStyle(FontFamily, FontSize, color: Foreground, fontResourceId: FontResourceId);
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
            return new TextRenderer(resolver, new TextMeasurer(resolver, LineBreakService.Default, resourceTextLayoutCache));
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
        return ResourceProvider ?? Root?.ResourceProvider;
    }
}
