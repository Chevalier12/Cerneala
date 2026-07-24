using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

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

    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(TextBox),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure |
                UiPropertyOptions.AffectsRender |
                UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<Color> CaretColorProperty = UiProperty<Color>.Register(
        nameof(CaretColor),
        typeof(TextBox),
        new UiPropertyMetadata<Color>(Color.Black, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Color> SelectionBackgroundProperty = UiProperty<Color>.Register(
        nameof(SelectionBackground),
        typeof(TextBox),
        new UiPropertyMetadata<Color>(new Color(0, 120, 215), UiPropertyOptions.AffectsRender));

    private readonly TextInputCore textInput;

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

    public TextSelection Selection => textInput.Selection;

    public TextCaret Caret => textInput.Caret;

    public Color CaretColor
    {
        get => GetValue(CaretColorProperty);
        set => SetValue(CaretColorProperty, value);
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

    public void ReceiveTextInput(string text) => textInput.ReceiveTextInput(text);

    public void Select(int anchor, int active) => textInput.Select(anchor, active);

    public void MoveCaret(int position, bool extendSelection = false) =>
        textInput.MoveCaret(position, extendSelection);

    public bool Undo() => textInput.Undo();

    public bool Redo() => textInput.Redo();

    public bool UpdateRenderTime(TimeSpan frameTime) => textInput.UpdateRenderTime(frameTime);

    protected virtual string NormalizeTextInput(string text) =>
        TextInputCore.NormalizeSingleLineInput(text);

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

    protected override LayoutSize MeasureCore(MeasureContext context) => textInput.Measure(context);

    protected override LayoutRect ArrangeCore(ArrangeContext context) => context.FinalRect;

    protected override void OnRender(RenderContext context) => textInput.Render(context);

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, TextProperty))
        {
            textInput.SynchronizeTextFromHost(Text);
            OnTextChanged(new TextChangedEventArgs(
                TextChangedEvent,
                this,
                (string)args.OldValue!,
                Text));
            textInput.ReportSelectionChanged();
        }

        if (ReferenceEquals(args.Property, IsKeyboardFocusedProperty))
        {
            textInput.OnKeyboardFocusChanged(IsKeyboardFocused);
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

    Color ITextInputHost.CaretColor => CaretColor;

    Color ITextInputHost.SelectionBackground => SelectionBackground;

    Thickness ITextInputHost.Insets => Insets;

    string ITextInputHost.NormalizeInput(string text) => NormalizeTextInput(text);

    void ITextInputHost.ApplyEditorText(string text) => SetValue(TextProperty, text);

    void ITextInputHost.RaiseSelectionChanged() =>
        OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));

    private void InitializeTextInputDefaults()
    {
        Focusable = true;
        IsTabStop = true;
        SetValue(PaddingProperty, new Thickness(4, 2, 4, 2), UiPropertyValueSource.AspectBase);
        SetValue(BorderThicknessProperty, new Thickness(1), UiPropertyValueSource.AspectBase);
        SetValue(
            BorderBrushProperty,
            new SolidColorBrush(new Color(120, 130, 145)),
            UiPropertyValueSource.AspectBase);
        SetValue(
            BackgroundProperty,
            new SolidColorBrush(Color.White),
            UiPropertyValueSource.AspectBase);
        Cursor = Cerneala.UI.Input.Cursor.IBeam;
    }
}
