using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaloniaOracle;

public sealed class MainWindow : Window
{
    private readonly TextBlock textBlock;

    public MainWindow(string fontFamily)
    {
        Title = "Avalonia text oracle";
        Width = 320;
        Height = 160;
        Background = Brushes.White;
        textBlock = new TextBlock
        {
            Text = "Hello world!",
            FontFamily = new FontFamily(fontFamily),
            FontSize = 16,
            Foreground = Brushes.Black,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Content = textBlock;
    }

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        PixelSize pixelSize = PixelSize.FromSize(Bounds.Size, RenderScaling);
        using RenderTargetBitmap bitmap = new(pixelSize, new Vector(96 * RenderScaling, 96 * RenderScaling));
        bitmap.Render(this);

        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        bitmap.Save(fullPath);

        string metricsPath = Path.ChangeExtension(fullPath, ".metrics.txt");
        var layout = textBlock.TextLayout;
        var line = layout.TextLines[0];
        File.WriteAllLines(metricsPath,
        [
            $"Bounds={textBlock.Bounds}",
            $"DesiredSize={textBlock.DesiredSize}",
            $"Layout.Width={layout.Width:R}",
            $"Layout.Height={layout.Height:R}",
            $"Layout.Baseline={layout.Baseline:R}",
            $"Line.Height={line.Height:R}",
            $"Line.Baseline={line.Baseline:R}",
            $"BaselineOffset={textBlock.BaselineOffset:R}"
        ]);
    }
}
