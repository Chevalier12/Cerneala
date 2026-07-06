using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public abstract class TextBoxBase : Control
{
    private readonly TextEditor editor;
    private readonly TextLayoutCache resourceTextLayoutCache = new();
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;
    private IResourceProvider? resourceProvider;
    private ResourceId<FontResource>? fontResourceId;
    private float horizontalTextOffset;

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
        Handlers.AddHandler(InputEvents.TextInputEvent, OnRoutedTextInput);
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnRoutedKeyDown);
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
        return ContentControl.Inflate(result.Size, insets);
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
        Invalidate(InvalidationFlags.Render, "TextBox caret changed");
        return true;
    }

    private bool HandleMoveTo(int position)
    {
        editor.MoveCaret(position);
        EnsureCaretVisible();
        Invalidate(InvalidationFlags.Render, "TextBox caret changed");
        return true;
    }

    private void SyncTextFromEditor(string reason)
    {
        SetValue(TextProperty, editor.Document.Text);
        EnsureCaretVisible();
        InvalidateTextMetrics(reason);
    }

    private void DrawSelection(RenderContext context, LayoutRect content)
    {
        float start = content.X + MeasureTextWidth(DisplayText[..Selection.Start]) - horizontalTextOffset;
        float end = content.X + MeasureTextWidth(DisplayText[..Selection.End]) - horizontalTextOffset;
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
        float x = content.X + MeasureTextWidth(DisplayText[..Caret.Position]) - horizontalTextOffset;
        x = Math.Clamp(x, content.X, content.X + content.Width);
        context.DrawingContext.FillRectangle(
            new DrawRect(x, content.Y, 1, MathF.Max(1, content.Height)),
            CaretColor);
    }

    private bool ShouldRenderCaret()
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

        float caretX = MeasureTextWidth(DisplayText[..Caret.Position]);
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
            ? new TextMeasurer(new FontResolver(provider), LineBreakService.Default, resourceTextLayoutCache)
            : TextMeasurer;
    }

    private TextRenderer GetTextRenderer()
    {
        IResourceProvider? provider = ResolveResourceProvider();
        if (FontResourceId is not null && provider is not null)
        {
            FontResolver resolver = new(provider);
            return new TextRenderer(resolver, new TextMeasurer(resolver, LineBreakService.Default, resourceTextLayoutCache));
        }

        return TextRenderer;
    }

    private IResourceProvider? ResolveResourceProvider()
    {
        return ResourceProvider ?? Root?.ResourceProvider;
    }
}
