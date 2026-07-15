using Avalonia;
using System.Globalization;

namespace AvaloniaOracle;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.ScreenshotPath = ReadScreenshotPath(args);
        App.FontFamily = ReadOption(args, "--font") ?? "Arial";
        App.Text = ReadOption(args, "--text") ?? "Hello world!";
        App.FontSize = ReadFontSize(args);
        App.SemiBold = args.Contains("--semibold", StringComparer.Ordinal);
        App.ShowSvg = args.Contains("--svg", StringComparer.Ordinal);
        App.Scenario = ReadOption(args, "--scenario");
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

    private static double ReadFontSize(string[] args)
    {
        string? value = ReadOption(args, "--font-size");
        if (value is null)
        {
            return 16;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double fontSize) ||
            !double.IsFinite(fontSize) ||
            fontSize <= 0)
        {
            throw new ArgumentException($"Invalid --font-size value: '{value}'.");
        }

        return fontSize;
    }

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
