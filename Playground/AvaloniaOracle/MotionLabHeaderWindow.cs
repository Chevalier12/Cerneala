using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaloniaOracle;

public sealed class MotionLabHeaderWindow : Window
{
    private static readonly Color PanelColor = Color.Parse("#101217");
    private readonly Border canvas;
    private readonly TextBlock label;

    public MotionLabHeaderWindow()
    {
        Title = "Avalonia Motion Lab header oracle";
        Width = 900;
        Height = 68;
        MinWidth = 1;
        MinHeight = 1;
        Background = new SolidColorBrush(PanelColor);

        label = new TextBlock
        {
            Text = "MOTION LAB / SECOND NATIVE WINDOW",
            FontFamily = new FontFamily("Cascadia Mono"),
            FontSize = 10,
            FontWeight = FontWeight.Normal,
            Foreground = new SolidColorBrush(Color.Parse("#8A93A6")),
            Margin = new Thickness(0, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        Border close = new()
        {
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.Parse("#424754")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 10),
            Child = new TextBlock
            {
                Text = "CLOSE",
                FontFamily = new FontFamily("Cascadia Mono"),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#EDEFF3")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        StackPanel right = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { label, close }
        };

        canvas = new Border
        {
            Background = new SolidColorBrush(PanelColor),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A2E38")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(24, 0),
            Child = right
        };
        Content = canvas;
    }

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        PixelSize pixelSize = PixelSize.FromSize(Bounds.Size, RenderScaling);
        using RenderTargetBitmap bitmap = new(pixelSize, new Vector(96 * RenderScaling, 96 * RenderScaling));
        bitmap.Render(canvas);

        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        bitmap.Save(fullPath);
        File.WriteAllLines(Path.ChangeExtension(fullPath, ".metrics.txt"),
        [
            "Scenario=motion-lab-header",
            $"RenderScaling={RenderScaling:R}",
            $"Text={label.Text}",
            $"FontFamily={label.FontFamily}",
            $"FontSize={label.FontSize:R}",
            $"FontWeight={label.FontWeight}",
            $"DesiredSize={label.DesiredSize}",
            $"Bounds={label.Bounds}"
        ]);
    }
}
