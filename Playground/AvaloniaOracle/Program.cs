using Avalonia;

namespace AvaloniaOracle;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.ScreenshotPath = ReadScreenshotPath(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    private static string? ReadScreenshotPath(string[] args)
    {
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], "--screenshot", StringComparison.Ordinal))
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        return null;
    }
}
