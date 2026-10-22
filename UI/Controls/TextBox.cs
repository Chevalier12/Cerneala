using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

[TemplatePart(TextInputTemplates.ContentHostPartName, typeof(ScrollViewer))]
public class TextBox : Control, ITimeSensitiveRenderElement, IPointerDragSource, ITextInputHost
{
    public static readonly RoutedEvent TextChangedEvent = RoutedEventRegistry.Register(
        nameof(TextChanged),
        typeof(TextBox),
        RoutingStrategy.Bubble,
        typeof(TextChangedEventArgs));

    public static readonly RoutedEvent SelectionChangedEvent = RoutedEventRegistry.Register(
        nameof(SelectionChanged),
        typeof(TextBox),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    // As WPF's TextBox.Text, the text affects neither the box's own measure nor its own drawing: the text view in the
    // template lays out and draws it and invalidates itself (TextInputView.InvalidateTextMetrics).
    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(TextBox),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<bool> IsReadOnlyProperty = UiProperty<bool>.Register(
        nameof(IsReadOnly),
        typeof(TextBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<Brush> CaretBrushProperty = UiProperty<Brush>.Register(
        nameof(CaretBrush),
        typeof(TextBox),
        new UiPropertyMetadata<Brush>(
            new SolidColorBrush(Color.Black),
            UiPropertyOptions.AffectsRender,
            validateValue: value => value is not null));

    public static readonly UiProperty<Color> SelectionBackgroundProperty = UiProperty<Color>.Register(
        nameof(SelectionBackground),
        typeof(TextBox),
        new UiPropertyMetadata<Color>(new Color(0, 120, 215), UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<bool> AcceptsReturnProperty = UiProperty<bool>.Register(
        nameof(AcceptsReturn),
        typeof(TextBox),
        new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<bool> AcceptsTabProperty = UiProperty<bool>.Register(
        nameof(AcceptsTab),
        typeof(TextBox),
        new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<int> MaxLengthProperty = UiProperty<int>.Register(
        nameof(MaxLength),
        typeof(TextBox),
        new UiPropertyMetadata<int>(0, validateValue: value => value >= 0));

    public static readonly UiProperty<CharacterCasing> CharacterCasingProperty = UiProperty<CharacterCasing>.Register(
        nameof(CharacterCasing),
        typeof(TextBox),
        new UiPropertyMetadata<CharacterCasing>(CharacterCasing.Normal));

    public static readonly UiProperty<bool> IsUndoEnabledProperty = UiProperty<bool>.Register(
        nameof(IsUndoEnabled),
        typeof(TextBox),
        new UiPropertyMetadata<bool>(true));

    public static readonly UiProperty<int> UndoLimitProperty = UiProperty<int>.Register(
        nameof(UndoLimit),
        typeof(TextBox),
        new UiPropertyMetadata<int>(-1, validateValue: value => value >= -1));

    public static readonly UiProperty<TextWrapping> TextWrappingProperty = UiProperty<TextWrapping>.Register(
        nameof(TextWrapping),
        typeof(TextBox),
        new UiPropertyMetadata<TextWrapping>(
            TextWrapping.NoWrap,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<int> MinLinesProperty = UiProperty<int>.Register(
        nameof(MinLines),
        typeof(TextBox),
        new UiPropertyMetadata<int>(1, UiPropertyOptions.AffectsMeasure, validateValue: value => value >= 1));

    public static readonly UiProperty<int> MaxLinesProperty = UiProperty<int>.Register(
        nameof(MaxLines),
        typeof(TextBox),
        new UiPropertyMetadata<int>(int.MaxValue, UiPropertyOptions.AffectsMeasure, validateValue: value => value >= 1));

    public static readonly UiProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
        UiProperty<ScrollBarVisibility>.Register(
            nameof(HorizontalScrollBarVisibility),
            typeof(TextBox),
            new UiPropertyMetadata<ScrollBarVisibility>(ScrollBarVisibility.Hidden, UiPropertyOptions.AffectsMeasure));

    public static readonly UiProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
        UiProperty<ScrollBarVisibility>.Register(
            nameof(VerticalScrollBarVisibility),
            typeof(TextBox),
            new UiPropertyMetadata<ScrollBarVisibility>(ScrollBarVisibility.Hidden, UiPropertyOptions.AffectsMeasure));

    private readonly TextInputCore textInput;
    private ScrollViewer? contentHost;
    private bool centerSingleLineContentVertically;

    public TextBox()
    {
        textInput = new TextInputCore(this, TextInputPolicy.TextBox);
        InitializeTextInputDefaults();
    }

    public event EventHandler<TextChangedEventArgs> TextChanged
    {
        add => AddTypedHandler(TextChangedEvent, value);
        remove => RemoveTypedHandler(TextChangedEvent, value);
    }

    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public virtual string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    // As WPF's TextBox.AcceptsTab: Tab and Shift+Tab type a tab instead of moving the focus; Ctrl+Tab still moves it.
    public bool AcceptsTab
    {
        get => GetValue(AcceptsTabProperty);
        set => SetValue(AcceptsTabProperty, value);
    }

    // The most characters that typed and pasted text can bring Text to; 0, the default, is no limit. Text set by code
    // is not limited.
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    // Upper or Lower changes the case of typed and pasted text; Text set by code keeps its case.
    public CharacterCasing CharacterCasing
    {
        get => GetValue(CharacterCasingProperty);
        set => SetValue(CharacterCasingProperty, value);
    }

    // False clears the undo history and stops recording it.
    public bool IsUndoEnabled
    {
        get => GetValue(IsUndoEnabledProperty);
        set => SetValue(IsUndoEnabledProperty, value);
    }

    // The most undo units kept; -1, the default, is no limit, and 0 keeps none.
    public int UndoLimit
    {
        get => GetValue(UndoLimitProperty);
        set => SetValue(UndoLimitProperty, value);
    }

    public bool CanUndo => textInput.CanUndo;

    public bool CanRedo => textInput.CanRedo;

    // The selected text. Setting it replaces the selection, as one undo unit, and selects the new text, as in WPF.
    public string SelectedText
    {
        get => textInput.SelectedText;
        set => textInput.ReplaceSelectedText(value ?? string.Empty);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public int MinLines
    {
        get => GetValue(MinLinesProperty);
        set => SetValue(MinLinesProperty, value);
    }

    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    // The drawn lines at the width of the last arrange; before the first layout the text does not wrap.
    public int LineCount => textInput.View.LineCount;

    public TextSelection Selection => textInput.Selection;

    public TextCaret Caret => textInput.Caret;

    public Brush CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetValue(CaretBrushProperty, value);
        }
    }

    public Color SelectionBackground
    {
        get => GetValue(SelectionBackgroundProperty);
        set => SetValue(SelectionBackgroundProperty, value);
    }

    public TextMeasurer TextMeasurer
    {
        get => textInput.TextMeasurer;
        set => textInput.TextMeasurer = value;
    }

    public TextRenderer TextRenderer
    {
        get => textInput.TextRenderer;
        set => textInput.TextRenderer = value;
    }

    public ResourceId<FontResource>? FontResourceId
    {
        get => textInput.FontResourceId;
        set => textInput.FontResourceId = value;
    }

    public IResourceProvider? ResourceProvider
    {
        get => textInput.ResourceProvider;
        set => textInput.ResourceProvider = value;
    }

    // The press belongs to the text box, as WPF's text editor handles MouseLeftButtonDown, so an enclosing button is
    // not pressed (#586).
    internal override bool OwnsPointerClick => IsEnabled;

    internal TextInputView TextView => textInput.View;

    internal bool CenterSingleLineContentVertically
    {
        get => centerSingleLineContentVertically;
        set
        {
            if (centerSingleLineContentVertically == value)
            {
                return;
            }

            centerSingleLineContentVertically = value;
            textInput.View.InvalidateTextRender("TextBox vertical content alignment changed");
        }
    }

    // The text of the drawn line, with its line break characters.
    public string GetLineText(int lineIndex) => textInput.View.GetLineText(lineIndex);

    // Scrolls the text by the least amount that shows the line.
    public void ScrollToLine(int lineIndex) => textInput.View.ScrollToLine(lineIndex);

    public void ReceiveTextInput(string text) => textInput.ReceiveTextInput(text);

    public void Select(int anchor, int active) => textInput.Select(anchor, active);

    public void MoveCaret(int position, bool extendSelection = false) =>
        textInput.MoveCaret(position, extendSelection);

    public bool Undo() => textInput.Undo();

    public bool Redo() => textInput.Redo();

    // Copy, Cut and Paste do what Ctrl+C, Ctrl+X and Ctrl+V do: Cut and Paste do nothing while IsReadOnly.
    public void Copy() => textInput.Copy();

    public void Cut() => textInput.Cut();

    public void Paste() => textInput.Paste();

    public bool UpdateRenderTime(TimeSpan frameTime) => textInput.UpdateRenderTime(frameTime);

    protected virtual string NormalizeTextInput(string text) =>
        TextInputCore.NormalizeInput(text, keepsLineBreaks: AcceptsReturn, keepsTabs: AcceptsTab);

    protected virtual void OnTextChanged(TextChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        RaiseEvent(args);
    }

    protected virtual void OnSelectionChanged(RoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        RaiseEvent(args);
    }

    // As WPF's TextBox: MaxLines caps the height the template gets, and MinLines sets the least desired height, both in
    // lines of the font plus the border and padding.
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        int minLines = MinLines;
        int maxLines = MaxLines;
        if (minLines > maxLines)
        {
            throw new InvalidOperationException(
                $"TextBox MinLines ({minLines}) cannot be greater than MaxLines ({maxLines}).");
        }

        float lineHeight = textInput.View.LineHeight;
        Thickness insets = Insets;
        float verticalInsets = insets.Top + insets.Bottom;
        LayoutSize available = context.AvailableSize;
        float height = maxLines == int.MaxValue
            ? available.Height
            : MathF.Min(available.Height, (maxLines * lineHeight) + verticalInsets);
        LayoutSize desired = base.MeasureCore(new MeasureContext(new LayoutSize(available.Width, height), context.Rounding));
        float minHeight = MathF.Min((minLines * lineHeight) + verticalInsets, height);
        return new LayoutSize(desired.Width, MathF.Max(desired.Height, minHeight));
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        textInput.View.CompleteLayout();
        return arranged;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        base.OnTemplateApplied(instance);
        contentHost = instance is null
            ? null
            : GetRequiredTemplatePart<ScrollViewer>(TextInputTemplates.ContentHostPartName);
        textInput.AttachContentHost(contentHost);
        UpdateContentHostScrollBars();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        UiProperty property = args.Property;
        if (ReferenceEquals(property, TextProperty))
        {
            textInput.SynchronizeTextFromHost(Text);
            OnTextChanged(new TextChangedEventArgs(
                TextChangedEvent,
                this,
                (string)args.OldValue!,
                Text));
            textInput.ReportSelectionChanged();
        }

        if (ReferenceEquals(property, IsKeyboardFocusedProperty))
        {
            textInput.OnKeyboardFocusChanged(IsKeyboardFocused);
        }

        if (ReferenceEquals(property, IsUndoEnabledProperty))
        {
            textInput.IsUndoEnabled = IsUndoEnabled;
        }
        else if (ReferenceEquals(property, UndoLimitProperty))
        {
            textInput.UndoLimit = UndoLimit;
        }

        if (ReferenceEquals(property, TextWrappingProperty) ||
            ReferenceEquals(property, HorizontalScrollBarVisibilityProperty) ||
            ReferenceEquals(property, VerticalScrollBarVisibilityProperty))
        {
            UpdateContentHostScrollBars();
        }

        if (ReferenceEquals(property, FontFamilyProperty) ||
            ReferenceEquals(property, FontSizeProperty) ||
            ReferenceEquals(property, FontWeightProperty) ||
            ReferenceEquals(property, FontStyleProperty) ||
            ReferenceEquals(property, FontStretchProperty) ||
            ReferenceEquals(property, ForegroundProperty) ||
            ReferenceEquals(property, PaddingProperty) ||
            ReferenceEquals(property, TextWrappingProperty) ||
            ReferenceEquals(property, AcceptsReturnProperty))
        {
            textInput.View.InvalidateTextMetrics("TextBox text layout property changed");
        }
        else if (ReferenceEquals(property, CaretBrushProperty) ||
            ReferenceEquals(property, SelectionBackgroundProperty) ||
            ReferenceEquals(property, IsEnabledProperty))
        {
            textInput.View.InvalidateTextRender("TextBox text render property changed");
        }
    }

    bool IPointerDragSource.BeginPointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args) =>
        textInput.BeginPointerDrag(captureManager, routeMap, args);

    bool IPointerDragSource.UpdatePointerDrag(MouseEventArgs args) =>
        textInput.UpdatePointerDrag(args);

    bool IPointerDragSource.CompletePointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args) =>
        textInput.CompletePointerDrag(captureManager, routeMap, args);

    Control ITextInputHost.Control => this;

    string ITextInputHost.TextValue => Text;

    string ITextInputHost.DisplayText => Text;

    int ITextInputHost.GetDisplayIndex(int textPosition) => textPosition;

    int ITextInputHost.GetTextPosition(int displayIndex) => displayIndex;

    Brush ITextInputHost.CaretBrush => CaretBrush;

    Color ITextInputHost.SelectionBackground => SelectionBackground;

    bool ITextInputHost.AllowsEditing => !IsReadOnly;

    bool ITextInputHost.AcceptsReturn => AcceptsReturn;

    bool ITextInputHost.AcceptsTab => AcceptsTab;

    int ITextInputHost.MaxLength => MaxLength;

    CharacterCasing ITextInputHost.CharacterCasing => CharacterCasing;

    TextWrapping ITextInputHost.TextWrapping => TextWrapping;

    bool ITextInputHost.CentersSingleLineContentVertically => CenterSingleLineContentVertically;

    string ITextInputHost.NormalizeInput(string text) => NormalizeTextInput(text);

    void ITextInputHost.ApplyEditorText(string text) => SetValue(TextProperty, text);

    void ITextInputHost.RaiseSelectionChanged() =>
        OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));

    // Wrapped text wraps at the viewport width, so it does not scroll horizontally, as in WPF. WrapWithOverflow keeps a word
    // that is wider than the viewport on one line, so that line has to scroll horizontally to be reachable.
    private void UpdateContentHostScrollBars()
    {
        if (contentHost is null)
        {
            return;
        }

        contentHost.HorizontalScrollBarVisibility = TextWrapping == TextWrapping.Wrap
            ? ScrollBarVisibility.Disabled
            : HorizontalScrollBarVisibility;
        contentHost.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
    }

    private void InitializeTextInputDefaults()
    {
        SetFrameworkDefault(FocusableProperty, true);
        SetFrameworkDefault(IsTabStopProperty, true);
        SetFrameworkDefault(ComponentTemplateProperty, TextInputTemplates.TextBox);
        SetFrameworkDefault(PaddingProperty, new Thickness(4, 2, 4, 2));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(120, 130, 145)));
        SetFrameworkDefault(BackgroundProperty, new SolidColorBrush(Color.White));
        SetFrameworkDefault(CursorProperty, Cerneala.UI.Input.Cursor.IBeam);
    }
}
