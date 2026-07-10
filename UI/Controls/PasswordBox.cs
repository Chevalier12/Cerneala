using System.Globalization;

using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public class PasswordBox : TextBoxBase
{
    public static readonly RoutedEvent PasswordChangedEvent = RoutedEventRegistry.Register(nameof(PasswordChanged), typeof(PasswordBox), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event RoutedEventHandler PasswordChanged { add => AddHandler(PasswordChangedEvent, value); remove => RemoveHandler(PasswordChangedEvent, value); }
    public static readonly UiProperty<char> PasswordCharProperty = UiProperty<char>.Register(
        nameof(PasswordChar),
        typeof(PasswordBox),
        new UiPropertyMetadata<char>('*', UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public string Password
    {
        get => Text;
        set => Text = value ?? string.Empty;
    }

    protected override string DisplayText => new(PasswordChar, CountTextElements(Text));

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, TextProperty))
        {
            RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, this));
        }
    }

    private static int CountTextElements(string text)
    {
        return text.Length == 0 ? 0 : StringInfo.ParseCombiningCharacters(text).Length;
    }
}
