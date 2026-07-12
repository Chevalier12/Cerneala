using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using SvgPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.Playground;

public sealed class SvgWindow : Window
{
    public SvgWindow(string svgPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(svgPath);

        Panel paths = new();
        DrawRect viewBox = new(0, 0, 72, 72);
        foreach (XElement element in XDocument.Load(svgPath).Descendants().Where(node => node.Name.LocalName == "path"))
        {
            paths.VisualChildren.Add(new SvgPath
            {
                Geometry = new SvgGeometry((string)element.Attribute("d")!, viewBox),
                Fill = new SolidColorBrush(ParseColor((string)element.Attribute("fill")!))
            });
        }

        Title = "Cerneala SVG oracle";
        Width = 420;
        Height = 420;
        MinWidth = 420;
        MinHeight = 420;
        MaxWidth = 420;
        MaxHeight = 420;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.Transparent);
        BorderBrush = new SolidColorBrush(Color.Transparent);
        BorderThickness = Thickness.Zero;
        Padding = Thickness.Zero;
        Content = new Border
        {
            Background = new SolidColorBrush(new Color(244, 244, 244)),
            Padding = new Thickness(42),
            Child = paths
        };
    }

    private static Color ParseColor(string value)
    {
        string hex = value.TrimStart('#');
        return new Color(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }
}
