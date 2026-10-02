using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private string? BuildBrushExpression(MarkupElement brush)
        {
            return brush.Name.LocalName switch
            {
                "SolidColorBrush" => BuildSolidColorBrushExpression(brush, out _),
                "LinearGradientBrush" => BuildLinearGradientBrushExpression(brush),
                "RadialGradientBrush" => BuildRadialGradientBrushExpression(brush),
                "ImageBrush" => BuildImageBrushExpression(brush),
                "DrawingBrush" => BuildDrawingBrushExpression(brush),
                _ => null
            };
        }

        private string? BuildSolidColorBrushExpression(MarkupElement resource, out string? colorExpression)
        {
            colorExpression = ParseBrushColor(resource.Attribute("Color"));
            if (colorExpression is null)
            {
                if (resource.Name.LocalName == "SolidColorBrush")
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("Color") ?? resource, "SolidColorBrush", "Color", resource.Attribute("Color")?.Value ?? string.Empty);
                }

                return null;
            }

            if (resource.Attribute("Opacity") is null)
            {
                return "new global::Cerneala.UI.Media.SolidColorBrush(" + colorExpression + ")";
            }

            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return opacity is null ? null : "new global::Cerneala.UI.Media.SolidColorBrush(" + colorExpression + ", " + opacity + ")";
        }

        private string? BuildLinearGradientBrushExpression(MarkupElement resource)
        {
            string? start = ParseBrushPoint(resource, "StartPoint");
            string? end = ParseBrushPoint(resource, "EndPoint");
            string? stops = ParseGradientStops(resource);
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return start is null || end is null || stops is null || opacity is null
                ? null
                : "new global::Cerneala.UI.Media.LinearGradientBrush(" + start + ", " + end + ", " + stops + ", " + opacity + ")";
        }

        private string? BuildRadialGradientBrushExpression(MarkupElement resource)
        {
            string? center = ParseBrushPoint(resource, "Center");
            string? radiusX = ParseBrushFloat(resource, "RadiusX", 0, value => value > 0);
            string? radiusY = ParseBrushFloat(resource, "RadiusY", 0, value => value > 0);
            string? stops = ParseGradientStops(resource);
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return center is null || radiusX is null || radiusY is null || stops is null || opacity is null
                ? null
                : "new global::Cerneala.UI.Media.RadialGradientBrush(" + center + ", " + radiusX + ", " + radiusY + ", " + stops + ", " + opacity + ")";
        }

        private string? BuildImageBrushExpression(MarkupElement resource)
        {
            string source = resource.Attribute("Source")?.Value.Trim() ?? string.Empty;
            if (source.Length == 0)
            {
                Report(InvalidPropertyValue, resource, "ImageBrush", "Source", source);
                return null;
            }

            string? tileArguments = ParseTileArguments(resource);
            return tileArguments is null
                ? null
                : "new global::Cerneala.UI.Media.ImageBrush(" + Literal(source) + ", " + tileArguments + ")";
        }

        private string? BuildDrawingBrushExpression(MarkupElement resource)
        {
            string? bounds = ParseBrushRect(resource, "ContentBounds");
            if (bounds is null)
            {
                return null;
            }

            List<string> commands = [];
            foreach (MarkupElement child in resource.Elements())
            {
                string? rect = ParseBrushRect(child, "Rect");
                string? color = ParseBrushColor(child.Attribute("Color"));
                if (rect is null || color is null || child.Name.LocalName is not ("FillRectangle" or "FillEllipse"))
                {
                    Report(UnsupportedElement, child, child.Name.LocalName);
                    return null;
                }

                commands.Add("global::Cerneala.Drawing.DrawCommand." + child.Name.LocalName + "(" + rect + ", " + color + ")");
            }

            if (commands.Count == 0)
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "DrawingBrush requires at least one drawing command.");
                return null;
            }

            string? tileArguments = ParseTileArguments(resource);
            return tileArguments is null
                ? null
                : "new global::Cerneala.UI.Media.DrawingBrush(new global::Cerneala.Drawing.DrawCommand[] { " +
                    string.Join(", ", commands) + " }, " + bounds + ", " + tileArguments + ")";
        }

        private string? ParseGradientStops(MarkupElement resource)
        {
            List<string> stops = [];
            foreach (MarkupElement stop in resource.Elements().Where(element => element.Name.LocalName == "GradientStop"))
            {
                string? offset = ParseBrushFloat(stop, "Offset", float.NaN, value => value >= 0 && value <= 1);
                string? color = ParseBrushColor(stop.Attribute("Color"));
                if (offset is null || color is null)
                {
                    Report(InvalidPropertyValue, stop, resource.Name.LocalName, "GradientStop", stop.ToString(MarkupSaveOptions.DisableFormatting));
                    return null;
                }

                stops.Add("new global::Cerneala.UI.Media.GradientStop(" + offset + ", " + color + ")");
            }

            if (stops.Count == 0)
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), resource.Name.LocalName + " requires at least one GradientStop child.");
                return null;
            }

            return "new global::Cerneala.UI.Media.GradientStop[] { " + string.Join(", ", stops) + " }";
        }

        private string? ParseTileArguments(MarkupElement resource)
        {
            string stretch = resource.Attribute("Stretch")?.Value.Trim() ?? "Fill";
            string alignmentX = resource.Attribute("AlignmentX")?.Value.Trim() ?? "Center";
            string alignmentY = resource.Attribute("AlignmentY")?.Value.Trim() ?? "Center";
            string tileMode = resource.Attribute("TileMode")?.Value.Trim() ?? "None";
            string? viewport = resource.Attribute("Viewport") is null ? "null" : ParseBrushRect(resource, "Viewport");
            string? viewbox = resource.Attribute("Viewbox") is null ? "null" : ParseBrushRect(resource, "Viewbox");
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            if (!new[] { "None", "Fill", "Uniform", "UniformToFill" }.Contains(stretch) ||
                !new[] { "Left", "Center", "Right" }.Contains(alignmentX) ||
                !new[] { "Top", "Center", "Bottom" }.Contains(alignmentY) ||
                !new[] { "None", "Tile", "FlipX", "FlipY", "FlipXY" }.Contains(tileMode) ||
                viewport is null || viewbox is null || opacity is null)
            {
                Report(InvalidPropertyValue, resource, resource.Name.LocalName, "Tile", resource.ToString(MarkupSaveOptions.DisableFormatting));
                return null;
            }

            return "global::Cerneala.Drawing.DrawBrushStretch." + stretch + ", global::Cerneala.Drawing.DrawBrushAlignmentX." + alignmentX +
                ", global::Cerneala.Drawing.DrawBrushAlignmentY." + alignmentY + ", " + viewport + ", " + viewbox +
                ", global::Cerneala.Drawing.DrawTileMode." + tileMode + ", " + opacity;
        }

        private string? ParseBrushPoint(MarkupElement element, string attributeName)
        {
            string[] parts = (element.Attribute(attributeName)?.Value ?? string.Empty).Split(',').Select(value => value.Trim()).ToArray();
            if (parts.Length != 2 || !TryParseFiniteFloat(parts[0], out string? x) || !TryParseFiniteFloat(parts[1], out string? y))
            {
                Report(InvalidPropertyValue, (object?)element.Attribute(attributeName) ?? element, element.Name.LocalName, attributeName, element.Attribute(attributeName)?.Value ?? string.Empty);
                return null;
            }

            return "new global::Cerneala.Drawing.DrawPoint(" + x + ", " + y + ")";
        }

        private string? ParseBrushRect(MarkupElement element, string attributeName)
        {
            string[] parts = (element.Attribute(attributeName)?.Value ?? string.Empty).Split(',').Select(value => value.Trim()).ToArray();
            if (parts.Length != 4 || parts.Any(part => !float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value)))
            {
                Report(InvalidPropertyValue, (object?)element.Attribute(attributeName) ?? element, element.Name.LocalName, attributeName, element.Attribute(attributeName)?.Value ?? string.Empty);
                return null;
            }

            return "new global::Cerneala.Drawing.DrawRect(" + string.Join(", ", parts.Select(part => float.Parse(part, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f")) + ")";
        }

        private string? ParseBrushFloat(MarkupElement element, string attributeName, float defaultValue, Func<float, bool> validate)
        {
            MarkupAttribute? attribute = element.Attribute(attributeName);
            if (attribute is null && !float.IsNaN(defaultValue) && !float.IsInfinity(defaultValue))
            {
                return defaultValue.ToString("R", CultureInfo.InvariantCulture) + "f";
            }

            if (attribute is null || !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value) || !validate(value))
            {
                Report(InvalidPropertyValue, (object?)attribute ?? element, element.Name.LocalName, attributeName, attribute?.Value ?? string.Empty);
                return null;
            }

            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static string? ParseBrushColor(MarkupAttribute? attribute)
        {
            if (attribute is null)
            {
                return null;
            }

            string value = attribute.Value.Trim();
            if (NamedColorNames.TryGetValue(value, out string? named))
            {
                return "global::Cerneala.Drawing.Color." + named;
            }

            return ParseHexColor(value) is ColorLiteral color ? color.ToExpression() : null;
        }
    }
}
