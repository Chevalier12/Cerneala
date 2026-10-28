using System.Globalization;
using Cerneala.Language.Semantics.Symbols;

namespace Cerneala.Language.Semantics;

// The literal rules of the source generator's ParseLiteralValue (Cerneala.SourceGen UiMarkupValueEmitter) for the value
// types it parses itself, so that the editor rejects exactly the attribute, attached-property and Aspect literals the
// build rejects with CERNEALAUI004. Examples: Margin="1,2" (Thickness takes 1 or 4 values), Width="-5" (Width is
// [MarkupValueConstraint(NonNegative)]) and Background="notacolor" are invalid; Margin="-4", Width="0" and
// Background="#80FF0000" are valid.
internal static class MarkupLiteralRules
{
    // Markup color names (case-insensitive) and the Cerneala.Drawing.Color member each one emits.
    internal static readonly IReadOnlyDictionary<string, string> NamedColorNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Transparent"] = "Transparent",
        ["AliceBlue"] = "AliceBlue",
        ["AntiqueWhite"] = "AntiqueWhite",
        ["Aqua"] = "Aqua",
        ["Aquamarine"] = "Aquamarine",
        ["Azure"] = "Azure",
        ["Beige"] = "Beige",
        ["Bisque"] = "Bisque",
        ["Black"] = "Black",
        ["BlanchedAlmond"] = "BlanchedAlmond",
        ["Blue"] = "Blue",
        ["BlueViolet"] = "BlueViolet",
        ["Brown"] = "Brown",
        ["BurlyWood"] = "BurlyWood",
        ["CadetBlue"] = "CadetBlue",
        ["Chartreuse"] = "Chartreuse",
        ["Chocolate"] = "Chocolate",
        ["Coral"] = "Coral",
        ["CornflowerBlue"] = "CornflowerBlue",
        ["Cornsilk"] = "Cornsilk",
        ["Crimson"] = "Crimson",
        ["Cyan"] = "Cyan",
        ["DarkBlue"] = "DarkBlue",
        ["DarkCyan"] = "DarkCyan",
        ["DarkGoldenrod"] = "DarkGoldenrod",
        ["DarkGray"] = "DarkGray",
        ["DarkGreen"] = "DarkGreen",
        ["DarkKhaki"] = "DarkKhaki",
        ["DarkMagenta"] = "DarkMagenta",
        ["DarkOliveGreen"] = "DarkOliveGreen",
        ["DarkOrange"] = "DarkOrange",
        ["DarkOrchid"] = "DarkOrchid",
        ["DarkRed"] = "DarkRed",
        ["DarkSalmon"] = "DarkSalmon",
        ["DarkSeaGreen"] = "DarkSeaGreen",
        ["DarkSlateBlue"] = "DarkSlateBlue",
        ["DarkSlateGray"] = "DarkSlateGray",
        ["DarkTurquoise"] = "DarkTurquoise",
        ["DarkViolet"] = "DarkViolet",
        ["DeepPink"] = "DeepPink",
        ["DeepSkyBlue"] = "DeepSkyBlue",
        ["DimGray"] = "DimGray",
        ["DodgerBlue"] = "DodgerBlue",
        ["Firebrick"] = "Firebrick",
        ["FloralWhite"] = "FloralWhite",
        ["ForestGreen"] = "ForestGreen",
        ["Fuchsia"] = "Fuchsia",
        ["Gainsboro"] = "Gainsboro",
        ["GhostWhite"] = "GhostWhite",
        ["Gold"] = "Gold",
        ["Goldenrod"] = "Goldenrod",
        ["Gray"] = "Gray",
        ["Green"] = "Green",
        ["GreenYellow"] = "GreenYellow",
        ["Honeydew"] = "Honeydew",
        ["HotPink"] = "HotPink",
        ["IndianRed"] = "IndianRed",
        ["Indigo"] = "Indigo",
        ["Ivory"] = "Ivory",
        ["Khaki"] = "Khaki",
        ["Lavender"] = "Lavender",
        ["LavenderBlush"] = "LavenderBlush",
        ["LawnGreen"] = "LawnGreen",
        ["LemonChiffon"] = "LemonChiffon",
        ["LightBlue"] = "LightBlue",
        ["LightCoral"] = "LightCoral",
        ["LightCyan"] = "LightCyan",
        ["LightGoldenrodYellow"] = "LightGoldenrodYellow",
        ["LightGray"] = "LightGray",
        ["LightGreen"] = "LightGreen",
        ["LightPink"] = "LightPink",
        ["LightSalmon"] = "LightSalmon",
        ["LightSeaGreen"] = "LightSeaGreen",
        ["LightSkyBlue"] = "LightSkyBlue",
        ["LightSlateGray"] = "LightSlateGray",
        ["LightSteelBlue"] = "LightSteelBlue",
        ["LightYellow"] = "LightYellow",
        ["Lime"] = "Lime",
        ["LimeGreen"] = "LimeGreen",
        ["Linen"] = "Linen",
        ["Magenta"] = "Magenta",
        ["Maroon"] = "Maroon",
        ["MediumAquamarine"] = "MediumAquamarine",
        ["MediumBlue"] = "MediumBlue",
        ["MediumOrchid"] = "MediumOrchid",
        ["MediumPurple"] = "MediumPurple",
        ["MediumSeaGreen"] = "MediumSeaGreen",
        ["MediumSlateBlue"] = "MediumSlateBlue",
        ["MediumSpringGreen"] = "MediumSpringGreen",
        ["MediumTurquoise"] = "MediumTurquoise",
        ["MediumVioletRed"] = "MediumVioletRed",
        ["MidnightBlue"] = "MidnightBlue",
        ["MintCream"] = "MintCream",
        ["MistyRose"] = "MistyRose",
        ["Moccasin"] = "Moccasin",
        ["NavajoWhite"] = "NavajoWhite",
        ["Navy"] = "Navy",
        ["OldLace"] = "OldLace",
        ["Olive"] = "Olive",
        ["OliveDrab"] = "OliveDrab",
        ["Orange"] = "Orange",
        ["OrangeRed"] = "OrangeRed",
        ["Orchid"] = "Orchid",
        ["PaleGoldenrod"] = "PaleGoldenrod",
        ["PaleGreen"] = "PaleGreen",
        ["PaleTurquoise"] = "PaleTurquoise",
        ["PaleVioletRed"] = "PaleVioletRed",
        ["PapayaWhip"] = "PapayaWhip",
        ["PeachPuff"] = "PeachPuff",
        ["Peru"] = "Peru",
        ["Pink"] = "Pink",
        ["Plum"] = "Plum",
        ["PowderBlue"] = "PowderBlue",
        ["Purple"] = "Purple",
        ["Red"] = "Red",
        ["RosyBrown"] = "RosyBrown",
        ["RoyalBlue"] = "RoyalBlue",
        ["SaddleBrown"] = "SaddleBrown",
        ["Salmon"] = "Salmon",
        ["SandyBrown"] = "SandyBrown",
        ["SeaGreen"] = "SeaGreen",
        ["SeaShell"] = "SeaShell",
        ["Sienna"] = "Sienna",
        ["Silver"] = "Silver",
        ["SkyBlue"] = "SkyBlue",
        ["SlateBlue"] = "SlateBlue",
        ["SlateGray"] = "SlateGray",
        ["Snow"] = "Snow",
        ["SpringGreen"] = "SpringGreen",
        ["SteelBlue"] = "SteelBlue",
        ["Tan"] = "Tan",
        ["Teal"] = "Teal",
        ["Thistle"] = "Thistle",
        ["Tomato"] = "Tomato",
        ["Turquoise"] = "Turquoise",
        ["Violet"] = "Violet",
        ["Wheat"] = "Wheat",
        ["White"] = "White",
        ["WhiteSmoke"] = "WhiteSmoke",
        ["Yellow"] = "Yellow",
        ["YellowGreen"] = "YellowGreen",
    };

    private static readonly char[] PointListSeparators = [',', ' ', '\t', '\r', '\n'];

    // True when the build accepts the literal for a value of this type. Types the generator does not parse itself
    // (enums, bool, int, string, PathGeometry, ...) are left to the caller and return true here.
    internal static bool IsValid(string value, string valueTypeMetadataName, LanguageValueConstraint constraint)
    {
        bool nonNegative = constraint == LanguageValueConstraint.NonNegative;
        switch (valueTypeMetadataName)
        {
            case "float" or "System.Single":
                return TryParseFiniteFloat(value, out float number) && constraint switch
                {
                    LanguageValueConstraint.NonNegative => number >= 0,
                    LanguageValueConstraint.Positive => number > 0,
                    _ => true
                };
            case "decimal" or "System.Decimal":
                return decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            case "byte" or "System.Byte":
                return byte.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "sbyte" or "System.SByte":
                return sbyte.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "short" or "System.Int16":
                return short.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "ushort" or "System.UInt16":
                return ushort.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "uint" or "System.UInt32":
                return uint.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "long" or "System.Int64":
                return long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "ulong" or "System.UInt64":
                return ulong.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case "Cerneala.UI.Layout.Thickness" or "Cerneala.UI.Layout.CornerRadius":
                // One value for all four sides (corners), or four values.
                string[] sides = SplitTrimmed(value);
                return sides.Length is 1 or 4 && sides.All(side =>
                    TryParseFiniteFloat(side, out float number) && (!nonNegative || number >= 0));
            case "Cerneala.UI.Layout.LayoutPoint" or "Cerneala.Drawing.DrawPoint":
                string[] coordinates = SplitTrimmed(value);
                return coordinates.Length == 2 && coordinates.All(coordinate => TryParseFiniteFloat(coordinate, out _));
            case "System.Collections.Generic.IReadOnlyList<Cerneala.Drawing.DrawPoint>":
                string[] points = value.Split(PointListSeparators, StringSplitOptions.RemoveEmptyEntries);
                return points.Length % 2 == 0 && points.All(coordinate => TryParseFiniteFloat(coordinate, out _));
            case "Cerneala.Drawing.Color" or "Cerneala.UI.Media.Brush":
                return IsColor(value);
            default:
                return true;
        }
    }

    // A color name, #RRGGBB, #AARRGGBB, or r,g,b / r,g,b,a with byte components.
    private static bool IsColor(string value)
    {
        if (NamedColorNames.ContainsKey(value))
        {
            return true;
        }

        if (value.Length is 7 or 9 && value[0] == '#')
        {
            for (int index = 1; index < value.Length; index += 2)
            {
                if (!byte.TryParse(value.Substring(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    return false;
                }
            }

            return true;
        }

        string[] components = SplitTrimmed(value);
        return components.Length is 3 or 4 && components.All(component =>
            byte.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
    }

    private static bool TryParseFiniteFloat(string value, out float parsed) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) &&
        !float.IsNaN(parsed) && !float.IsInfinity(parsed);

    private static string[] SplitTrimmed(string value) => value.Split(',').Select(part => part.Trim()).ToArray();
}