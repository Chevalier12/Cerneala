namespace Cerneala.VisualStudio.Preview;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

internal static class CernealaPreviewChrome
{
    public static readonly SolidColorBrush SurfaceBrush = Brush(30, 30, 30);
    public static readonly SolidColorBrush ToolbarBrush = Brush(37, 37, 38);
    public static readonly SolidColorBrush BorderBrush = Brush(63, 63, 66);
    public static readonly SolidColorBrush TextBrush = Brush(225, 225, 225);
    public static readonly SolidColorBrush MutedTextBrush = Brush(170, 170, 170);
    public static readonly SolidColorBrush AccentBrush = Brush(0, 122, 204);

    public static Button Button(string label, string toolTip, double minWidth = 28) => new()
    {
        Content = label,
        ToolTip = toolTip,
        Height = 24,
        MinWidth = minWidth,
        Margin = new Thickness(1, 3, 1, 3),
        Padding = new Thickness(7, 0, 7, 0),
        Background = Brushes.Transparent,
        Foreground = TextBrush,
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(1),
        FontSize = 11,
        Focusable = false,
        Template = CreateButtonTemplate()
    };

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(5, 0, 4, 0),
        Foreground = MutedTextBrush,
        FontSize = 11
    };

    public static void ConfigureTextBox(TextBox textBox, double width)
    {
        ConfigureCompactInput(textBox, width);
        textBox.Padding = new Thickness(4, 0, 4, 0);
        textBox.CaretBrush = TextBrush;
        textBox.SelectionBrush = AccentBrush;
    }

    public static void ConfigureComboBox(ComboBox comboBox, double width)
    {
        comboBox.IsEditable = true;
        ConfigureCompactInput(comboBox, width);
        comboBox.ItemContainerStyle = CreateComboBoxItemStyle();
        comboBox.Template = CreateComboBoxTemplate();
    }

    public static Border Separator() => new()
    {
        Width = 1,
        Height = 16,
        Margin = new Thickness(6, 7, 6, 7),
        Background = BorderBrush
    };

    public static SolidColorBrush Brush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static void ConfigureCompactInput(Control input, double width)
    {
        input.Width = width;
        input.Height = 24;
        input.Margin = new Thickness(1, 3, 1, 3);
        input.VerticalAlignment = VerticalAlignment.Center;
        input.VerticalContentAlignment = VerticalAlignment.Center;
        input.Background = SurfaceBrush;
        input.Foreground = TextBrush;
        input.BorderBrush = BorderBrush;
        input.BorderThickness = new Thickness(1);
        input.FontSize = 11;
    }

    private static ControlTemplate CreateButtonTemplate()
    {
        FrameworkElementFactory chrome = new(typeof(Border)) { Name = "ButtonChrome" };
        chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        FrameworkElementFactory content = new(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        content.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(
            TextElement.ForegroundProperty,
            new TemplateBindingExtension(Control.ForegroundProperty));
        chrome.AppendChild(content);

        ControlTemplate template = new(typeof(Button)) { VisualTree = chrome };
        template.Triggers.Add(CreateNamedTrigger(
            UIElement.IsMouseOverProperty,
            true,
            Border.BackgroundProperty,
            Brush(52, 52, 55),
            "ButtonChrome"));
        template.Triggers.Add(CreateNamedTrigger(
            ButtonBase.IsPressedProperty,
            true,
            Border.BackgroundProperty,
            Brush(62, 62, 65),
            "ButtonChrome"));
        template.Triggers.Add(CreateNamedTrigger(
            UIElement.IsEnabledProperty,
            false,
            UIElement.OpacityProperty,
            0.55,
            "ButtonChrome"));
        return template;
    }

    private static ControlTemplate CreateComboBoxTemplate()
    {
        FrameworkElementFactory root = new(typeof(Grid));

        FrameworkElementFactory chrome = new(typeof(Border)) { Name = "Chrome" };
        chrome.SetValue(Border.BackgroundProperty, SurfaceBrush);
        chrome.SetValue(Border.BorderBrushProperty, BorderBrush);
        chrome.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        FrameworkElementFactory layout = new(typeof(DockPanel));
        FrameworkElementFactory toggle = new(typeof(ToggleButton)) { Name = "DropDownToggle" };
        toggle.SetValue(FrameworkElement.WidthProperty, 20d);
        toggle.SetValue(DockPanel.DockProperty, Dock.Right);
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
        toggle.SetValue(Control.TemplateProperty, CreateDropDownToggleTemplate());
        toggle.SetBinding(
            ToggleButton.IsCheckedProperty,
            new Binding(nameof(ComboBox.IsDropDownOpen))
            {
                RelativeSource = RelativeSource.TemplatedParent,
                Mode = BindingMode.TwoWay
            });
        layout.AppendChild(toggle);

        FrameworkElementFactory editor = new(typeof(TextBox)) { Name = "PART_EditableTextBox" };
        editor.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        editor.SetValue(Control.ForegroundProperty, TextBrush);
        editor.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        editor.SetValue(Control.PaddingProperty, new Thickness(6, 0, 4, 0));
        editor.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        editor.SetValue(Control.FontSizeProperty, 11d);
        editor.SetValue(TextBoxBase.CaretBrushProperty, TextBrush);
        editor.SetValue(TextBoxBase.SelectionBrushProperty, AccentBrush);
        editor.SetBinding(
            TextBoxBase.IsReadOnlyProperty,
            new Binding(nameof(ComboBox.IsReadOnly))
            {
                RelativeSource = RelativeSource.TemplatedParent
            });
        layout.AppendChild(editor);
        chrome.AppendChild(layout);
        root.AppendChild(chrome);

        FrameworkElementFactory popup = new(typeof(Popup)) { Name = "PART_Popup" };
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(UIElement.FocusableProperty, false);
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
        popup.SetBinding(
            Popup.IsOpenProperty,
            new Binding(nameof(ComboBox.IsDropDownOpen))
            {
                RelativeSource = RelativeSource.TemplatedParent
            });
        popup.SetBinding(
            Popup.PlacementTargetProperty,
            new Binding
            {
                RelativeSource = RelativeSource.TemplatedParent
            });

        FrameworkElementFactory popupChrome = new(typeof(Border));
        popupChrome.SetValue(Border.BackgroundProperty, ToolbarBrush);
        popupChrome.SetValue(Border.BorderBrushProperty, BorderBrush);
        popupChrome.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupChrome.SetValue(Border.PaddingProperty, new Thickness(1));
        popupChrome.SetValue(FrameworkElement.MaxHeightProperty, 260d);
        popupChrome.SetBinding(
            FrameworkElement.MinWidthProperty,
            new Binding(nameof(FrameworkElement.ActualWidth))
            {
                RelativeSource = RelativeSource.TemplatedParent
            });

        FrameworkElementFactory scrollViewer = new(typeof(ScrollViewer));
        scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);
        scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scrollViewer.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
        popupChrome.AppendChild(scrollViewer);
        popup.AppendChild(popupChrome);
        root.AppendChild(popup);

        ControlTemplate template = new(typeof(ComboBox)) { VisualTree = root };
        template.Triggers.Add(CreateNamedTrigger(
            UIElement.IsKeyboardFocusWithinProperty,
            true,
            Border.BorderBrushProperty,
            AccentBrush,
            "Chrome"));
        template.Triggers.Add(CreateNamedTrigger(
            ComboBox.IsDropDownOpenProperty,
            true,
            Border.BorderBrushProperty,
            AccentBrush,
            "Chrome"));
        template.Triggers.Add(CreateNamedTrigger(
            UIElement.IsEnabledProperty,
            false,
            UIElement.OpacityProperty,
            0.55,
            "Chrome"));
        return template;
    }

    private static ControlTemplate CreateDropDownToggleTemplate()
    {
        FrameworkElementFactory chrome = new(typeof(Border)) { Name = "ToggleChrome" };
        chrome.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        chrome.SetValue(Border.BorderBrushProperty, BorderBrush);
        chrome.SetValue(Border.BorderThicknessProperty, new Thickness(1, 0, 0, 0));

        FrameworkElementFactory arrow = new(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "\u25BE");
        arrow.SetValue(TextBlock.ForegroundProperty, MutedTextBrush);
        arrow.SetValue(TextBlock.FontSizeProperty, 9d);
        arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        chrome.AppendChild(arrow);

        ControlTemplate template = new(typeof(ToggleButton)) { VisualTree = chrome };
        template.Triggers.Add(CreateNamedTrigger(
            UIElement.IsMouseOverProperty,
            true,
            Border.BackgroundProperty,
            Brush(52, 52, 55),
            "ToggleChrome"));
        template.Triggers.Add(CreateNamedTrigger(
            ToggleButton.IsCheckedProperty,
            true,
            Border.BackgroundProperty,
            Brush(48, 48, 51),
            "ToggleChrome"));
        return template;
    }

    private static Style CreateComboBoxItemStyle()
    {
        FrameworkElementFactory chrome = new(typeof(Border)) { Name = "ItemChrome" };
        chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        FrameworkElementFactory content = new(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        chrome.AppendChild(content);

        ControlTemplate template = new(typeof(ComboBoxItem)) { VisualTree = chrome };
        template.Triggers.Add(CreateNamedTrigger(
            ComboBoxItem.IsSelectedProperty,
            true,
            Border.BackgroundProperty,
            Brush(49, 49, 52),
            "ItemChrome"));
        template.Triggers.Add(CreateNamedTrigger(
            ComboBoxItem.IsHighlightedProperty,
            true,
            Border.BackgroundProperty,
            AccentBrush,
            "ItemChrome"));

        Style style = new(typeof(ComboBoxItem));
        style.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, SurfaceBrush));
        style.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, TextBrush));
        style.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(7, 4, 7, 4)));
        style.Setters.Add(new Setter(ComboBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(ComboBoxItem.TemplateProperty, template));
        return style;
    }

    private static Trigger CreateNamedTrigger(
        DependencyProperty conditionProperty,
        object conditionValue,
        DependencyProperty targetProperty,
        object targetValue,
        string targetName)
    {
        Trigger trigger = new()
        {
            Property = conditionProperty,
            Value = conditionValue
        };
        trigger.Setters.Add(new Setter(targetProperty, targetValue, targetName));
        return trigger;
    }
}
