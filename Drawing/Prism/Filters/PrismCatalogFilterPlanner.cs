using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismCatalogFilterPrimitive
{
    Morphology,
    Quantization,
    Procedural,
    Video,
    Artistic,
    EdgeDetection,
    Tiling,
    Texture,
    Convolution,
    Color,
    Extrude,
    LineIntegralConvolution
}

internal enum PrismCatalogFilterPassKind
{
    Direct,
    Horizontal,
    Vertical,
    Iteration
}

internal readonly record struct PrismCatalogFilterPass(
    PrismCatalogFilterPassKind Kind,
    float RadiusX,
    float RadiusY,
    float BoundsRadiusX,
    float BoundsRadiusY,
    int Iteration,
    bool IsNoOp);

internal readonly record struct PrismCatalogFilterPlan
{
    public PrismCatalogFilterPlan(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismBlendMode blendMode,
        ImmutableArray<PrismCatalogFilterPass> passes)
    {
        this = default;
        Filter = filter;
        Primitive = primitive;
        BlendMode = blendMode;
        Passes = passes;
    }

    public PrismFilterId Filter { get; init; }

    public PrismCatalogFilterPrimitive Primitive { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public ImmutableArray<PrismCatalogFilterPass> Passes { get; init; }

    public Vector4 Options0 { get; init; }

    public Vector4 Options1 { get; init; }

    public Vector4 Options2 { get; init; }

    public Vector4 Options3 { get; init; }

    public Vector4 Options4 { get; init; }

    public Vector4 Options5 { get; init; }

    public Vector4 Options6 { get; init; }

    public Vector4 Options7 { get; init; }

    public Vector4 Options8 { get; init; }

    public PrismResourceId PrimaryResource { get; init; }

    public bool PrimaryResourceRequired { get; init; }

    public PrismResourceId AuxiliaryResource { get; init; }

    public bool AuxiliaryResourceRequired { get; init; }

    public PrismWaveNoiseTable WaveNoiseTable { get; init; }

    public uint WaveNoiseSeed { get; init; }

    public uint SpatterSeed { get; init; }

    public Vector4 GetOption(int slot) =>
        slot switch
        {
            0 => Options0,
            1 => Options1,
            2 => Options2,
            3 => Options3,
            4 => Options4,
            5 => Options5,
            6 => Options6,
            7 => Options7,
            8 => Options8,
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

    public Vector4 GetOption(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return TryGetOption(propertyName, out Vector4 value)
            ? value
            : throw new InvalidOperationException(
                $"Filter '{Filter}' has no generated property '{propertyName}'.");
    }

    public bool TryGetOption(
        string propertyName,
        out Vector4 value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)Filter);
        foreach (PrismCatalogPropertyDescriptor property in entry.Properties)
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.Ordinal))
            {
                value = GetOption(property.Slot);
                return true;
            }
        }

        value = default;
        return false;
    }
}

internal static class PrismCatalogFilterPlanner
{
    private const string KernelOwnerPrefix =
        "PrismKernelRegistry/";
    private const string TestOwnerPrefix =
        "PrismCatalogFilterTests/";

    public static bool IsSupported(PrismFilterId filter)
    {
        if (!TryGetPrimitive(
                filter,
                out PrismCatalogFilterPrimitive _))
        {
            return false;
        }

        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        return entry.Kind == "filter" &&
            entry.Execution is not null &&
            string.Equals(
                entry.Coverage.Kernel,
                KernelOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal) &&
            string.Equals(
                entry.Coverage.Test,
                TestOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal);
    }

    public static bool RequiresOriginalInput(
        PrismFilterId filter,
        PrismCatalogFilterPass pass) =>
        ((filter is
                PrismFilterId.ColoredPencil or
                PrismFilterId.Fresco) &&
            pass.Iteration == 3) ||
        (filter == PrismFilterId.Watercolor &&
            pass.Iteration == 6) ||
        (filter == PrismFilterId.WaterPaper &&
            pass.Iteration == 1) ||
        (filter == PrismFilterId.SumiE &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Charcoal &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.ConteCrayon &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.GraphicPen &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.ChalkCharcoal &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Cutout &&
            pass.Kind == PrismCatalogFilterPassKind.Direct) ||
        (filter is
                PrismFilterId.AccentedEdges or
                PrismFilterId.DarkStrokes or
                PrismFilterId.InkOutlines &&
            pass.Iteration == 2) ||
        (filter is
                PrismFilterId.BasRelief or
                PrismFilterId.PosterEdges &&
            pass.Iteration is 2 or 4 or 5) ||
        (filter == PrismFilterId.GlowingEdges &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.NotePaper &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Plaster &&
            pass.Iteration is 3 or 4) ||
        (filter is
                PrismFilterId.Photocopy or
                PrismFilterId.Stamp or
                PrismFilterId.TornEdges &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Chrome &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.StainedGlass &&
            pass.Kind == PrismCatalogFilterPassKind.Direct &&
            pass.Iteration > 0) ||
        filter == PrismFilterId.Wind;

