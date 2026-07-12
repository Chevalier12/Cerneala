using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace AvaloniaOracle;

public sealed class SvgWindow : Window
{
    public SvgWindow()
    {
        Title = "Avalonia SVG oracle";
        Width = 420;
        Height = 420;
        Background = new SolidColorBrush(Color.Parse("#F4F4F4"));

        Canvas canvas = new() { Width = 72, Height = 72 };
        foreach (XElement element in XDocument.Load(Path.Combine(AppContext.BaseDirectory, "fxemoji.svg")).Descendants().Where(node => node.Name.LocalName == "path"))
        {
            canvas.Children.Add(new AvaloniaPath
            {
                Fill = new SolidColorBrush(Color.Parse((string)element.Attribute("fill")!)),
                Data = Geometry.Parse((string)element.Attribute("d")!)
            });
        }

        Content = new Viewbox
        {
            // Match Cerneala's 52/53 physical-pixel layout rounding at 125% DPI.
            Margin = new Thickness(41.6, 41.6, 42.4, 42.4),
            Stretch = Stretch.Uniform,
            Child = canvas
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
