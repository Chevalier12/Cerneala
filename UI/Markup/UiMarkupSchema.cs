using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

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
                    "RepeatButton",
                    () => new RepeatButton(),
                    (element, child) => ((RepeatButton)element).Content = child,
                    nameof(RepeatButton.Content))
                .RegisterProperty(new UiMarkupPropertyRegistration(nameof(RepeatButton.Content), (element, value) => ((RepeatButton)element).Content = value))
                .RegisterProperty(new UiMarkupPropertyRegistration(nameof(RepeatButton.Delay), (element, value) => ((RepeatButton)element).Delay = ParseInt(value)))
                .RegisterProperty(new UiMarkupPropertyRegistration(nameof(RepeatButton.Interval), (element, value) => ((RepeatButton)element).Interval = ParseInt(value)))))
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
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.Background), (element, value) => ((Control)element).Background = ParseBrush((Control)element, value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.Foreground), (element, value) => ((Control)element).Foreground = ParseBrush((Control)element, value)))
            .RegisterProperty(new UiMarkupPropertyRegistration(nameof(Control.BorderBrush), (element, value) => ((Control)element).BorderBrush = ParseBrush((Control)element, value)))
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

    private static int ParseInt(string value)
    {
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a valid Int32 value.");
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

    private static Color ParseColor(string value)
    {
        return Color.TryParse(value, out Color parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a valid color. Use a WPF named color, #RRGGBB, #AARRGGBB, or RGB/RGBA byte values.");
    }

    private static Brush ParseBrush(Control element, string value)
    {
        if (value.StartsWith('$') && value.Length > 1)
        {
            return element.FindResource<Brush>(value[1..]);
        }

        return new SolidColorBrush(ParseColor(value));
    }
}
