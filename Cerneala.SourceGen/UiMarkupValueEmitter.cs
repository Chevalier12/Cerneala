using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private sealed class GeneratedExpression
        {
            public GeneratedExpression(string code, MarkupValueKind kind, string? applicationResourceName = null)
            {
                Code = code;
                Kind = kind;
                ApplicationResourceName = applicationResourceName;
            }

            public string Code { get; }

            public MarkupValueKind Kind { get; }

            public string? ApplicationResourceName { get; }
        }

        private readonly struct ColorLiteral
        {
            public ColorLiteral(byte r, byte g, byte b, byte a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public byte R { get; }

            public byte G { get; }

            public byte B { get; }

            public byte A { get; }

            public string ToExpression()
            {
                return A == 255
                    ? "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ")"
                    : "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ", " + A + ")";
            }
        }

        private static bool TryParseFiniteFloat(string value, out string? expression)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && !float.IsNaN(parsed) && !float.IsInfinity(parsed))
            {
                expression = parsed.ToString("R", CultureInfo.InvariantCulture) + "f";
                return true;
            }

            expression = null;
            return false;
        }

        private static ColorLiteral? ParseHexColor(string value)
        {
            if (value.Length != 7 && value.Length != 9)
            {
                return null;
            }

            if (value[0] != '#')
            {
                return null;
            }

            static bool TryByte(string text, out byte parsed)
            {
                return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
            }

            if (value.Length == 7 &&
                TryByte(value.Substring(1, 2), out byte r) &&
                TryByte(value.Substring(3, 2), out byte g) &&
                TryByte(value.Substring(5, 2), out byte b))
            {
                return new ColorLiteral(r, g, b, 255);
            }

            if (value.Length == 9 &&
                TryByte(value.Substring(1, 2), out byte a) &&
                TryByte(value.Substring(3, 2), out byte rr) &&
                TryByte(value.Substring(5, 2), out byte gg) &&
                TryByte(value.Substring(7, 2), out byte bb))
            {
                return new ColorLiteral(rr, gg, bb, a);
            }

            return null;
        }

        private GeneratedExpression? ParseLiteralValue(string elementName, string propertyName, MarkupAttribute attribute, string value, PropertySpec spec)
        {
            MarkupValueKind kind = spec.ValueKind;
            string? code = kind switch
            {
                MarkupValueKind.String when !string.IsNullOrWhiteSpace(value) => Literal(value),
                MarkupValueKind.Bool => Bool(elementName, propertyName, attribute),
                MarkupValueKind.Float => Float(elementName, propertyName, attribute),
                MarkupValueKind.Integer => Integer(elementName, propertyName, attribute, spec.LiteralType.SpecialType),
                MarkupValueKind.Double => Double(elementName, propertyName, attribute),
                MarkupValueKind.Decimal => Decimal(elementName, propertyName, attribute),
                MarkupValueKind.NonNegativeFloat => NonNegativeFloat(elementName, propertyName, attribute),
                MarkupValueKind.PositiveFloat => PositiveFloat(elementName, propertyName, attribute),
                MarkupValueKind.Thickness => Thickness(elementName, propertyName, attribute),
                MarkupValueKind.NonNegativeThickness => NonNegativeThickness(elementName, propertyName, attribute),
                MarkupValueKind.LayoutPoint => LayoutPoint(elementName, propertyName, attribute),
                MarkupValueKind.DrawPoint => DrawPoint(elementName, propertyName, attribute),
                MarkupValueKind.DrawPointList => DrawPointList(elementName, propertyName, attribute),
                MarkupValueKind.PathGeometry => "global::Cerneala.UI.Media.PathGeometry.Parse(" + Literal(value) + ")",
                MarkupValueKind.Color => Color(elementName, propertyName, attribute),
                MarkupValueKind.Brush => Brush(elementName, propertyName, attribute),
                MarkupValueKind.Enum => EnumValue(elementName, propertyName, attribute, spec.LiteralType),
                _ => null
            };

            if (code is null)
            {
                if (kind is MarkupValueKind.String or MarkupValueKind.Unsupported)
                {
                    Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
                }

                return null;
            }

            return new GeneratedExpression(code, kind);
        }

        private string? NonNegativeFloat(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string? code = Float(elementName, propertyName, attribute);
            if (code is null || !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < 0)
            {
                return code is null ? null : Invalid(attribute, elementName, propertyName, attribute.Value);
            }

            return code;
        }

        private string? EnumValue(
            string elementName,
            string propertyName,
            MarkupAttribute attribute,
            ITypeSymbol enumType)
        {
            string value = attribute.Value.Trim();
            IFieldSymbol? member = enumType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(candidate => candidate.HasConstantValue &&
                    string.Equals(candidate.Name, value, StringComparison.OrdinalIgnoreCase));
            return member is null
                ? Invalid(attribute, elementName, propertyName, attribute.Value)
                : enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + member.Name;
        }

        private static string Literal(string value)
        {
            StringBuilder builder = new();
            builder.Append('"');
            foreach (char character in value)
            {
                builder.Append(character switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\0' => "\\0",
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\v' => "\\v",
                    _ when char.IsControl(character) => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                    _ => character.ToString()
                });
            }

            builder.Append('"');
            return builder.ToString();
        }

        private string? Bool(string elementName, string propertyName, MarkupAttribute attribute)
        {
            return bool.TryParse(attribute.Value, out bool parsed) ? (parsed ? "true" : "false") : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Float(string elementName, string propertyName, MarkupAttribute attribute)
        {
            return FloatPart(elementName, propertyName, attribute, attribute.Value);
        }

        private string? Integer(string elementName, string propertyName, MarkupAttribute attribute, SpecialType type)
        {
            string value = attribute.Value.Trim();
            bool valid = type switch
            {
                SpecialType.System_Byte => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_SByte => sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int16 => short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt16 => ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int32 => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt32 => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int64 => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt64 => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                _ => false
            };
            return valid ? value : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Double(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                !double.IsNaN(parsed) && !double.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "d"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Decimal(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed.ToString(CultureInfo.InvariantCulture) + "m"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? PositiveFloat(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed > 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, value);
        }

        private string? Thickness(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length == 1 && FloatPart(elementName, propertyName, attribute, parts[0]) is string uniform)
            {
                return "new global::Cerneala.UI.Layout.Thickness(" + uniform + ")";
            }

            if (parts.Length == 1)
            {
                return null;
            }

            if (parts.Length == 4)
            {
                string? left = FloatPart(elementName, propertyName, attribute, parts[0]);
                string? top = FloatPart(elementName, propertyName, attribute, parts[1]);
                string? right = FloatPart(elementName, propertyName, attribute, parts[2]);
                string? bottom = FloatPart(elementName, propertyName, attribute, parts[3]);
                if (left is not null && top is not null && right is not null && bottom is not null)
                {
                    return "new global::Cerneala.UI.Layout.Thickness(" + left + ", " + top + ", " + right + ", " + bottom + ")";
                }

                return null;
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? NonNegativeThickness(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length == 1 && NonNegativeFloatPart(elementName, propertyName, attribute, parts[0]) is string uniform)
            {
                return "new global::Cerneala.UI.Layout.Thickness(" + uniform + ")";
            }

            if (parts.Length == 1)
            {
                return null;
            }

            if (parts.Length == 4)
            {
                string? left = NonNegativeFloatPart(elementName, propertyName, attribute, parts[0]);
                string? top = NonNegativeFloatPart(elementName, propertyName, attribute, parts[1]);
                string? right = NonNegativeFloatPart(elementName, propertyName, attribute, parts[2]);
                string? bottom = NonNegativeFloatPart(elementName, propertyName, attribute, parts[3]);
                if (left is not null && top is not null && right is not null && bottom is not null)
                {
                    return "new global::Cerneala.UI.Layout.Thickness(" + left + ", " + top + ", " + right + ", " + bottom + ")";
                }

                return null;
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? LayoutPoint(string elementName, string propertyName, MarkupAttribute attribute)
        {
            return Point(
                elementName,
                propertyName,
                attribute,
                "global::Cerneala.UI.Layout.LayoutPoint");
        }

        private string? DrawPoint(string elementName, string propertyName, MarkupAttribute attribute)
        {
            return Point(
                elementName,
                propertyName,
                attribute,
                "global::Cerneala.Drawing.DrawPoint");
        }

        private string? DrawPointList(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string[] parts = attribute.Value.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length % 2 != 0)
            {
                return Invalid(attribute, elementName, propertyName, attribute.Value);
            }

            List<string> points = new();
            for (int index = 0; index < parts.Length; index += 2)
            {
                string? x = FloatPart(elementName, propertyName, attribute, parts[index]);
                string? y = FloatPart(elementName, propertyName, attribute, parts[index + 1]);
                if (x is null || y is null)
                {
                    return null;
                }
                points.Add("new global::Cerneala.Drawing.DrawPoint(" + x + ", " + y + ")");
            }
            return "new global::Cerneala.Drawing.DrawPoint[] { " + string.Join(", ", points) + " }";
        }

        private string? Point(
            string elementName,
            string propertyName,
            MarkupAttribute attribute,
            string typeName)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length != 2)
            {
                return Invalid(attribute, elementName, propertyName, value);
            }

            string? x = FloatPart(elementName, propertyName, attribute, parts[0]);
            string? y = FloatPart(elementName, propertyName, attribute, parts[1]);
            return x is not null && y is not null
                ? "new " + typeName + "(" + x + ", " + y + ")"
                : null;
        }

        private string? FloatPart(string elementName, string propertyName, MarkupAttribute attribute, string value)
        {
            return TryParseFiniteFloat(value, out string? expression)
                ? expression
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? NonNegativeFloatPart(string elementName, string propertyName, MarkupAttribute attribute, string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed >= 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Color(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            if (NamedColorNames.TryGetValue(value, out string? namedColor))
            {
                return "global::Cerneala.Drawing.Color." + namedColor;
            }

            if (ParseHexColor(value) is ColorLiteral hexColor)
            {
                return hexColor.ToExpression();
            }

            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length is 3 or 4 &&
                byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r) &&
                byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g) &&
                byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b) &&
                (parts.Length == 3 || byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                string alpha = parts.Length == 4 ? ", " + parts[3] : string.Empty;
                return "new global::Cerneala.Drawing.Color(" + r + ", " + g + ", " + b + alpha + ")";
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? Brush(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string? color = Color(elementName, propertyName, attribute);
            return color is null ? null : "new global::Cerneala.UI.Media.SolidColorBrush(" + color + ")";
        }

        private string? Invalid(MarkupAttribute attribute, string elementName, string propertyName, string value)
        {
            Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
            return null;
        }
    }
}
