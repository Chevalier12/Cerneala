namespace Cerneala.VisualStudio.Preview;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

internal sealed class CernealaPreviewModeBar : Border, IDisposable
{
    private readonly CernealaPreviewSession session;
    private readonly List<Button> modeButtons = new();
    private readonly List<Button> orientationButtons = new();
    private readonly TextBlock status = new();
    private bool disposed;

    public CernealaPreviewModeBar(CernealaPreviewSession session)
    {
        this.session = session;
        Height = 31;
        Background = CernealaPreviewChrome.ToolbarBrush;
        BorderBrush = CernealaPreviewChrome.BorderBrush;
        BorderThickness = new Thickness(0, 1, 0, 0);
        Child = BuildContent();
        session.Changed += OnSessionChanged;
        ApplySessionState();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        session.Changed -= OnSessionChanged;
    }

    private UIElement BuildContent()
    {
        DockPanel panel = new() { LastChildFill = true, Margin = new Thickness(6, 0, 6, 0) };

        StackPanel modes = new() { Orientation = Orientation.Horizontal };
        modes.Children.Add(CreateModeButton("Design", PreviewViewMode.Design));
        modes.Children.Add(CreateModeButton("Split", PreviewViewMode.Split));
        modes.Children.Add(CreateModeButton("Code", PreviewViewMode.Code));
        DockPanel.SetDock(modes, Dock.Left);
        panel.Children.Add(modes);

        StackPanel commands = new() { Orientation = Orientation.Horizontal };
        commands.Children.Add(CernealaPreviewChrome.Label("Split"));
        commands.Children.Add(CreateOrientationButton("H", "Horizontal split", PreviewSplitOrientation.Horizontal));
        commands.Children.Add(CreateOrientationButton("V", "Vertical split", PreviewSplitOrientation.Vertical));
        commands.Children.Add(CernealaPreviewChrome.Separator());
        Button refresh = CernealaPreviewChrome.Button("Refresh", "Recompile and refresh Live Preview", 58);
        refresh.Click += (_, _) => session.Refresh();
        commands.Children.Add(refresh);
        DockPanel.SetDock(commands, Dock.Right);
        panel.Children.Add(commands);

        status.VerticalAlignment = VerticalAlignment.Center;
        status.Margin = new Thickness(10, 0, 10, 0);
        status.Foreground = CernealaPreviewChrome.MutedTextBrush;
        status.FontSize = 11;
        status.TextTrimming = TextTrimming.CharacterEllipsis;
        panel.Children.Add(status);
        return panel;
    }

    private Button CreateModeButton(string label, PreviewViewMode mode)
    {
        Button button = CernealaPreviewChrome.Button(label, $"Show {label.ToLowerInvariant()} view", 62);
        button.Tag = mode;
        button.Click += (_, _) => session.SetMode(mode);
        modeButtons.Add(button);
        return button;
    }

    private Button CreateOrientationButton(
        string label,
        string toolTip,
        PreviewSplitOrientation orientation)
    {
        Button button = CernealaPreviewChrome.Button(label, toolTip, 28);
        button.Tag = orientation;
        button.Click += (_, _) =>
        {
            session.SetOrientation(orientation);
            if (session.Mode == PreviewViewMode.Code)
            {
                session.SetMode(PreviewViewMode.Split);
            }
        };
        orientationButtons.Add(button);
        return button;
    }

    private void OnSessionChanged(object? sender, EventArgs args) => ApplySessionState();

    private void ApplySessionState()
    {
        if (disposed)
        {
            return;
        }

        status.Text = session.Status;
        UpdateSelection(modeButtons, session.Mode);
        UpdateSelection(orientationButtons, session.Orientation);
    }

    private static void UpdateSelection<T>(IEnumerable<Button> buttons, T selected)
    {
        foreach (Button button in buttons)
        {
            bool isSelected = Equals(button.Tag, selected);
            button.Background = isSelected
                ? CernealaPreviewChrome.Brush(62, 62, 64)
                : Brushes.Transparent;
            button.BorderBrush = isSelected
                ? CernealaPreviewChrome.AccentBrush
                : CernealaPreviewChrome.BorderBrush;
        }
    }
}
