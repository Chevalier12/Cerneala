using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AvaloniaOracle;

public sealed class App : Application
{
    internal static string? ScreenshotPath { get; set; }

    internal static string FontFamily { get; set; } = "Arial";

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow window = new(FontFamily);
            desktop.MainWindow = window;

            if (ScreenshotPath is not null)
            {
                window.Opened += (_, _) =>
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            window.SaveScreenshot(ScreenshotPath);
                            desktop.Shutdown();
                        },
                        DispatcherPriority.Background);
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
