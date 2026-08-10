using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public static class TextSearch
{
    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        "Text",
        typeof(TextSearch),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.None,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<string> TextPathProperty = UiProperty<string>.Register(
        "TextPath",
        typeof(TextSearch),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.None,
            coerceValue: (_, value) => value ?? string.Empty));

    public static string GetText(UiObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(TextProperty);
    }

    public static void SetText(UiObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TextProperty, value ?? string.Empty);
    }

    public static string GetTextPath(UiObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(TextPathProperty);
    }

    public static void SetTextPath(UiObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TextPathProperty, value ?? string.Empty);
    }
}
