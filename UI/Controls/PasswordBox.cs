using System.Globalization;

using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public class PasswordBox : TextBoxBase
{
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

    private static int CountTextElements(string text)
    {
        return text.Length == 0 ? 0 : StringInfo.ParseCombiningCharacters(text).Length;
    }
}
