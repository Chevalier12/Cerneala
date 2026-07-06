using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Accessibility;

public static class AccessibleName
{
    public static readonly UiProperty<string?> NameProperty = UiProperty<string?>.Register(
        "AccessibleName",
        typeof(AccessibleName),
        new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsSemantics, coerceValue: (_, value) => string.IsNullOrWhiteSpace(value) ? null : value));

    public static string? GetName(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        string? explicitName = element.GetValue(NameProperty);
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return explicitName;
        }

        return GetContentText(element);
    }

    public static void SetName(UIElement element, string? name)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(NameProperty, name);
    }

    public static string? GetContentText(object? content)
    {
        return content switch
        {
            string text when !string.IsNullOrWhiteSpace(text) => text,
            TextBlock textBlock when !string.IsNullOrWhiteSpace(textBlock.Text) => textBlock.Text,
            Button button => GetContentText(button.Content),
            ContentControl contentControl => GetContentText(contentControl.Content),
            ContentPresenter presenter => GetContentText(presenter.Content),
            _ => null
        };
    }
}
