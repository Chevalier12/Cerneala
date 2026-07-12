using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AvaloniaOracle;

public sealed class App : Application
{
    internal static string? ScreenshotPath { get; set; }

    internal static string FontFamily { get; set; } = "Arial";

    internal static bool ShowSvg { get; set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Window window = ShowSvg ? new SvgWindow() : new MainWindow(FontFamily);
            desktop.MainWindow = window;

            if (ScreenshotPath is not null)
            {
                window.Opened += (_, _) =>
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            if (window is SvgWindow svgWindow)
                            {
                                svgWindow.SaveScreenshot(ScreenshotPath);
                            }
                            else
                            {
                                ((MainWindow)window).SaveScreenshot(ScreenshotPath);
                            }
                            desktop.Shutdown();
                        },
                        DispatcherPriority.Background);
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
