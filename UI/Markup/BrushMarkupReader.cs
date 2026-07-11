using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.UI.Markup;

public sealed class BrushMarkupReader
{
    private readonly Func<string, IDrawImage?>? imageResolver;
    private readonly Func<string, UIElement?>? visualResolver;

    public BrushMarkupReader(
        Func<string, IDrawImage?>? imageResolver = null,
        Func<string, UIElement?>? visualResolver = null)
    {
        this.imageResolver = imageResolver;
        this.visualResolver = visualResolver;
    }

    public MarkupResult<Brush> Read(string markup)
    {
        List<MarkupDiagnostic> diagnostics = [];
        if (string.IsNullOrWhiteSpace(markup))
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP030", "Brush markup must contain a brush element."));
            return new MarkupResult<Brush>(null, diagnostics);
        }

        try
        {
            XDocument document = XDocument.Parse(markup, LoadOptions.SetLineInfo);
            Brush brush = ParseBrush(document.Root ?? throw new FormatException("Brush markup requires a root element."));
            return new MarkupResult<Brush>(brush, diagnostics);
        }
        catch (Exception ex) when (ex is XmlException or FormatException or ArgumentException or InvalidOperationException)
        {
            diagnostics.Add(MarkupDiagnostic.Error("MARKUP031", ex.Message));
            return new MarkupResult<Brush>(null, diagnostics);
        }
    }

    private Brush ParseBrush(XElement element)
    {
        float opacity = OptionalFloat(element, "Opacity", 1, value => value is >= 0 and <= 1);
        return element.Name.LocalName switch
        {
            "SolidColorBrush" => new SolidColorBrush(RequiredColor(element, "Color"), opacity),
            "LinearGradientBrush" => new LinearGradientBrush(
                RequiredPoint(element, "StartPoint"),
                RequiredPoint(element, "EndPoint"),
                RequiredStops(element),
                opacity),
            "RadialGradientBrush" => new RadialGradientBrush(
                RequiredPoint(element, "Center"),
                RequiredFloat(element, "RadiusX", value => value > 0),
                RequiredFloat(element, "RadiusY", value => value > 0),
                RequiredStops(element),
                opacity),
            "ImageBrush" => ParseImageBrush(element, opacity),
            "DrawingBrush" => ParseDrawingBrush(element, opacity),
            "VisualBrush" => ParseVisualBrush(element, opacity),
            _ => throw new FormatException($"Unknown brush element '{element.Name.LocalName}'.")
        };
    }

    private ImageBrush ParseImageBrush(XElement element, float opacity)
    {
        string source = RequiredString(element, "Source");
        TileOptions tile = ReadTileOptions(element, opacity);
        IDrawImage? image = imageResolver?.Invoke(source);
        return image is null
            ? new ImageBrush(source, tile.Stretch, tile.AlignmentX, tile.AlignmentY, tile.Viewport, tile.Viewbox, tile.TileMode, opacity)
            : new ImageBrush(image, tile.Stretch, tile.AlignmentX, tile.AlignmentY, tile.Viewport, tile.Viewbox, tile.TileMode, opacity);
    }

    private DrawingBrush ParseDrawingBrush(XElement element, float opacity)
    {
        DrawRect bounds = RequiredRect(element, "ContentBounds");
        DrawCommand[] commands = element.Elements().Select(ParseCommand).ToArray();
        if (commands.Length == 0)
        {
            throw new FormatException("DrawingBrush requires at least one drawing command.");
        }

        TileOptions tile = ReadTileOptions(element, opacity);
        return new DrawingBrush(commands, bounds, tile.Stretch, tile.AlignmentX, tile.AlignmentY, tile.Viewport, tile.Viewbox, tile.TileMode, opacity);
    }

    private VisualBrush ParseVisualBrush(XElement element, float opacity)
    {
        string source = RequiredString(element, "Source").TrimStart('$');
        UIElement visual = visualResolver?.Invoke(source)
            ?? throw new InvalidOperationException($"VisualBrush source '{source}' could not be resolved.");
        TileOptions tile = ReadTileOptions(element, opacity);
        return new VisualBrush(visual, tile.Stretch, tile.AlignmentX, tile.AlignmentY, tile.Viewport, tile.Viewbox, tile.TileMode, opacity);
    }

    private static DrawCommand ParseCommand(XElement element)
    {
        DrawRect rect = RequiredRect(element, "Rect");
        Color color = RequiredColor(element, "Color");
        return element.Name.LocalName switch
        {
            "FillRectangle" => DrawCommand.FillRectangle(rect, color),
            "FillEllipse" => DrawCommand.FillEllipse(rect, color),
            "DrawRectangle" => DrawCommand.DrawRectangle(rect, color, RequiredFloat(element, "Thickness", value => value > 0)),
            "DrawEllipse" => DrawCommand.DrawEllipse(rect, color, RequiredFloat(element, "Thickness", value => value > 0)),
            _ => throw new FormatException($"Unsupported DrawingBrush command '{element.Name.LocalName}'.")
        };
    }

    private static IReadOnlyList<GradientStop> RequiredStops(XElement element)
    {
        GradientStop[] stops = element.Elements("GradientStop")
            .Select(stop => new GradientStop(
                RequiredFloat(stop, "Offset", value => value is >= 0 and <= 1),
                RequiredColor(stop, "Color")))
            .ToArray();
        return stops.Length > 0
            ? stops
            : throw new FormatException($"{element.Name.LocalName} requires at least one GradientStop.");
    }

    private static TileOptions ReadTileOptions(XElement element, float opacity)
    {
        return new TileOptions(
            OptionalEnum(element, "Stretch", DrawBrushStretch.Fill),
            OptionalEnum(element, "AlignmentX", DrawBrushAlignmentX.Center),
            OptionalEnum(element, "AlignmentY", DrawBrushAlignmentY.Center),
            OptionalRect(element, "Viewport"),
            OptionalRect(element, "Viewbox"),
            OptionalEnum(element, "TileMode", DrawTileMode.None),
            opacity);
    }

    private static string RequiredString(XElement element, string name)
    {
        string value = element.Attribute(name)?.Value.Trim() ?? string.Empty;
        return value.Length > 0 ? value : throw new FormatException($"{element.Name.LocalName}.{name} is required.");
    }

    private static Color RequiredColor(XElement element, string name)
    {
        string value = RequiredString(element, name);
        return Color.TryParse(value, out Color color)
            ? color
            : throw new FormatException($"'{value}' is not a valid color.");
    }

    private static DrawPoint RequiredPoint(XElement element, string name)
    {
        float[] values = ParseFloatList(RequiredString(element, name), 2, name);
        return new DrawPoint(values[0], values[1]);
    }

    private static DrawRect RequiredRect(XElement element, string name)
    {
        float[] values = ParseFloatList(RequiredString(element, name), 4, name);
        return new DrawRect(values[0], values[1], values[2], values[3]);
    }

    private static DrawRect? OptionalRect(XElement element, string name)
    {
        return element.Attribute(name) is null ? null : RequiredRect(element, name);
    }

    private static float RequiredFloat(XElement element, string name, Func<float, bool> validate)
    {
        return OptionalFloat(element, name, float.NaN, validate);
    }

    private static float OptionalFloat(XElement element, string name, float defaultValue, Func<float, bool> validate)
    {
        XAttribute? attribute = element.Attribute(name);
        if (attribute is null && float.IsFinite(defaultValue))
        {
            return defaultValue;
        }

        if (attribute is null ||
            !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            !float.IsFinite(value) ||
            !validate(value))
        {
            throw new FormatException($"'{attribute?.Value}' is not a valid {element.Name.LocalName}.{name} value.");
        }

        return value;
    }

    private static TEnum OptionalEnum<TEnum>(XElement element, string name, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        string? value = element.Attribute(name)?.Value;
        return value is null
            ? defaultValue
            : Enum.TryParse(value, ignoreCase: false, out TEnum parsed)
                ? parsed
                : throw new FormatException($"'{value}' is not a valid {element.Name.LocalName}.{name} value.");
    }

    private static float[] ParseFloatList(string value, int count, string name)
    {
        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != count)
        {
            throw new FormatException($"{name} requires {count} comma-separated values.");
        }

        float[] result = new float[count];
        for (int i = 0; i < count; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]) || !float.IsFinite(result[i]))
            {
                throw new FormatException($"'{value}' is not a valid {name} value.");
            }
        }

        return result;
    }

    private readonly record struct TileOptions(
        DrawBrushStretch Stretch,
        DrawBrushAlignmentX AlignmentX,
        DrawBrushAlignmentY AlignmentY,
        DrawRect? Viewport,
        DrawRect? Viewbox,
        DrawTileMode TileMode,
        float Opacity);
}
