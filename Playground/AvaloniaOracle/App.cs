using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AvaloniaOracle;

public sealed class App : Application
{
    internal static string? ScreenshotPath { get; set; }

    internal static string FontFamily { get; set; } = "Arial";

    internal static string Text { get; set; } = "Hello world!";

    internal static double FontSize { get; set; } = 16;

    internal static bool SemiBold { get; set; }

    internal static bool ShowSvg { get; set; }

    internal static string? Scenario { get; set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Window window = ShowSvg
                ? new SvgWindow()
                : string.Equals(Scenario, "motion-lab-header", StringComparison.OrdinalIgnoreCase)
                    ? new MotionLabHeaderWindow()
                    : new MainWindow(Text, FontFamily, FontSize, SemiBold);
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
                            else if (window is MotionLabHeaderWindow motionLabHeaderWindow)
                            {
                                motionLabHeaderWindow.SaveScreenshot(ScreenshotPath);
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
