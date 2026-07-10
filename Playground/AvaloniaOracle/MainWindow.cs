using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaloniaOracle;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Avalonia text oracle";
        Width = 320;
        Height = 160;
        Background = Brushes.White;
        Content = new TextBlock
        {
            Text = "Hello world!",
            FontFamily = new FontFamily("Arial"),
            FontSize = 16,
            Foreground = Brushes.Black,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
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
    }
}
