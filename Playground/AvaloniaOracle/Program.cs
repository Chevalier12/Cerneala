using Avalonia;

namespace AvaloniaOracle;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.ScreenshotPath = ReadScreenshotPath(args);
        App.FontFamily = ReadOption(args, "--font") ?? "Arial";
        App.ShowSvg = args.Contains("--svg", StringComparer.Ordinal);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    private static string? ReadScreenshotPath(string[] args)
        => ReadOption(args, "--screenshot") is { } path ? Path.GetFullPath(path) : null;

    private static string? ReadOption(string[] args, string option)
    {
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
