using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismCatalogFilterPlanner
{
    private static void ApplyFilterOptions(
        PrismFilterId filter,
        PrismCatalogEntryDescriptor entry,
        PrismFilterParameterReader reader,
        float deviceScale,
        Vector4[] options)
    {
        if (filter == PrismFilterId.Charcoal)
        {
            options[5] = CharcoalFdogSettings(reader, deviceScale);
            options[6] = CharcoalEtfSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.Extrude)
        {
            SetOption(
                options,
                entry,
                "Type",
                new Vector4(
                reader.SymbolCode(
                    "Type",
                    ("Blocks", 0),
                    ("Pyramids", 1)),
                    0,
                    0,
                    0));
            SetOption(
                options,
                entry,
                "DepthMode",
                new Vector4(
                reader.SymbolCode(
                    "DepthMode",
                    ("Random", 0),
                    ("Level", 1)),
                    0,
                    0,
                    0));
        }
        else if (filter == PrismFilterId.ColorMatrix)
        {
            PrismColorMatrixFilter.Pack(
                resource: null,
                out options[2],
                out options[3],
                out options[4],
                out options[5],
                out options[6]);
        }
        else if (filter == PrismFilterId.CustomConvolution)
        {
            SetOption(
                options,
                entry,
                "EdgeMode",
                new Vector4(
                    reader.SymbolCode(
                        "EdgeMode",
                        ("Clamp", 0),
                        ("Transparent", 1),
                        ("Wrap", 2),
                        ("Mirror", 3),
                        ("Reflect", 3)),
                    0,
                    0,
                    0));
        }
        else if (filter == PrismFilterId.ChalkCharcoal)
        {
            options[5] = ChalkCharcoalGaussianSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.ConteCrayon)
        {
            options[4] = PackLightDirection(reader);
            options[6].X = Math.Clamp(
                MathF.Abs(options[6].X) * deviceScale,
                0.125f,
                16);
            options[7] = PackSurfaceTexture(reader);
            options[8] = ConteCrayonXDogSettings(deviceScale);
        }
        else if (filter == PrismFilterId.Texturizer)
        {
            options[4] = PackSurfaceTexture(reader);
            options[3].X = Math.Clamp(
                MathF.Abs(reader.Number("Scaling")),
                0.125f,
                16);
            options[2].X = Math.Clamp(
                MathF.Abs(reader.Number("Relief")),
                0,
                1);
            options[1] = PackLightDirection(reader);
            options[6].X = deviceScale;
        }
        else if (filter == PrismFilterId.GraphicPen)
        {
            options[4].X = Math.Clamp(
                MathF.Abs(options[4].X) * deviceScale,
                1,
                96);
            options[3] = new Vector4(
                reader.SymbolCode(
                    "StrokeDirection",
                    ("RightDiagonal", 0),
                    ("Horizontal", 1),
                    ("LeftDiagonal", 2),
                    ("Vertical", 3)),
                0,
                0,
                0);
            options[5] = GraphicPenXDogSettings(reader, deviceScale);
            options[6] = GraphicPenEtfSettings();
        }
        else if (filter == PrismFilterId.AccentedEdges)
        {
            options[3] = AccentedEdgesGaussianSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.GlowingEdges)
        {
            options[3] = GlowingEdgesSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.DarkStrokes)
        {
            options[3] = XDogGaussianSettings(deviceScale);
        }
        else if (filter == PrismFilterId.InkOutlines)
        {
            options[3] = InkOutlinesGaussianSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.SumiE)
        {
            options[3] = SumiEGaussianSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.Chrome)
        {
            options[2] = ChromeSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.NotePaper)
        {
            options[5] = NotePaperSettings(reader);
        }
        else if (filter == PrismFilterId.Plaster)
        {
            options[6] = PackLightDirection(reader);
            options[5] = PlasterSettings(reader, deviceScale);
        }
        else if (filter is PrismFilterId.Photocopy or PrismFilterId.Stamp)
        {
            options[4] = filter == PrismFilterId.Photocopy
                ? PhotocopyXDogSettings(reader, deviceScale)
                : StampXDogSettings(reader, deviceScale);
            options[5] = reader.Color("Foreground");
            options[6] = reader.Color("Background");
        }
        else if (filter == PrismFilterId.TornEdges)
        {
            options[5] = TornEdgesXDogSettings(reader, deviceScale);
            options[6] = TornEdgesNoiseSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.Craquelure)
        {
            options[4] = CraquelureSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.Grain)
        {
            GrainSettings(
                reader,
                deviceScale,
                out options[4],
                out options[5]);
        }
        else if (filter == PrismFilterId.MosaicTiles)
        {
            Vector3 settings = MosaicTilesSettings(
                reader,
                deviceScale);
            SetOption(
                options,
                entry,
                "TileSize",
                new Vector4(settings.X, 0, 0, 0));
            SetOption(
                options,
                entry,
                "GroutWidth",
                new Vector4(settings.Y, 0, 0, 0));
            SetOption(
                options,
                entry,
                "LightenGrout",
                new Vector4(settings.Z, 0, 0, 0));
        }
        else if (filter == PrismFilterId.Patchwork)
        {
            Vector2 settings = PatchworkSettings(
                reader,
                deviceScale);
            SetOption(
                options,
                entry,
                "SquareSize",
                new Vector4(settings.X, 0, 0, 0));
            SetOption(
                options,
                entry,
                "Relief",
                new Vector4(settings.Y, 0, 0, 0));
        }
        else if (filter == PrismFilterId.StainedGlass)
        {
            Vector3 settings = StainedGlassSettings(
                reader,
                deviceScale);
            SetOption(
                options,
                entry,
                "CellSize",
                new Vector4(settings.X, 0, 0, 0));
            SetOption(
                options,
                entry,
                "BorderThickness",
                new Vector4(settings.Y, 0, 0, 0));
            SetOption(
                options,
                entry,
                "LightIntensity",
                new Vector4(settings.Z, 0, 0, 0));
        }
        else if (filter == PrismFilterId.Reticulation)
        {
            options[4] = ReticulationSettings(reader, deviceScale);
        }
        else if (filter == PrismFilterId.WaterPaper)
        {
            options[2].X = Math.Clamp(
                MathF.Abs(options[2].X) * deviceScale,
                1,
                96);
        }
        else if (filter == PrismFilterId.Wind)
        {
            options[0] = new Vector4(
                reader.SymbolCode(
                    "Direction",
                    ("FromRight", 0),
                    ("FromLeft", 1)),
                0,
                0,
                0);
            options[1] = new Vector4(
                reader.SymbolCode(
                    "Method",
                    ("Wind", 0),
                    ("Blast", 1),
                    ("Stagger", 2)),
                0,
                0,
                0);
            options[3].X = Math.Clamp(
                MathF.Abs(options[3].X) * deviceScale,
                0,
                16);
        }
        else if (filter == PrismFilterId.ColorHalftone)
        {
            Vector4 radians =
                reader.Vector("Angles") * (MathF.PI / 180);
            options[2] = new Vector4(
                MathF.Cos(radians.X),
                MathF.Cos(radians.Y),
                MathF.Cos(radians.Z),
                MathF.Cos(radians.W));
            options[3] = new Vector4(
                MathF.Sin(radians.X),
                MathF.Sin(radians.Y),
                MathF.Sin(radians.Z),
                MathF.Sin(radians.W));
        }
        else if (filter == PrismFilterId.HalftonePattern)
        {
            float cellSize =
                MathF.Max(0, reader.Number("Size")) *
                deviceScale *
                2;
            options[4].X = Math.Clamp(cellSize, 2, 16384);
            options[3] = new Vector4(
                reader.SymbolCode(
                    "PatternType",
                    ("Dot", 0),
                    ("Line", 1),
                    ("Circle", 2)),
                0,
                0,
                0);
        }
        else if (filter == PrismFilterId.Mezzotint)
        {
            options[2] = MezzotintPattern(reader);
        }
        else if (filter == PrismFilterId.PaintDaubs)
        {
            options[1] = new Vector4(
                reader.SymbolCode(
                    "BrushType",
                    ("Simple", 0),
                    ("LightRough", 1),
                    ("DarkRough", 2),
                    ("WideSharp", 3),
                    ("WideBlurry", 4),
                    ("Sparkle", 5)),
                0,
                0,
                0);
        }
        else if (filter == PrismFilterId.BasRelief)
        {
            options[3] = PackLightDirection(reader);
        }
        else if (filter == PrismFilterId.RoughPastels)
        {
            options[6] = PackSurfaceTexture(reader);
            options[1] = PackLightDirection(reader);
        }
        else if (filter == PrismFilterId.Underpainting)
        {
            options[5] = PackSurfaceTexture(reader);
            options[2] = PackLightDirection(reader);
        }
        else if (filter == PrismFilterId.Crosshatch)
        {
            options[0].X *= deviceScale;
        }
        else if (filter == PrismFilterId.Spatter)
        {
            options[2].X *= deviceScale;
        }
        else if (filter == PrismFilterId.SprayedStrokes)
        {
            options[3].X = MathF.Max(options[3].X, 0) * deviceScale;
            options[2].X = MathF.Max(options[2].X, 0) * deviceScale;
            options[0] = new Vector4(
                reader.SymbolCode(
                    "Direction",
                    ("RightDiagonal", 0),
                    ("Horizontal", 1),
                    ("LeftDiagonal", 2),
                    ("Vertical", 3)),
                0,
                0,
                0);
        }
        else if (filter == PrismFilterId.LightingEffects)
        {
            options[6].X *= deviceScale;
        }
        else if (filter == PrismFilterId.Deinterlace)
        {
            options[0] = new Vector4(
                reader.SymbolCode(
                    "Field",
                    ("Even", 0),
                    ("Odd", 1)),
                0,
                0,
                0);
            options[1] = new Vector4(
                reader.SymbolCode(
                    "Replacement",
                    ("Interpolation", 0),
                    ("Duplication", 1),
                    ("Duplicate", 1)),
                0,
                0,
                0);
        }
        else if (filter == PrismFilterId.NtscColors)
        {
            options[0] = new Vector4(
                reader.SymbolCode(
                    "Standard",
                    ("NTSC", 0)),
                0,
                0,
                0);
            options[1] = new Vector4(
                reader.SymbolCode(
                    "Method",
                    ("ReduceLuminance", 0)),
                0,
                0,
                0);
        }
        if (filter == PrismFilterId.Tiles)
        {
            SetOption(
                options,
                entry,
                "MaximumOffset",
                new Vector4(
                    Math.Clamp(
                        reader.Number("MaximumOffset"),
                        0,
                        1),
                    0,
                    0,
                    0));
            SetOption(
                options,
                entry,
                "Tiles",
                new Vector4(
                    Math.Clamp(
                        MathF.Round(reader.Number("Tiles")),
                        1,
                        16384),
                    0,
                    0,
                    0));
        }
    }

    private static Vector4 MezzotintPattern(
        PrismFilterParameterReader values)
    {
        int type = values.SymbolCode(
            "Type",
            ("FineDots", 0),
            ("MediumDots", 1),
            ("GrainyDots", 2),
            ("CoarseDots", 3),
            ("ShortLines", 4),
            ("MediumLines", 5),
            ("LongLines", 6),
            ("ShortStrokes", 7),
            ("MediumStrokes", 8),
            ("LongStrokes", 9));
        return type switch
        {
            0 => new Vector4(1, 1, 0, 0),
            1 => new Vector4(2, 2, 0, 0),
            2 => new Vector4(1, 1, 1, 0),
            3 => new Vector4(4, 4, 0, 0),
            4 => new Vector4(3, 1, 2, 0),
            5 => new Vector4(6, 1, 2, 0),
            6 => new Vector4(9, 1, 2, 0),
            7 => new Vector4(3, 2, 3, 0),
            8 => new Vector4(6, 2, 3, 0),
            9 => new Vector4(9, 2, 3, 0),
            _ => throw new InvalidOperationException(
                $"Unsupported Mezzotint type '{type}'.")
        };
    }

    private static Vector4 PackLightDirection(
        PrismFilterParameterReader values) =>
        new(
            values.SymbolCode(
                "LightDirection",
                ("Top", 0),
                ("TopRight", 1),
                ("Right", 2),
                ("BottomRight", 3),
                ("Bottom", 4),
                ("BottomLeft", 5),
                ("Left", 6),
                ("TopLeft", 7)),
            0,
            0,
            0);

    private static Vector4 PackSurfaceTexture(
        PrismFilterParameterReader values) =>
        new(
            values.SymbolCode(
                "Texture",
                ("Canvas", 0),
                ("Brick", 1),
                ("Burlap", 2),
                ("Sandstone", 3)),
            0,
            0,
            0);

    private static Vector4 Pack(
        PrismFilterParameterReader reader,
        PrismCatalogPropertyDescriptor property,
        PrismGraphParameter parameter)
    {
        return property.ValueType switch
        {
            PrismCatalogValueType.Boolean =>
                new Vector4(parameter.BooleanValue ? 1 : 0, 0, 0, 0),
            PrismCatalogValueType.Integer =>
                PackInteger(parameter.IntegerValue),
            PrismCatalogValueType.Number =>
                new Vector4(parameter.NumberValue, 0, 0, 0),
            PrismCatalogValueType.Color =>
                reader.Color(property.Name),
            PrismCatalogValueType.Vector =>
                parameter.VectorValue,
            PrismCatalogValueType.Symbol =>
                PackInteger(parameter.IntegerValue),
            PrismCatalogValueType.Resource =>
                Vector4.Zero,
            _ => throw new InvalidOperationException(
                $"Property '{property.Name}' has an unknown catalog value type.")
        };
    }

    private static void SetOption(
        Vector4[] options,
        PrismCatalogEntryDescriptor entry,
        string propertyName,
        Vector4 value)
    {
        PrismCatalogPropertyDescriptor property =
            entry.Properties.First(candidate =>
                string.Equals(
                    candidate.Name,
                    propertyName,
                    StringComparison.Ordinal));
        options[property.Slot] = value;
    }

    private static Vector4 PackInteger(int value)
    {
        uint bits = unchecked((uint)value);
        return new Vector4(
            bits & 0xffffu,
            bits >> 16,
            0,
            0);
    }
}
