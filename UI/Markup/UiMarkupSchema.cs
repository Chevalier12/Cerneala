using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Markup;

public static class UiMarkupSchema
{
    public static UiMarkupTypeRegistry CreateDefault()
    {
        UiMarkupTypeRegistry registry = new();
        registry
            .Register(WithControlProperties(new UiMarkupElementRegistration(
                "Panel",
                () => new Panel(),
                AddLogicalAndVisualChild)))
            .Register(WithControlProperties(new UiMarkupElementRegistration(
                "StackPanel",
                () => new StackPanel(),
                AddLogicalAndVisualChild)))
            .Register(WithControlProperties(new UiMarkupElementRegistration(
                "Border",
                () => new Border(),
                (element, child) => ((Border)element).Child = child,
                nameof(Border.Child))))
            .Register(WithControlProperties(new UiMarkupElementRegistration(
                    "Button",
                    () => new Button(),
                    (element, child) => ((Button)element).Content = child,
                    nameof(Button.Content))
                .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Button.Content), (element, value) => ((Button)element).Content = value))))
            .Register(WithControlProperties(new UiMarkupElementRegistration(
                    "TextBlock",
                    () => new TextBlock(),
                    contentPropertyName: nameof(TextBlock.Text))
                .RegisterProperty(new UiMarkupPropertyRegistration(nameof(TextBlock.Text), (element, value) => ((TextBlock)element).Text = value))));

        return registry;
    }

    private static UiMarkupElementRegistration WithControlProperties(UiMarkupElementRegistration registration)
    {
        return registration
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(UIElement.IsEnabled), (element, value) => element.IsEnabled = ParseBool(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(UIElement.IsVisible), (element, value) => element.IsVisible = ParseBool(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(UIElement.Margin), (element, value) => element.Margin = ParseThickness(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.Background), (element, value) => ((Control)element).Background = ParseColor(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.Foreground), (element, value) => ((Control)element).Foreground = ParseColor(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.BorderColor), (element, value) => ((Control)element).BorderColor = ParseColor(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.BorderThickness), (element, value) => ((Control)element).BorderThickness = ParseThickness(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.Padding), (element, value) => ((Control)element).Padding = ParseThickness(value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.FontFamily), (element, value) => ((Control)element).FontFamily = value))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.FontSize), (element, value) => ((Control)element).FontSize = ParseFloat(value)));
    }

    private static void AddLogicalAndVisualChild(UIElement parent, UIElement child)
    {
        parent.LogicalChildren.Add(child);
        parent.VisualChildren.Add(child);
    }

    private static bool ParseBool(string value)
    {
        return bool.TryParse(value, out bool parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a valid Boolean value.");
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a valid Single value.");
    }

    private static Thickness ParseThickness(string value)
    {
        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => new Thickness(ParseFloat(parts[0])),
            4 => new Thickness(ParseFloat(parts[0]), ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])),
            _ => throw new FormatException("Thickness must use one value or four comma-separated values.")
        };
    }

    private static DrawColor ParseColor(string value)
    {
        if (string.Equals(value, nameof(DrawColor.Transparent), StringComparison.OrdinalIgnoreCase))
        {
            return DrawColor.Transparent;
        }

        if (string.Equals(value, nameof(DrawColor.White), StringComparison.OrdinalIgnoreCase))
        {
            return DrawColor.White;
        }

        if (string.Equals(value, nameof(DrawColor.Black), StringComparison.OrdinalIgnoreCase))
        {
            return DrawColor.Black;
        }

        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length is not (3 or 4))
        {
            throw new FormatException("DrawColor must use a named color or three/four comma-separated byte values.");
        }

        return new DrawColor(ParseByte(parts[0]), ParseByte(parts[1]), ParseByte(parts[2]), parts.Length == 4 ? ParseByte(parts[3]) : (byte)255);
    }

    private static byte ParseByte(string value)
    {
        return byte.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out byte parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a valid byte value.");
    }
}