    public static PrismCatalogFilterPlan Create(
        PrismFilterId filter,
        ImmutableArray<PrismGraphParameter> parameters,
        PrismBlendMode blendMode,
        float pixelScale,
        Matrix3x2 effectiveTransform,
        DrawRect sourceBounds)
    {
        if (!IsSupported(filter) ||
            !TryGetPrimitive(
                filter,
                out PrismCatalogFilterPrimitive primitive))
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has no catalog filter planner.");
        }
        if (!float.IsFinite(pixelScale) || pixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelScale),
                pixelScale,
                "Filter planning requires a finite positive pixel scale.");
        }

        float transformScale = MathF.Max(
            MathF.Sqrt(
                (effectiveTransform.M11 * effectiveTransform.M11) +
                (effectiveTransform.M12 * effectiveTransform.M12)),
            MathF.Sqrt(
                (effectiveTransform.M21 * effectiveTransform.M21) +
                (effectiveTransform.M22 * effectiveTransform.M22)));
        float deviceScale = transformScale * pixelScale;
        if (!float.IsFinite(deviceScale) || deviceScale <= 0)
        {
            throw new InvalidOperationException(
                "The filter transform produced an invalid device scale.");
        }

        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        if (parameters.Length != entry.Properties.Length)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has {parameters.Length} graph values " +
                $"for {entry.Properties.Length} generated properties.");
        }
        if (entry.Properties.Length > 9)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' exceeds the nine generated option slots.");
        }

        PrismFilterParameterReader reader =
            new(filter, parameters);
        Vector4[] options = new Vector4[9];
        PrismResourceId primaryResource = default;
        PrismResourceId auxiliaryResource = default;
        bool primaryRequired = false;
        bool auxiliaryRequired = false;
        int resourceCount = 0;
        for (int index = 0; index < entry.Properties.Length; index++)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties[index];
            PrismGraphParameter parameter = parameters[index];
            ValidateSlot(filter, property, parameter, index);
            if (property.ValueType == PrismCatalogValueType.Resource)
            {
                if (resourceCount == 0)
                {
                    primaryResource = parameter.ResourceValue;
                    primaryRequired = property.Required;
                }
                else if (resourceCount == 1)
                {
                    auxiliaryResource = parameter.ResourceValue;
                    auxiliaryRequired = property.Required;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Filter '{filter}' exceeds two auxiliary resources.");
                }
                resourceCount++;
                continue;
            }

            options[property.Slot] = Pack(
                reader,
                property,
                parameter);
        }

        ImmutableArray<PrismCatalogFilterPass> passes =
            CreatePasses(
                filter,
                primitive,
                reader,
                deviceScale,
                pixelScale,
                sourceBounds);
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
            options[4] = new Vector4(
                reader.SymbolCode(
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
            options[6].X = Math.Clamp(
                MathF.Abs(options[6].X) * deviceScale,
                0.125f,
                16);
            options[7] = new Vector4(
                reader.SymbolCode(
                    "Texture",
                    ("Canvas", 0),
                    ("Brick", 1),
                    ("Burlap", 2),
                    ("Sandstone", 3)),
                0,
                0,
                0);
            options[8] = ConteCrayonXDogSettings(deviceScale);
        }
        else if (filter == PrismFilterId.Texturizer)
        {
            options[4] = new Vector4(
                reader.SymbolCode(
                    "Texture",
                    ("Canvas", 0),
                    ("Brick", 1),
                    ("Burlap", 2),
                    ("Sandstone", 3)),
                0,
                0,
                0);
            options[3].X = Math.Clamp(
                MathF.Abs(reader.Number("Scaling")),
                0.125f,
                16);
            options[2].X = Math.Clamp(
                MathF.Abs(reader.Number("Relief")),
                0,
                1);
            options[1] = new Vector4(
                reader.SymbolCode(
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
            options[6] = new Vector4(
                reader.SymbolCode(
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
            options[3] = new Vector4(
                reader.SymbolCode(
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
        }
        else if (filter == PrismFilterId.RoughPastels)
        {
            options[6] = new Vector4(
                reader.SymbolCode(
                    "Texture",
                    ("Canvas", 0),
                    ("Brick", 1),
                    ("Burlap", 2),
                    ("Sandstone", 3)),
                0,
                0,
                0);
            options[1] = new Vector4(
                reader.SymbolCode(
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
        }
        else if (filter == PrismFilterId.Underpainting)
        {
            options[5] = new Vector4(
                reader.SymbolCode(
                    "Texture",
                    ("Canvas", 0),
                    ("Brick", 1),
                    ("Burlap", 2),
                    ("Sandstone", 3)),
                0,
                0,
                0);
            options[2] = new Vector4(
                reader.SymbolCode(
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
        PrismWaveNoiseTable waveNoiseTable = default;
        uint waveNoiseSeed = 0;
        uint spatterSeed = 0;
        if (filter is
            PrismFilterId.Clouds or
            PrismFilterId.DifferenceClouds)
        {
            waveNoiseSeed = unchecked(
                (uint)reader.Integer("Seed"));
            PrismWaveSpectrum spectrum = (PrismWaveSpectrum)
                reader.SymbolCode(
                    "Spectrum",
                    ("White", (int)PrismWaveSpectrum.White),
                    ("Blue", (int)PrismWaveSpectrum.Blue),
                    ("Pink", (int)PrismWaveSpectrum.Pink),
                    ("Brown", (int)PrismWaveSpectrum.Brown));
            waveNoiseTable = PrismWaveNoise.Precompute(
                unchecked((int)waveNoiseSeed),
                reader.Vector("FrequencyRange"),
                spectrum);
        }
        else if (filter == PrismFilterId.Spatter)
        {
            spatterSeed = unchecked((uint)reader.Integer("Seed"));
        }
        return new PrismCatalogFilterPlan(
            filter,
            primitive,
            blendMode,
            passes)
        {
            Options0 = options[0],
            Options1 = options[1],
            Options2 = options[2],
            Options3 = options[3],
            Options4 = options[4],
            Options5 = options[5],
            Options6 = options[6],
            Options7 = options[7],
            Options8 = options[8],
            PrimaryResource = primaryResource,
            PrimaryResourceRequired = primaryRequired,
            AuxiliaryResource = auxiliaryResource,
            AuxiliaryResourceRequired = auxiliaryRequired,
            WaveNoiseTable = waveNoiseTable,
            WaveNoiseSeed = waveNoiseSeed,
            SpatterSeed = spatterSeed
        };
    }

    private static ImmutableArray<PrismCatalogFilterPass> CreatePasses(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismFilterParameterReader values,
        float deviceScale,
        float pixelScale,
        DrawRect sourceBounds)
    {
        if (filter == PrismFilterId.MosaicTiles)
        {
            float mosaicRadius =
                MosaicTilesSettings(values, deviceScale).X * 0.5f;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    mosaicRadius,
                    mosaicRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Patchwork)
        {
            float patchworkRadius =
                PatchworkSettings(values, deviceScale).X * 0.5f;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    patchworkRadius,
                    patchworkRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.StainedGlass)
        {
            Vector3 settings = StainedGlassSettings(
                values,
                deviceScale);
            double scaledWidth = Math.Max(sourceBounds.Width, 0) *
                deviceScale;
            double scaledHeight = Math.Max(sourceBounds.Height, 0) *
                deviceScale;
            double maximumDimension = Math.Max(
                scaledWidth,
                scaledHeight);
            int floodPassCount = maximumDimension <= 1
                ? 0
                : Math.Clamp(
                    (int)Math.Ceiling(Math.Log2(maximumDimension)),
                    0,
                    30);
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    floodPassCount + 2);
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                0,
                0,
                0,
                0,
                0,
                IsNoOp: false));
            float jump = floodPassCount == 0
                ? 0
                : MathF.Pow(2, floodPassCount - 1);
            for (int index = 0; index < floodPassCount; index++)
            {
                passes.Add(new(
                    PrismCatalogFilterPassKind.Iteration,
                    jump,
                    jump,
                    0,
                    0,
                    index + 1,
                    IsNoOp: false));
                jump *= 0.5f;
            }
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                settings.Y,
                settings.Y,
                0,
                0,
                floodPassCount + 1,
                IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.Plaster)
        {
            float radius = PlasterSettings(values, deviceScale).Y;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    4,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.Photocopy or
            PrismFilterId.Stamp or
            PrismFilterId.TornEdges)
        {
            float radius = filter switch
            {
                PrismFilterId.Photocopy =>
                    PhotocopyXDogSettings(values, deviceScale).Z,
                PrismFilterId.Stamp =>
                    StampXDogSettings(values, deviceScale).Z,
                _ => TornEdgesXDogSettings(values, deviceScale).Z
            };
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.NotePaper)
        {
            float radius = Math.Clamp(
                2 * MathF.Sqrt(deviceScale),
                1,
                4);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Chrome)
        {
            float radius = ChromeSettings(values, deviceScale).Y;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Charcoal)
        {
            Vector4 fdog = CharcoalFdogSettings(values, deviceScale);
            Vector4 etf = CharcoalEtfSettings(values, deviceScale);
            int refinementCount = (int)etf.Y;
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    refinementCount + 4);
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                1,
                1,
                0,
                0,
                0,
                IsNoOp: false));
            for (int iteration = 1; iteration <= refinementCount; iteration++)
            {
                passes.Add(new(
                    PrismCatalogFilterPassKind.Iteration,
                    etf.X,
                    etf.X,
                    0,
                    0,
                    iteration,
                    IsNoOp: false));
            }

            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                fdog.Z,
                fdog.Z,
                0,
                0,
                4,
                IsNoOp: false));
            passes.Add(new(
                PrismCatalogFilterPassKind.Iteration,
                fdog.W,
                fdog.W,
                0,
                0,
                5,
                IsNoOp: false));
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                0,
                0,
                0,
                0,
                6,
                IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.ConteCrayon)
        {
            Vector4 xdog = ConteCrayonXDogSettings(deviceScale);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    3,
                    3,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    xdog.Z,
                    xdog.Z,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    xdog.W,
                    xdog.W,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.GraphicPen)
        {
            Vector4 xdog = GraphicPenXDogSettings(values, deviceScale);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    3,
                    3,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    xdog.Z,
                    xdog.Z,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    xdog.W,
                    xdog.W,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.ChalkCharcoal)
        {
            float radius = ChalkCharcoalGaussianSettings(
                values,
                deviceScale).W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SumiE)
        {
            float strokeWidth = MathF.Max(
                values.Number("StrokeWidth"),
                0);
            float washRadius = Math.Clamp(
                (1 + (strokeWidth * 0.25f)) * deviceScale,
                1,
                6);
            Vector4 gaussian = SumiEGaussianSettings(
                values,
                deviceScale);
            float gaussianRadius = gaussian.W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    washRadius,
                    washRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    gaussianRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    gaussianRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Watercolor)
        {
            float detail = Math.Clamp(
                values.Number("BrushDetail"),
                0,
                16);
            float meanShiftRadius = MathF.Max(
                (3 - (2 * detail / 16)) * deviceScale,
                1);
            float morphologyRadius = MathF.Max(
                (detail < 6 ? 2 : 1) * deviceScale,
                1);
            float edgeRadius = MathF.Max(deviceScale, 1);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    meanShiftRadius,
                    meanShiftRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    meanShiftRadius,
                    meanShiftRadius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    edgeRadius,
                    edgeRadius,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.WaterPaper)
        {
            float fiberLength = Math.Clamp(
                MathF.Abs(values.Number("FiberLength")) * deviceScale,
                1,
                96);
            float pigmentRadius = Math.Clamp(
                MathF.Sqrt(fiberLength) * 0.75f,
                1,
                6);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    pigmentRadius,
                    pigmentRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    1,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Wind)
        {
            int method = values.SymbolCode(
                "Method",
                ("Wind", 0),
                ("Blast", 1),
                ("Stagger", 2));
            float methodScale = method switch
            {
                1 => 5.5f,
                2 => 4.5f,
                _ => 4
            };
            float radius = Math.Clamp(
                MathF.Abs(values.Number("Strength")) *
                    deviceScale *
                    methodScale,
                0,
                64);
            if (radius == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    radius,
                    radius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.AngledStrokes)
        {
            float radius = Math.Clamp(
                values.Number("StrokeLength"),
                1,
                50) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SprayedStrokes)
        {
            float strokeLength = MathF.Max(
                values.Number("StrokeLength"),
                0);
            float sprayRadius = MathF.Max(
                values.Number("SprayRadius"),
                0);
            float brushRadius = MathF.Max(
                0.75f,
                (strokeLength * 0.08f) +
                    (sprayRadius * 0.2f));
            float radius =
                ((strokeLength * 0.5f) +
                    sprayRadius +
                    brushRadius) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: strokeLength == 0 && sprayRadius == 0)
            ];
        }

        if (filter == PrismFilterId.RoughPastels)
        {
            float coarseRadius = Math.Clamp(
                MathF.Max(values.Number("StrokeLength"), 1),
                1,
                12) *
                deviceScale;
            float detailMix = Math.Clamp(
                values.Number("StrokeDetail"),
                0,
                16) /
                16;
            float fineRadius = MathF.Max(
                coarseRadius *
                    (0.25f + (0.25f * detailMix)),
                1);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    coarseRadius,
                    coarseRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    fineRadius,
                    fineRadius,
                    0,
                    0,
                    1,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SmudgeStick)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("StrokeLength"), 0),
                0,
                12) *
                deviceScale;
            float intensity = Math.Clamp(
                values.Number("Intensity"),
                0,
                10);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0 || intensity == 0)
            ];
        }

        if (filter == PrismFilterId.Sponge)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("BrushSize"), 0),
                0,
                12) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0)
            ];
        }

        if (filter == PrismFilterId.Underpainting)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("BrushSize"), 0),
                0,
                12) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.BasRelief or
            PrismFilterId.PosterEdges)
        {
            float requestedRadius = filter == PrismFilterId.BasRelief
                ? values.Number("Smoothness")
                : values.Number("EdgeThickness");
            float radius = MathF.Round(Math.Clamp(
                MathF.Max(requestedRadius, 0) *
                    deviceScale,
                1,
                8));
            float finalRadius = filter == PrismFilterId.BasRelief
                ? 1
                : radius;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    finalRadius,
                    finalRadius,
                    0,
                    0,
                    5,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.AccentedEdges or
            PrismFilterId.DarkStrokes or
            PrismFilterId.InkOutlines)
        {
            Vector4 gaussian = filter switch
            {
                PrismFilterId.AccentedEdges =>
                    AccentedEdgesGaussianSettings(values, deviceScale),
                PrismFilterId.InkOutlines =>
                    InkOutlinesGaussianSettings(values, deviceScale),
                _ => XDogGaussianSettings(deviceScale)
            };
            float radius = gaussian.W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.GlowingEdges)
        {
            Vector4 settings = GlowingEdgesSettings(values, deviceScale);
            float edgeRadius = settings.X;
            float gaussianRadius = settings.Z;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    edgeRadius,
                    edgeRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    gaussianRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    gaussianRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Cutout)
        {
            float simplicity = Math.Clamp(
                values.Number("EdgeSimplicity"),
                0,
                10);
            int iterations = Math.Clamp(
                (int)MathF.Ceiling(simplicity * 0.5f),
                1,
                4);
            float radius = Math.Clamp(
                1 + (simplicity * 0.75f),
                1,
                8) *
                deviceScale;
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    iterations + 1);
            for (int index = 0; index < iterations; index++)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        radius,
                        radius,
                        0,
                        0,
                        index,
                        IsNoOp: false));
            }
            passes.Add(
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    iterations,
                    IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.ColoredPencil)
        {
            float pencilWidth = Math.Clamp(
                values.Number("PencilWidth"),
                0,
                12);
            float tensorBlurRadius = Math.Clamp(
                pencilWidth * 0.5f,
                1,
                4) *
                deviceScale;
            float licRadius = pencilWidth * deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    deviceScale,
                    deviceScale,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    tensorBlurRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    tensorBlurRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    licRadius,
                    licRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Fresco)
        {
            float brushRadius = Math.Clamp(
                MathF.Max(
                    values.Number("BrushSize"),
                    1) *
                deviceScale,
                1,
                6);
            float tensorBlurRadius = Math.Clamp(
                brushRadius * 0.5f,
                1,
                4);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    deviceScale,
                    deviceScale,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    tensorBlurRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    tensorBlurRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    brushRadius,
                    brushRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.DryBrush)
        {
            float radius = MathF.Min(
                MathF.Max(
                    values.Number("BrushSize"),
                    1),
                6) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.PaletteKnife)
        {
            float radius = MathF.Max(
                values.Number("StrokeSize"),
                0) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0)
            ];
        }

        if (filter == PrismFilterId.PaintDaubs)
        {
            float radius = Math.Clamp(
                values.Number("BrushSize"),
                1,
                50) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter is PrismFilterId.Maximum or PrismFilterId.Minimum)
        {
            int morphologyShape = filter == PrismFilterId.Maximum
                ? values.SymbolCode(
                    "Preserve",
                    ("Roundness", 0))
                : values.SymbolCode(
                    "Preserve",
                    ("Roundness", 0),
                    ("Squareness", 1));

            float radius = MorphologyRadius(
                values.Number("Radius"),
                deviceScale,
                sourceBounds);
            if (radius == 0 ||
                (sourceBounds.Width <= 0 && sourceBounds.Height <= 0))
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            float radiusX = sourceBounds.Width > 0
                ? radius
                : 0;
            float radiusY = sourceBounds.Height > 0
                ? radius
                : 0;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radiusX,
                    radiusY,
                    radiusX / pixelScale,
                    radiusY / pixelScale,
                    morphologyShape,
                    IsNoOp: false)
            ];
        }

        if (filter is PrismFilterId.Facet or PrismFilterId.Diffuse)
        {
            int iterations = IterationCount(
                values.Number("Iterations"),
                filter);
            if (iterations == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    iterations);
            float iterationSampleRadius =
                filter == PrismFilterId.Facet
                    ? 6
                    : deviceScale;
            float iterationBoundsRadius =
                filter == PrismFilterId.Facet
                    ? 6 / pixelScale
                    : (2 * deviceScale) / pixelScale;
            for (int index = 0; index < iterations; index++)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        iterationSampleRadius,
                        iterationSampleRadius,
                        iterationBoundsRadius,
                        iterationBoundsRadius,
                        index,
                        IsNoOp: false));
            }
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.Fragment)
        {
            float offset =
                MathF.Max(0, values.Number("Offset")) *
                deviceScale;
            if (offset == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            float boundsRadius = offset / pixelScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    offset,
                    offset,
                    boundsRadius,
                    boundsRadius,
                    0,
                    IsNoOp: false)
            ];
        }

        float sampleRadius =
            SampleRadius(filter, primitive, values) * deviceScale;
        return
        [
            new(
                PrismCatalogFilterPassKind.Direct,
                sampleRadius,
                sampleRadius,
                0,
                0,
                0,
                IsNoOp: false)
        ];
    }

    private static float SampleRadius(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismFilterParameterReader values)
    {
        return filter switch
        {
            PrismFilterId.Crosshatch => 0,
            PrismFilterId.Craquelure => 0,
            PrismFilterId.Texturizer => 0,
            PrismFilterId.Reticulation => 0,
            PrismFilterId.Spatter => 0,
            PrismFilterId.ColorHalftone =>
                MathF.Max(0, values.Number("MaxRadius")),
            PrismFilterId.ChromaticAberration =>
                ChromaticAberrationSampleRadius(values),
            PrismFilterId.Deinterlace => 9,
            PrismFilterId.CustomConvolution => 1,
            PrismFilterId.FindEdges => 1,
            PrismFilterId.Emboss =>
                MathF.Max(1, values.Number("Height")),
            PrismFilterId.PlasticWrap =>
                1 +
                (2 * Math.Clamp(
                    values.Number("Smoothness") / 15,
                    0,
                    1)),
            PrismFilterId.OilPaint =>
                1 +
                (2 * Math.Clamp(
                    values.Number("Scale"),
                    0,
                    3)),
            PrismFilterId.TraceContour => 1,
            _ when primitive is
                PrismCatalogFilterPrimitive.Artistic or
                PrismCatalogFilterPrimitive.EdgeDetection or
                PrismCatalogFilterPrimitive.Texture => 1,
            _ => 0
        };
    }

    private static float ChromaticAberrationSampleRadius(
        PrismFilterParameterReader values)
    {
        float amount = MathF.Abs(values.Number("Amount"));
        if (!values.Boolean("Radial"))
        {
            return amount;
        }

        Vector4 center = values.Vector("Center");
        float left = MathF.Max(
            center.X * center.X,
            (1 - center.X) * (1 - center.X));
        float top = MathF.Max(
            center.Y * center.Y,
            (1 - center.Y) * (1 - center.Y));
        return amount * 2 * MathF.Sqrt(left + top);
    }

    private static Vector4 AccentedEdgesGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            MathF.Max(values.Number("EdgeWidth"), 0) *
            deviceScale *
                0.5f);

    private static Vector4 GlowingEdgesSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float edgeRadius = Math.Clamp(
            MathF.Round(
                MathF.Max(values.Number("EdgeWidth"), 1) *
                deviceScale),
            1,
            8);
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15);
        float sigma = Math.Clamp(
            (0.65f + (smoothness * 0.18f)) * deviceScale,
            0.5f,
            4);
        float gaussianRadius = Math.Clamp(
            MathF.Ceiling(sigma * 2),
            1,
            8);
        float haloMix = 0.35f + (0.65f * (smoothness / 15));
        return new Vector4(
            edgeRadius,
            sigma,
            gaussianRadius,
            haloMix);
    }

    private static Vector4 ChromeSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15);
        float sigma = Math.Clamp(
            (0.55f + (smoothness * 0.18f)) * deviceScale,
            0.5f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(sigma * 2),
            1,
            8);
        float detailGain = 1 + (detail * 0.75f);
        float reflectionWidth =
            0.035f + ((smoothness / 15) * 0.08f);
        return new Vector4(
            sigma,
            radius,
            detailGain,
            reflectionWidth);
    }

    private static Vector4 NotePaperSettings(
        PrismFilterParameterReader values) =>
        new(
            Math.Clamp(values.Number("ImageBalance"), 0, 50) / 50,
            Math.Clamp(values.Number("Graininess"), 0, 20) / 20,
            Math.Clamp(values.Number("Relief"), 0, 50) / 50,
            0);

    private static Vector4 PlasterSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float balance = Math.Clamp(
            values.Number("ImageBalance"),
            0,
            50) / 50;
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            0,
            15) / 15;
        float radius = Math.Clamp(
            MathF.Ceiling(
                (1 + (smoothness * 7)) *
                MathF.Sqrt(deviceScale)),
            1,
            12);
        float epsilon = float.Lerp(
            0.0004f,
            0.014f,
            smoothness * smoothness);
        float normalStrength = float.Lerp(7, 3.5f, smoothness);
        return new Vector4(
            balance,
            radius,
            epsilon,
            normalStrength);
    }

    private static Vector4 PhotocopyXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 24);
        float sigma = Math.Clamp(
            (1.2f / MathF.Sqrt(1 + (detail * 0.22f))) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.8f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float epsilon = Math.Clamp(
            values.Number("Darkness") / 40,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, epsilon);
    }

    private static Vector4 StampXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = Math.Clamp(
            values.Number("Smoothness"),
            1,
            50);
        float normalizedSmoothness = (smoothness - 1) / 49;
        float sigma = Math.Clamp(
            float.Lerp(0.5f, 3.75f, normalizedSmoothness) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.8f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float epsilon = Math.Clamp(
            values.Number("LightDarkBalance") / 50,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, epsilon);
    }

    private static Vector4 TornEdgesXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = TornEdgesNormalizedSmoothness(values);
        float sigma = Math.Clamp(
            float.Lerp(0.65f, 2.6f, smoothness) * deviceScale,
            0.5f,
            3.75f);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            4);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 3),
            1,
            12);
        float threshold = Math.Clamp(
            values.Number("ImageBalance") / 50,
            0,
            1);
        return new Vector4(sigma, extendedSigma, radius, threshold);
    }

    private static Vector4 TornEdgesNoiseSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float smoothness = TornEdgesNormalizedSmoothness(values);
        float contrast = Math.Clamp(
            (values.Number("Contrast") - 1) / 24,
            0,
            1);
        float sharpen = float.Lerp(8, 48, contrast);
        float amplitude = float.Lerp(0.16f, 0.035f, smoothness);
        float frequency = Math.Clamp(
            float.Lerp(0.18f, 0.055f, smoothness) / deviceScale,
            0.01f,
            0.5f);
        float transitionWidth = float.Lerp(0.2f, 0.07f, contrast);
        return new Vector4(
            sharpen,
            amplitude,
            frequency,
            transitionWidth);
    }

    private static float TornEdgesNormalizedSmoothness(
        PrismFilterParameterReader values) =>
        Math.Clamp(
            (values.Number("Smoothness") - 1) / 14,
            0,
            1);

    private static Vector4 ReticulationSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float density = Math.Clamp(
            values.Number("Density"),
            0,
            50) / 50;
        float cellSize = Math.Clamp(
            float.Lerp(18, 3, density) * deviceScale,
            2,
            256);
        float foregroundLevel = Math.Clamp(
            values.Number("ForegroundLevel"),
            0,
            50) / 50;
        float backgroundLevel = Math.Clamp(
            values.Number("BackgroundLevel"),
            0,
            50) / 50;
        return new Vector4(
            cellSize,
            foregroundLevel,
            backgroundLevel,
            0);
    }

    private static Vector4 CraquelureSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float cellSize = Math.Clamp(
            MathF.Abs(values.Number("CrackSpacing")) * deviceScale,
            2,
            256);
        float depth = Math.Clamp(values.Number("CrackDepth"), 0, 10) / 10;
        float brightness = Math.Clamp(
            values.Number("CrackBrightness"),
            0,
            10) / 10;
        float crackWidth = float.Lerp(0.035f, 0.16f, depth);
        return new Vector4(cellSize, crackWidth, depth, brightness);
    }

    private static void GrainSettings(
        PrismFilterParameterReader values,
        float deviceScale,
        out Vector4 model,
        out Vector4 shape)
    {
        int type = values.SymbolCode(
            "Type",
            ("Regular", 0),
            ("Soft", 1),
            ("Sprinkles", 2),
            ("Clumped", 3),
            ("Contrasty", 4),
            ("Enlarged", 5),
            ("Stippled", 6),
            ("Horizontal", 7),
            ("Vertical", 8),
            ("Speckle", 9));
        (float radiusX, float radiusY, float softness, float gain) =
            type switch
            {
                0 => (1.15f, 1.15f, 0.2f, 1f),
                1 => (1.4f, 1.4f, 0.45f, 0.75f),
                2 => (0.65f, 0.65f, 0.1f, 1.2f),
                3 => (1.8f, 1.8f, 0.18f, 1.1f),
                4 => (1.05f, 1.05f, 0.08f, 1.35f),
                5 => (2.4f, 2.4f, 0.22f, 1f),
                6 => (0.75f, 0.75f, 0.05f, 1.1f),
                7 => (1.8f, 0.7f, 0.12f, 1f),
                8 => (0.7f, 1.8f, 0.12f, 1f),
                _ => (0.45f, 0.45f, 0.04f, 1.25f)
            };
        radiusX *= deviceScale;
        radiusY *= deviceScale;
        float cellSize = Math.Clamp(
            MathF.Max(2 * deviceScale, 2.5f * MathF.Max(radiusX, radiusY)),
            1,
            256);
        model = new Vector4(
            Math.Clamp(values.Number("Intensity"), 0, 100) / 100,
            Math.Clamp(values.Number("Contrast"), 0, 100) / 100,
            type,
            cellSize);
        shape = new Vector4(radiusX, radiusY, softness, gain);
    }

    private static Vector4 ChalkCharcoalGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float charcoalSigma = Math.Clamp(
            (0.5f +
                (MathF.Max(values.Number("CharcoalArea"), 0) * 0.25f)) *
            deviceScale,
            0.5f,
            4);
        float requestedChalkSigma = Math.Clamp(
            (0.5f +
                (MathF.Max(values.Number("ChalkArea"), 0) * 0.25f)) *
            deviceScale,
            0.5f,
            6.4f);
        float chalkSigma = Math.Clamp(
            MathF.Max(requestedChalkSigma, charcoalSigma + 0.25f),
            charcoalSigma,
            6.4f);
        float charcoalRadius = Math.Clamp(
            MathF.Ceiling(charcoalSigma * 2),
            1,
            8);
        float chalkRadius = Math.Clamp(
            MathF.Ceiling(chalkSigma * 2),
            charcoalRadius,
            8);
        return new Vector4(
            charcoalSigma,
            chalkSigma,
            charcoalRadius,
            chalkRadius);
    }

    private static Vector4 CharcoalFdogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float thickness = Math.Clamp(
            MathF.Abs(values.Number("CharcoalThickness")),
            0.25f,
            4);
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float sigma = Math.Clamp(
            (0.55f + (thickness * 0.45f)) * deviceScale,
            0.5f,
            4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round((3 + (detail * 0.6f)) * deviceScale),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 CharcoalEtfSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float detail = Math.Clamp(values.Number("Detail"), 0, 10);
        float radius = Math.Clamp(
            MathF.Round((2 + (detail * 0.2f)) * deviceScale),
            2,
            4);
        float refinementCount = 1 + MathF.Round(detail / 5);
        float thresholdSlope = 18 + (detail * 2.4f);
        return new Vector4(radius, refinementCount, 0.98f, thresholdSlope);
    }

    private static Vector4 ConteCrayonXDogSettings(float deviceScale)
    {
        float sigma = Math.Clamp(0.85f * deviceScale, 0.5f, 4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round(5 * MathF.Sqrt(deviceScale)),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 GraphicPenXDogSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float strokeLength = Math.Clamp(
            MathF.Abs(values.Number("StrokeLength")) * deviceScale,
            1,
            96);
        float sigma = Math.Clamp(
            0.65f + (MathF.Min(strokeLength, 32) * 0.025f),
            0.5f,
            4);
        float extendedSigma = Math.Clamp(
            sigma * 1.6f,
            sigma + 0.25f,
            6.4f);
        float normalRadius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            2,
            8);
        float flowRadius = Math.Clamp(
            MathF.Round(4 + MathF.Sqrt(strokeLength) * 0.45f),
            3,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            normalRadius,
            flowRadius);
    }

    private static Vector4 GraphicPenEtfSettings() =>
        new(3, 1, 0.98f, 0);

    private static Vector3 MosaicTilesSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float tileSize = Math.Clamp(
            values.Number("TileSize") * deviceScale,
            1,
            16384);
        float groutWidth = Math.Clamp(
            values.Number("GroutWidth") * deviceScale,
            0,
            tileSize);
        float lightenGrout = Math.Clamp(
            values.Number("LightenGrout") / 10,
            0,
            1);
        return new Vector3(tileSize, groutWidth, lightenGrout);
    }

    private static Vector2 PatchworkSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float squareSize = Math.Clamp(
            values.Number("SquareSize") * deviceScale,
            1,
            16384);
        float relief = Math.Clamp(
            values.Number("Relief") / 50,
            0,
            1);
        return new Vector2(squareSize, relief);
    }

    private static Vector3 StainedGlassSettings(
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float cellSize = Math.Clamp(
            MathF.Abs(values.Number("CellSize")) * deviceScale,
            2,
            16384);
        float borderThickness = Math.Clamp(
            MathF.Abs(values.Number("BorderThickness")) * deviceScale,
            0,
            1024);
        float lightIntensity = Math.Clamp(
            values.Number("LightIntensity"),
            0,
            10);
        return new Vector3(
            cellSize,
            borderThickness,
            lightIntensity);
    }

    private static Vector4 InkOutlinesGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            MathF.Max(values.Number("StrokeLength"), 0) *
                deviceScale *
                0.25f);

    private static Vector4 SumiEGaussianSettings(
        PrismFilterParameterReader values,
        float deviceScale) =>
        XDogGaussianSettings(
            (0.65f +
                (MathF.Max(values.Number("StrokeWidth"), 0) * 0.08f)) *
            deviceScale);

    private static Vector4 XDogGaussianSettings(float requestedSigma)
    {
        float sigma = Math.Clamp(requestedSigma, 0.5f, 4);
        float extendedSigma = MathF.Min(sigma * 1.6f, 6.4f);
        float radius = Math.Clamp(
            MathF.Ceiling(extendedSigma * 2),
            1,
            8);
        return new Vector4(
            sigma,
            extendedSigma,
            Math.Clamp(MathF.Ceiling(sigma * 2), 1, 8),
            radius);
    }

    private static float MorphologyRadius(
        float radius,
        float deviceScale,
        DrawRect sourceBounds)
    {
        double scaledRadius = (double)radius * deviceScale;
        double width = Math.Max(sourceBounds.Width, 0) *
            deviceScale;
        double height = Math.Max(sourceBounds.Height, 0) *
            deviceScale;
        double maximumRelevantRadius = Math.Sqrt(
            (width * width) + (height * height));
        return (float)Math.Min(
            Math.Min(scaledRadius, maximumRelevantRadius),
            float.MaxValue);
    }

    private static int IterationCount(
        float value,
        PrismFilterId filter)
    {
        if (!float.IsFinite(value) ||
            value < 0 ||
            MathF.Truncate(value) != value ||
            value > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' requires an integral iteration count " +
                "representable by the runtime.");
        }
        return (int)value;
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

    private static void ValidateSlot(
        PrismFilterId filter,
        PrismCatalogPropertyDescriptor property,
        PrismGraphParameter parameter,
        int index)
    {
        PrismGraphParameterValueKind expected =
            property.ValueType switch
            {
                PrismCatalogValueType.Boolean =>
                    PrismGraphParameterValueKind.Boolean,
                PrismCatalogValueType.Integer =>
                    PrismGraphParameterValueKind.Integer,
                PrismCatalogValueType.Number =>
                    PrismGraphParameterValueKind.Number,
                PrismCatalogValueType.Color =>
                    PrismGraphParameterValueKind.Color,
                PrismCatalogValueType.Vector =>
                    PrismGraphParameterValueKind.Vector,
                PrismCatalogValueType.Symbol =>
                    PrismGraphParameterValueKind.Symbol,
                PrismCatalogValueType.Resource =>
                    PrismGraphParameterValueKind.Resource,
                _ => throw new InvalidOperationException(
                    $"Filter '{filter}' has an unknown generated property type.")
            };
        if (property.Slot != index ||
            parameter.Index != index ||
            parameter.Kind != expected)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' property '{property.Name}' does not " +
                "match its generated slot and value type.");
        }
    }

    private static bool TryGetPrimitive(
        PrismFilterId filter,
        out PrismCatalogFilterPrimitive primitive)
    {
        int stableId = (int)filter;
        primitive = stableId switch
        {
            55 or 56 =>
                PrismCatalogFilterPrimitive.Morphology,
            >= 63 and <= 69 =>
                PrismCatalogFilterPrimitive.Quantization,
            >= 70 and <= 74 or 106 or 114 =>
                PrismCatalogFilterPrimitive.Procedural,
            75 or 76 or 134 =>
                PrismCatalogFilterPrimitive.Video,
            >= 77 and <= 99 or 113 =>
                PrismCatalogFilterPrimitive.Artistic,
            >= 100 and <= 105 or >= 107 and <= 112 or
                115 or 117 or 118 or 121 =>
                PrismCatalogFilterPrimitive.EdgeDetection,
            116 =>
                PrismCatalogFilterPrimitive.Extrude,
            120 or 133 =>
                PrismCatalogFilterPrimitive.Tiling,
            122 =>
                PrismCatalogFilterPrimitive.LineIntegralConvolution,
            >= 123 and <= 129 =>
                PrismCatalogFilterPrimitive.Texture,
            130 =>
                PrismCatalogFilterPrimitive.Convolution,
            119 or 131 or 132 =>
                PrismCatalogFilterPrimitive.Color,
            _ => (PrismCatalogFilterPrimitive)(-1)
        };
        return (int)primitive >= 0;
    }
}
