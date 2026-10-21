using System.Globalization;
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

    // The press belongs to the password box, as WPF's text editor handles MouseLeftButtonDown, so an enclosing
    // button is not pressed (#586).
    internal override bool OwnsPointerClick => IsEnabled;

    protected virtual string NormalizeTextInput(string text) =>
        TextInputCore.NormalizeSingleLineInput(text);

    protected virtual void OnPasswordChanged(RoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        RaiseEvent(args);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        textInput.View.CompleteLayout();
        return arranged;
    }

    // The password box scrolls its single line horizontally like a TextBox, without scroll bars, as in WPF.
    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        base.OnTemplateApplied(instance);
        ScrollViewer? contentHost = instance is null
            ? null
            : GetRequiredTemplatePart<ScrollViewer>(TextInputTemplates.ContentHostPartName);
        textInput.AttachContentHost(contentHost);
        if (contentHost is not null)
        {
            contentHost.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            contentHost.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        UiProperty property = args.Property;
        if (ReferenceEquals(property, IsKeyboardFocusedProperty))
        {
            textInput.OnKeyboardFocusChanged(IsKeyboardFocused);
        }

        if (ReferenceEquals(property, PasswordCharProperty) ||
            ReferenceEquals(property, FontFamilyProperty) ||
            ReferenceEquals(property, FontSizeProperty) ||
            ReferenceEquals(property, ForegroundProperty) ||
            ReferenceEquals(property, PaddingProperty))
        {
            textInput.View.InvalidateTextMetrics("PasswordBox text layout property changed");
        }
        else if (ReferenceEquals(property, CaretBrushProperty) ||
            ReferenceEquals(property, SelectionBackgroundProperty) ||
            ReferenceEquals(property, IsEnabledProperty))
        {
            textInput.View.InvalidateTextRender("PasswordBox text render property changed");
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

    // The mask has one character per text element, while editor positions are UTF-16 offsets
    // into Password. A position inside a text element maps to that element's start.
    int ITextInputHost.GetDisplayIndex(int textPosition)
    {
        if (textPosition >= Password.Length)
        {
            return CountTextElements(Password);
        }

        int[] starts = StringInfo.ParseCombiningCharacters(Password);
        int index = 0;
        while (index + 1 < starts.Length && starts[index + 1] <= textPosition)
        {
            index++;
        }

        return index;
    }

    int ITextInputHost.GetTextPosition(int displayIndex)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(Password);
        if (displayIndex >= starts.Length)
        {
            return Password.Length;
        }

        return displayIndex <= 0 ? 0 : starts[displayIndex];
    }

    Brush ITextInputHost.CaretBrush => CaretBrush;

    Color ITextInputHost.SelectionBackground => SelectionBackground;

    bool ITextInputHost.AllowsEditing => true;

    bool ITextInputHost.AcceptsReturn => false;

    bool ITextInputHost.AcceptsTab => false;

    int ITextInputHost.MaxLength => 0;

    CharacterCasing ITextInputHost.CharacterCasing => CharacterCasing.Normal;

    TextWrapping ITextInputHost.TextWrapping => TextWrapping.NoWrap;

    bool ITextInputHost.CentersSingleLineContentVertically => false;

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
        SetFrameworkDefault(ComponentTemplateProperty, TextInputTemplates.PasswordBox);
        SetFrameworkDefault(PaddingProperty, new Thickness(4, 2, 4, 2));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(120, 130, 145)));
        SetFrameworkDefault(BackgroundProperty, new SolidColorBrush(Color.White));
        Cursor = Cerneala.UI.Input.Cursor.IBeam;
    }

    private static int CountTextElements(string text)
    {
        return text.Length == 0 ? 0 : StringInfo.ParseCombiningCharacters(text).Length;
    }
}
