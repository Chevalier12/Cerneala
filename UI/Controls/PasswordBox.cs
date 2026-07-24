using System.Globalization;
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

public class PasswordBox : Control, ITimeSensitiveRenderElement, IPointerDragSource, ITextInputHost
{
    public static readonly RoutedEvent PasswordChangedEvent = RoutedEventRegistry.Register(
        nameof(PasswordChanged),
        typeof(PasswordBox),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly UiProperty<char> PasswordCharProperty = UiProperty<char>.Register(
        nameof(PasswordChar),
        typeof(PasswordBox),
        new UiPropertyMetadata<char>(
            '*',
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Brush> CaretBrushProperty = UiProperty<Brush>.Register(
        nameof(CaretBrush),
        typeof(PasswordBox),
        new UiPropertyMetadata<Brush>(
            new SolidColorBrush(Color.Black),
            UiPropertyOptions.AffectsRender,
            validateValue: value => value is not null));

    public static readonly UiProperty<Color> SelectionBackgroundProperty = UiProperty<Color>.Register(
        nameof(SelectionBackground),
        typeof(PasswordBox),
        new UiPropertyMetadata<Color>(new Color(0, 120, 215), UiPropertyOptions.AffectsRender));

    private readonly TextInputCore textInput;
    private string password = string.Empty;

    public PasswordBox()
    {
        textInput = new TextInputCore(this, TextInputPolicy.PasswordBox);
        InitializeTextInputDefaults();
    }

    public event RoutedEventHandler PasswordChanged
    {
        add => AddHandler(PasswordChangedEvent, value);
        remove => RemoveHandler(PasswordChangedEvent, value);
    }

    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public virtual string Password
    {
        get => password;
        set => SetPassword(value ?? string.Empty, synchronizeEditor: true);
    }

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

    public bool UpdateRenderTime(TimeSpan frameTime) => textInput.UpdateRenderTime(frameTime);

    internal int UndoHistoryCount => textInput.UndoHistoryCount;

    protected virtual string NormalizeTextInput(string text) =>
        TextInputCore.NormalizeSingleLineInput(text);

    protected virtual void OnPasswordChanged(RoutedEventArgs args)
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

    string ITextInputHost.TextValue => Password;

    string ITextInputHost.DisplayText => new(PasswordChar, CountTextElements(Password));

    Brush ITextInputHost.CaretBrush => CaretBrush;

    Color ITextInputHost.SelectionBackground => SelectionBackground;

    Thickness ITextInputHost.Insets => Insets;

    string ITextInputHost.NormalizeInput(string text) => NormalizeTextInput(text);

    void ITextInputHost.ApplyEditorText(string text) =>
        SetPassword(text, synchronizeEditor: false);

    void ITextInputHost.RaiseSelectionChanged()
    {
    }

    private void SetPassword(string value, bool synchronizeEditor)
    {
        string next = value ?? string.Empty;
        if (password == next)
        {
            return;
        }

        password = next;
        if (synchronizeEditor)
        {
            textInput.SynchronizeTextFromHost(next);
        }

        OnPasswordChanged(new RoutedEventArgs(PasswordChangedEvent, this));
    }

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

    private static int CountTextElements(string text)
    {
        return text.Length == 0 ? 0 : StringInfo.ParseCombiningCharacters(text).Length;
    }
}
