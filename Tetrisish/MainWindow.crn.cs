using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.Tetris;

public partial class MainWindow : Window
{
    private long displayedStateVersion = -1;

    private void OnPreviewKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs)
        {
            return;
        }

        bool handled = keyArgs.Key switch
        {
            InputKey.Left => GameSurface.MoveHorizontal(-1),
            InputKey.Right => GameSurface.MoveHorizontal(1),
            InputKey.Down => GameSurface.BeginSoftDrop(),
            InputKey.Up or InputKey.X => GameSurface.RotateClockwise(),
            InputKey.Z => GameSurface.RotateCounterClockwise(),
            InputKey.Space => GameSurface.HardDrop(),
            InputKey.C => GameSurface.Hold(),
            InputKey.P or InputKey.Escape => GameSurface.TogglePause(),
            InputKey.R => GameSurface.Restart(),
            _ => false
        };

        if (handled)
        {
            args.Handled = true;
            RefreshStatus(force: true);
        }
    }

    private void OnPreviewKeyUp(UiElementId sender, RoutedEventArgs args)
    {
        if (args is KeyEventArgs { Key: InputKey.Down })
        {
            GameSurface.EndSoftDrop();
            args.Handled = true;
        }
    }

    private void OnDeactivated(object? sender, EventArgs args)
    {
        GameSurface.EndSoftDrop();
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        RefreshStatus(force: false);
    }

    private void RefreshStatus(bool force)
    {
        if (!force && displayedStateVersion == GameSurface.StateVersion)
        {
            return;
        }

        displayedStateVersion = GameSurface.StateVersion;
        ScoreText.Text = GameSurface.Score.ToString("N0");
        LinesText.Text = GameSurface.Lines.ToString();
        LevelText.Text = GameSurface.Level.ToString();
        NextText.Text = GameSurface.NextKind.ToString();
        HoldText.Text = GameSurface.HeldKind?.ToString() ?? "—";

        if (GameSurface.IsGameOver)
        {
            OverlayText.Text = "GAME OVER";
            StatusText.Text = "Game over — press R to restart";
            StatusText.Foreground = new UI.Media.SolidColorBrush(new Drawing.Color(255, 105, 126));
        }
        else if (GameSurface.IsPaused)
        {
            OverlayText.Text = "PAUSED";
            StatusText.Text = "Paused — press P or Escape to continue";
            StatusText.Foreground = new UI.Media.SolidColorBrush(new Drawing.Color(255, 214, 102));
        }
        else
        {
            OverlayText.Text = string.Empty;
            StatusText.Text = "Playing";
            StatusText.Foreground = new UI.Media.SolidColorBrush(new Drawing.Color(145, 255, 199));
        }
    }

}
