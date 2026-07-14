using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using ColumnDefinition = Cerneala.UI.Layout.Panels.ColumnDefinition;
using Grid = Cerneala.UI.Layout.Panels.Grid;
using GridLength = Cerneala.UI.Layout.Panels.GridLength;
using RowDefinition = Cerneala.UI.Layout.Panels.RowDefinition;

namespace Cerneala.Playground;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush ShowcaseCellBrush = new(new Color(31, 39, 47));
    private static readonly SolidColorBrush ShowcaseAlternateCellBrush = new(new Color(23, 76, 66));
    private static readonly SolidColorBrush ShowcaseBorderBrush = new(new Color(52, 60, 70));
    private static readonly SolidColorBrush ShowcaseTextBrush = new(new Color(242, 244, 245));

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null)
        {
            return;
        }

        FrameDiagnosticsText.Text =
            $"{LastFrame.ElapsedTime.TotalMilliseconds:0.00} ms | {FrameDiagnostics.Format(LastFrame.Stats)}";
    }

    private void OnShowcaseSelected(object? sender, ShowcaseSelectedEventArgs args)
    {
        SelectedShowcaseText.Text = args.ShowcaseName;
        if (args.ShowcaseName == "ScrollViewer")
        {
            MountShowcase(CreateScrollViewerShowcase());
            return;
        }

        EmptyStateTitle.Text = args.ShowcaseName;
        EmptyStateDescription.Text = "View-ul separat pentru acest showcase va fi montat aici.";
        MountShowcase(EmptyState);
    }

    private void MountShowcase(UIElement showcase)
    {
        while (ShowcaseHost.VisualChildren.Count > 0)
        {
            ShowcaseHost.VisualChildren.Remove(ShowcaseHost.VisualChildren[0]);
        }

        while (ShowcaseHost.LogicalChildren.Count > 0)
        {
            ShowcaseHost.LogicalChildren.Remove(ShowcaseHost.LogicalChildren[0]);
        }

        ShowcaseHost.VisualChildren.Add(showcase);
    }

    private static ScrollViewer CreateScrollViewerShowcase()
    {
        const int columnCount = 8;
        const int rowCount = 20;
        Grid content = new();
        for (int column = 0; column < columnCount; column++)
        {
            content.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(132)));
        }

        for (int row = 0; row < rowCount; row++)
        {
            content.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(48)));
            for (int column = 0; column < columnCount; column++)
            {
                TextBlock cell = new()
                {
                    Text = $"R{row + 1:00}  C{column + 1:00}",
                    FontFamily = "Consolas",
                    FontSize = 13,
                    Foreground = ShowcaseTextBrush,
                    Background = (row + column) % 3 == 0
                        ? ShowcaseAlternateCellBrush
                        : ShowcaseCellBrush,
                    BorderBrush = ShowcaseBorderBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(12, 14, 12, 10)
                };
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, column);
                content.VisualChildren.Add(cell);
            }
        }

        return new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Margin = new Thickness(24)
        };
    }
}

public sealed class ShowcaseNavigation : ScrollViewer
{
    private static readonly SolidColorBrush MutedTextBrush = new(new Cerneala.Drawing.Color(169, 177, 186));

    private static readonly (string Name, string[] Items)[] Sections =
    [
        ("CONTROALE",
        [
            "Border", "Button", "CheckBox", "ComboBox", "ContentControl", "Image", "InkCanvas",
            "ItemsControl", "Label", "ListBox", "PasswordBox", "ProgressBar", "RadioButton",
            "ScrollViewer", "Slider", "TabControl", "TextBlock", "TextBox", "ToolTip"
        ]),
        ("LAYOUT",
        [
            "Canvas", "Grid", "StackPanel", "VirtualizingStackPanel", "Layout boundaries", "Layout rounding"
        ]),
        ("ASPECT",
        [
            "Aspect cascade", "States and variants", "Tokens and themes", "Component templates", "Content templates"
        ]),
        ("MOTION",
        [
            "Tween and easing", "Spring and decay", "Keyframes", "Layout motion", "Presence",
            "Motion transactions", "Gesture motion"
        ]),
        ("SISTEME",
        [
            "Binding and observable data", "Brushes and drawing", "Shapes and geometry", "Text and typography",
            "Input and commands", "Focus and navigation", "Accessibility", "Resources", "Window lifecycle",
            "Runtime diagnostics"
        ])
    ];

    public ShowcaseNavigation()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Padding = new Thickness(14, 4, 14, 18);
        NavigationPanel = new StackPanel();
        Content = NavigationPanel;

        foreach ((string sectionName, string[] items) in Sections)
        {
            AddNavigationChild(CreateSectionLabel(sectionName));
            foreach (string item in items)
            {
                AddNavigationChild(CreateNavigationButton(item));
            }
        }
    }

    public event EventHandler<ShowcaseSelectedEventArgs>? ShowcaseSelected;

    public StackPanel NavigationPanel { get; }

    private void AddNavigationChild(Cerneala.UI.Elements.UIElement child)
    {
        NavigationPanel.LogicalChildren.Add(child);
        NavigationPanel.VisualChildren.Add(child);
    }

    private static TextBlock CreateSectionLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = "Segoe UI Semibold",
            FontSize = 11,
            Foreground = MutedTextBrush,
            Margin = new Thickness(4, 16, 4, 6)
        };
    }

    private Button CreateNavigationButton(string text)
    {
        Button button = new()
        {
            Content = text,
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 3)
        };
        button.Click += OnNavigationButtonClick;
        return button;
    }

    private void OnNavigationButtonClick(UiElementId sender, RoutedEventArgs args)
    {
        if (args.OriginalSource is Button { Content: string showcaseName })
        {
            ShowcaseSelected?.Invoke(this, new ShowcaseSelectedEventArgs(showcaseName));
        }
    }
}

public sealed class ShowcaseSelectedEventArgs : EventArgs
{
    public ShowcaseSelectedEventArgs(string showcaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(showcaseName);
        ShowcaseName = showcaseName;
    }

    public string ShowcaseName { get; }
}
