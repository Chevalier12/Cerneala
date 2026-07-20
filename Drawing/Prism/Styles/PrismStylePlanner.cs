using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Styles;

internal enum PrismStylePaintKind
{
    Color,
    Gradient,
    Pattern
}

[Flags]
internal enum PrismStyleFlags
{
    None = 0,
    AntiAlias = 1 << 0,
    Reverse = 1 << 1,
    Dither = 1 << 2,
    Invert = 1 << 3,
    Knockout = 1 << 4,
    AlignWithLayer = 1 << 5,
    TextureEnabled = 1 << 6,
    TextureInvert = 1 << 7,
    ContourEnabled = 1 << 8,
    ResourceLinked = 1 << 9,
    ContourAntiAlias = 1 << 10
}

internal readonly record struct PrismStyleSamplingGeometry(
    Vector2 Offset,
    float Size,
    float Spread,
    float Soften);

internal readonly record struct PrismStylePlan
{
    public PrismStylePlan(PrismStyleId style, int kind)
    {
        this = default;
        Style = style;
        Kind = kind;
        BlendMode = PrismBlendMode.Normal;
        SecondaryBlendMode = PrismBlendMode.Normal;
        PrimaryColor = Vector4.One;
        SecondaryColor = Vector4.One;
        Opacity = 1;
        SecondaryOpacity = 1;
        Range = 1;
        Scale = 1;
    }

    public PrismStyleId Style { get; init; }

    public int Kind { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public PrismBlendMode SecondaryBlendMode { get; init; }

    public PrismStylePaintKind PaintKind { get; init; }

    public Vector4 PrimaryColor { get; init; }

    public Vector4 SecondaryColor { get; init; }

    public float Opacity { get; init; }

    public float SecondaryOpacity { get; init; }

    public float Angle { get; init; }

    public float Altitude { get; init; }

    public float Distance { get; init; }

    public float Size { get; init; }

    public float Spread { get; init; }

    public float Soften { get; init; }

    public float Depth { get; init; }

    public float Range { get; init; }

    public float Noise { get; init; }

    public float Jitter { get; init; }

    public float Scale { get; init; }

    public float TextureDepth { get; init; }

    public Vector2 Offset { get; init; }

    public int Contour { get; init; }

    public int DetailContour { get; init; }

    public int Technique { get; init; }

    public int Position { get; init; }

    public int Origin { get; init; }

    public int Direction { get; init; }

    public int GradientMethod { get; init; }

    public int GradientStyle { get; init; }

    public int BevelStyle { get; init; }

    public PrismStyleFlags Flags { get; init; }

    public PrismResourceId Resource { get; init; }

    public bool ResourceEnabled { get; init; }

    public bool ResourceRequired { get; init; }
}

internal static class PrismStylePlanner
{
    private const int DropShadowKind = 0;
    private const int InnerShadowKind = 1;
    private const int OuterGlowKind = 2;
    private const int InnerGlowKind = 3;
    private const int BevelEmbossKind = 4;
    private const int SatinKind = 5;
    private const int ColorOverlayKind = 6;
    private const int GradientOverlayKind = 7;
    private const int PatternOverlayKind = 8;
    private const int StrokeKind = 9;

    private static readonly int LinearSymbol =
        PrismCatalogRuntime.ResolveSymbol("Contour", "Linear");
    private static readonly int GaussianSymbol =
        PrismCatalogRuntime.ResolveSymbol("Contour", "Gaussian");
    private static readonly int SofterSymbol =
        PrismCatalogRuntime.ResolveSymbol("Technique", "Softer");
    private static readonly int PreciseSymbol =
        PrismCatalogRuntime.ResolveSymbol("Technique", "Precise");
    private static readonly int SmoothSymbol =
        PrismCatalogRuntime.ResolveSymbol("Technique", "Smooth");
    private static readonly int OutsideSymbol =
        PrismCatalogRuntime.ResolveSymbol("Position", "Outside");
    private static readonly int CenterSymbol =
        PrismCatalogRuntime.ResolveSymbol("Position", "Center");
    private static readonly int InsideSymbol =
        PrismCatalogRuntime.ResolveSymbol("Position", "Inside");
    private static readonly int EdgeSymbol =
        PrismCatalogRuntime.ResolveSymbol("Origin", "Edge");
    private static readonly int UpSymbol =
        PrismCatalogRuntime.ResolveSymbol("Direction", "Up");
    private static readonly int ColorSymbol =
        PrismCatalogRuntime.ResolveSymbol("FillType", "Color");
    private static readonly int GradientSymbol =
        PrismCatalogRuntime.ResolveSymbol("FillType", "Gradient");
    private static readonly int PatternSymbol =
        PrismCatalogRuntime.ResolveSymbol("FillType", "Pattern");
    private static readonly int PerceptualSymbol =
        PrismCatalogRuntime.ResolveSymbol("Method", "Perceptual");
    private static readonly int RadialSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "Radial");
    private static readonly int AngleSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "Angle");
    private static readonly int ReflectedSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "Reflected");
    private static readonly int DiamondSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "Diamond");
    private static readonly int OuterBevelSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "OuterBevel");
    private static readonly int EmbossSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "Emboss");
    private static readonly int PillowEmbossSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "PillowEmboss");
    private static readonly int StrokeEmbossSymbol =
        PrismCatalogRuntime.ResolveSymbol("Style", "StrokeEmboss");
    private static readonly (int Symbol, PrismBlendMode Mode)[]
        HashedBlendModes = Enum
            .GetValues<PrismBlendMode>()
            .Select(mode => (
                PrismCatalogRuntime.ResolveSymbol(
                    "HighlightMode",
                    mode.ToString()),
                mode))
            .ToArray();

    public static PrismStylePlan Create(
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Kind != PrismGraphNodeKind.Style ||
            node.Style is not PrismStyleId style)
        {
            throw new ArgumentException(
                "A layer-style plan requires a style graph node.",
                nameof(node));
        }

        ParameterReader parameters = new(node, style);
        return style switch
        {
            PrismStyleId.DropShadow =>
                CreateDropShadow(parameters, scope),
            PrismStyleId.InnerShadow =>
                CreateInnerShadow(parameters, scope),
            PrismStyleId.OuterGlow =>
                CreateOuterGlow(parameters, scope),
            PrismStyleId.InnerGlow =>
                CreateInnerGlow(parameters, scope),
            PrismStyleId.BevelEmboss =>
                CreateBevelEmboss(parameters, scope),
            PrismStyleId.Satin =>
                CreateSatin(parameters, scope),
            PrismStyleId.ColorOverlay =>
                CreateColorOverlay(parameters, scope),
            PrismStyleId.GradientOverlay =>
                CreateGradientOverlay(parameters, scope),
            PrismStyleId.PatternOverlay =>
                CreatePatternOverlay(parameters),
            PrismStyleId.Stroke =>
                CreateStroke(parameters, scope),
            _ => throw new ArgumentOutOfRangeException(
                nameof(node),
                style,
                "Unknown Prism layer style.")
        };
    }

    public static PrismStyleSamplingGeometry ResolveSamplingGeometry(
        in PrismStylePlan plan,
        PrismGraphScope scope)
    {
        float scale = ResolveSpatialScale(scope);
        float radians = plan.Angle * (MathF.PI / 180f);
        Vector2 offset = new(
            MathF.Cos(radians) * plan.Distance * scale,
            -MathF.Sin(radians) * plan.Distance * scale);
        return new PrismStyleSamplingGeometry(
            offset,
            checked(plan.Size * scale),
            checked(plan.Spread * scale),
            checked(plan.Soften * scale));
    }

    public static DrawRect ExpandBounds(
        in PrismStylePlan plan,
        PrismGraphScope scope,
        DrawRect bounds)
    {
        PrismStyleSamplingGeometry geometry =
            ResolveSamplingGeometry(plan, scope);
        return plan.Style switch
        {
            PrismStyleId.DropShadow => Union(
                bounds,
                Translate(
                    Inflate(
                        bounds,
                        checked(geometry.Size + geometry.Spread)),
                    geometry.Offset.X,
                    geometry.Offset.Y)),
            PrismStyleId.OuterGlow => Inflate(
                bounds,
                checked(geometry.Size + geometry.Spread)),
            PrismStyleId.BevelEmboss => Inflate(
                bounds,
                checked(geometry.Size + geometry.Soften)),
            PrismStyleId.Stroke when plan.Position == 0 =>
                Inflate(bounds, geometry.Size),
            PrismStyleId.Stroke when plan.Position == 1 =>
                Inflate(bounds, geometry.Size * 0.5f),
            _ => bounds
        };
    }

    private static PrismStylePlan CreateDropShadow(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return new PrismStylePlan(
            PrismStyleId.DropShadow,
            DropShadowKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            Angle = ResolveAngle(parameters, scope),
            Distance = parameters.Number("Distance"),
            Spread = parameters.Number("Spread"),
            Size = parameters.Number("Size"),
            Contour = ContourCode(parameters.Symbol("Contour")),
            Noise = parameters.Number("Noise"),
            Flags =
                Flag(
                    parameters.Boolean("AntiAlias"),
                    PrismStyleFlags.AntiAlias) |
                Flag(
                    parameters.Boolean("LayerKnocksOut"),
                    PrismStyleFlags.Knockout)
        };
    }

    private static PrismStylePlan CreateInnerShadow(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return new PrismStylePlan(
            PrismStyleId.InnerShadow,
            InnerShadowKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            Angle = ResolveAngle(parameters, scope),
            Distance = parameters.Number("Distance"),
            Spread = parameters.Number("Choke"),
            Size = parameters.Number("Size"),
            Contour = ContourCode(parameters.Symbol("Contour")),
            Noise = parameters.Number("Noise"),
            Flags = Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias)
        };
    }

    private static PrismStylePlan CreateOuterGlow(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismResourceId gradient = parameters.Resource("Gradient");
        return new PrismStylePlan(
            PrismStyleId.OuterGlow,
            OuterGlowKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            PaintKind = gradient.Value > 0
                ? PrismStylePaintKind.Gradient
                : PrismStylePaintKind.Color,
            Opacity = parameters.Number("Opacity"),
            Noise = parameters.Number("Noise"),
            Technique = TechniqueCode(
                parameters.Symbol("Technique")),
            Spread = parameters.Number("Spread"),
            Size = parameters.Number("Size"),
            Contour = ContourCode(parameters.Symbol("Contour")),
            Range = parameters.Number("Range"),
            Jitter = parameters.Number("Jitter"),
            Flags = Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias),
            Resource = gradient,
            ResourceEnabled = gradient.Value > 0,
            ResourceRequired = gradient.Value > 0
        };
    }

    private static PrismStylePlan CreateInnerGlow(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismResourceId gradient = parameters.Resource("Gradient");
        return new PrismStylePlan(
            PrismStyleId.InnerGlow,
            InnerGlowKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            PaintKind = gradient.Value > 0
                ? PrismStylePaintKind.Gradient
                : PrismStylePaintKind.Color,
            Opacity = parameters.Number("Opacity"),
            Noise = parameters.Number("Noise"),
            Technique = TechniqueCode(
                parameters.Symbol("Technique")),
            Origin = OriginCode(parameters.Symbol("Origin")),
            Spread = parameters.Number("Choke"),
            Size = parameters.Number("Size"),
            Contour = ContourCode(parameters.Symbol("Contour")),
            Range = parameters.Number("Range"),
            Jitter = parameters.Number("Jitter"),
            Flags = Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias),
            Resource = gradient,
            ResourceEnabled = gradient.Value > 0,
            ResourceRequired = gradient.Value > 0
        };
    }

    private static PrismStylePlan CreateBevelEmboss(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        bool textureEnabled =
            parameters.Boolean("TextureEnabled");
        PrismResourceId pattern = parameters.Resource("Pattern");
        PrismStyleFlags flags =
            Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias) |
            Flag(
                parameters.Boolean("ContourEnabled"),
                PrismStyleFlags.ContourEnabled) |
            Flag(
                parameters.Boolean("ContourAntiAlias"),
                PrismStyleFlags.ContourAntiAlias) |
            Flag(
                textureEnabled,
                PrismStyleFlags.TextureEnabled) |
            Flag(
                parameters.Boolean("TextureInvert"),
                PrismStyleFlags.TextureInvert) |
            Flag(
                parameters.Boolean("TextureLinkWithLayer"),
                PrismStyleFlags.ResourceLinked);
        return new PrismStylePlan(
            PrismStyleId.BevelEmboss,
            BevelEmbossKind)
        {
            BevelStyle = BevelStyleCode(parameters.Symbol("Style")),
            Technique = TechniqueCode(
                parameters.Symbol("Technique")),
            Depth = parameters.Number("Depth"),
            Direction = DirectionCode(
                parameters.Symbol("Direction")),
            Size = parameters.Number("Size"),
            Soften = parameters.Number("Soften"),
            Angle = ResolveAngle(parameters, scope),
            Altitude = ResolveAltitude(parameters, scope),
            Contour = ContourCode(
                parameters.Symbol("GlossContour")),
            BlendMode = parameters.BlendMode("HighlightMode"),
            PrimaryColor = parameters.Color(
                "HighlightColor",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("HighlightOpacity"),
            SecondaryBlendMode =
                parameters.BlendMode("ShadowMode"),
            SecondaryColor = parameters.Color(
                "ShadowColor",
                scope.CompositionSettings.WorkingColorProfile),
            SecondaryOpacity =
                parameters.Number("ShadowOpacity"),
            DetailContour = ContourCode(
                parameters.Symbol("Contour")),
            Range = parameters.Number("ContourRange"),
            PaintKind = textureEnabled
                ? PrismStylePaintKind.Pattern
                : PrismStylePaintKind.Color,
            Resource = pattern,
            ResourceEnabled = textureEnabled,
            ResourceRequired = textureEnabled,
            Scale = parameters.Number("TextureScale"),
            TextureDepth = parameters.Number("TextureDepth"),
            Offset = parameters.Vector2("TextureOffset"),
            Flags = flags
        };
    }

    private static PrismStylePlan CreateSatin(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return new PrismStylePlan(
            PrismStyleId.Satin,
            SatinKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            Angle = parameters.Number("Angle"),
            Distance = parameters.Number("Distance"),
            Size = parameters.Number("Size"),
            Contour = ContourCode(parameters.Symbol("Contour")),
            Flags =
                Flag(
                    parameters.Boolean("AntiAlias"),
                    PrismStyleFlags.AntiAlias) |
                Flag(
                    parameters.Boolean("Invert"),
                    PrismStyleFlags.Invert)
        };
    }

    private static PrismStylePlan CreateColorOverlay(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return new PrismStylePlan(
            PrismStyleId.ColorOverlay,
            ColorOverlayKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            PaintKind = PrismStylePaintKind.Color
        };
    }

    private static PrismStylePlan CreateGradientOverlay(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return new PrismStylePlan(
            PrismStyleId.GradientOverlay,
            GradientOverlayKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            Opacity = parameters.Number("Opacity"),
            PrimaryColor = parameters.ColorConstant(
                new Color(0, 0, 0),
                scope.CompositionSettings.WorkingColorProfile),
            SecondaryColor = parameters.ColorConstant(
                new Color(255, 255, 255),
                scope.CompositionSettings.WorkingColorProfile),
            PaintKind = PrismStylePaintKind.Gradient,
            DetailContour = StableSymbolCode(
                parameters.Symbol("Gradient")),
            GradientMethod = GradientMethodCode(
                parameters.Symbol("Method")),
            GradientStyle = GradientStyleCode(
                parameters.Symbol("Style")),
            Angle = parameters.Number("Angle"),
            Scale = parameters.Number("Scale"),
            Offset = parameters.Vector2("Offset"),
            Flags =
                Flag(
                    parameters.Boolean("AlignWithLayer"),
                    PrismStyleFlags.AlignWithLayer) |
                Flag(
                    parameters.Boolean("Reverse"),
                    PrismStyleFlags.Reverse) |
                Flag(
                    parameters.Boolean("Dither"),
                    PrismStyleFlags.Dither)
        };
    }

    private static PrismStylePlan CreatePatternOverlay(
        ParameterReader parameters)
    {
        PrismResourceId pattern = parameters.Resource("Pattern");
        return new PrismStylePlan(
            PrismStyleId.PatternOverlay,
            PatternOverlayKind)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            Opacity = parameters.Number("Opacity"),
            PaintKind = PrismStylePaintKind.Pattern,
            Resource = pattern,
            ResourceEnabled = true,
            ResourceRequired = true,
            Scale = parameters.Number("Scale"),
            Offset = parameters.Vector2("Offset"),
            Flags = Flag(
                parameters.Boolean("LinkWithLayer"),
                PrismStyleFlags.ResourceLinked)
        };
    }

    private static PrismStylePlan CreateStroke(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismStylePaintKind paintKind = PaintKindCode(
            parameters.Symbol("FillType"));
        PrismResourceId pattern = parameters.Resource("Pattern");
        PrismStylePlan plan = new(
            PrismStyleId.Stroke,
            StrokeKind)
        {
            Size = parameters.Number("Size"),
            Position = PositionCode(
                parameters.Symbol("Position")),
            BlendMode = parameters.BlendMode("BlendMode"),
            Opacity = parameters.Number("Opacity"),
            PaintKind = paintKind,
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            SecondaryColor = parameters.ColorConstant(
                new Color(255, 255, 255),
                scope.CompositionSettings.WorkingColorProfile),
            DetailContour = StableSymbolCode(
                parameters.Symbol("Gradient")),
            GradientMethod = GradientMethodCode(
                parameters.Symbol("GradientMethod")),
            GradientStyle = GradientStyleCode(
                parameters.Symbol("GradientStyle")),
            Angle = parameters.Number("GradientAngle"),
            Scale = paintKind == PrismStylePaintKind.Pattern
                ? parameters.Number("PatternScale")
                : parameters.Number("GradientScale"),
            Offset = paintKind == PrismStylePaintKind.Pattern
                ? parameters.Vector2("PatternOffset")
                : parameters.Vector2("GradientOffset"),
            Resource = pattern,
            ResourceEnabled =
                paintKind == PrismStylePaintKind.Pattern,
            ResourceRequired =
                paintKind == PrismStylePaintKind.Pattern
        };
        PrismStyleFlags flags = PrismStyleFlags.None;
        if (paintKind == PrismStylePaintKind.Gradient)
        {
            flags |= Flag(
                parameters.Boolean("GradientAlignWithLayer"),
                PrismStyleFlags.AlignWithLayer);
            flags |= Flag(
                parameters.Boolean("GradientReverse"),
                PrismStyleFlags.Reverse);
            flags |= Flag(
                parameters.Boolean("GradientDither"),
                PrismStyleFlags.Dither);
        }
        else
        {
            flags |= Flag(
                parameters.Boolean("PatternLinkWithLayer"),
                PrismStyleFlags.ResourceLinked);
        }
        return plan with { Flags = flags };
    }

    private static float ResolveAngle(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return parameters.Boolean("UseGlobalLight")
            ? scope.CompositionSettings.GlobalLightAngle
            : parameters.Number("Angle");
    }

    private static float ResolveAltitude(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return parameters.Boolean("UseGlobalLight")
            ? scope.CompositionSettings.GlobalLightAltitude
            : parameters.Number("Altitude");
    }

    private static PrismStyleFlags Flag(
        bool condition,
        PrismStyleFlags flag) =>
        condition ? flag : PrismStyleFlags.None;

    private static int ContourCode(int symbol)
    {
        if (symbol == LinearSymbol)
        {
            return 0;
        }
        if (symbol == GaussianSymbol)
        {
            return 1;
        }
        return 2 + (StableSymbolCode(symbol) % 2);
    }

    private static int TechniqueCode(int symbol)
    {
        if (symbol == SofterSymbol || symbol == SmoothSymbol)
        {
            return 0;
        }
        if (symbol == PreciseSymbol)
        {
            return 1;
        }
        return 2;
    }

    private static int PositionCode(int symbol)
    {
        if (symbol == OutsideSymbol)
        {
            return 0;
        }
        if (symbol == CenterSymbol)
        {
            return 1;
        }
        if (symbol == InsideSymbol)
        {
            return 2;
        }
        return 0;
    }

    private static int OriginCode(int symbol) =>
        symbol == EdgeSymbol ? 0 : 1;

    private static int DirectionCode(int symbol) =>
        symbol == UpSymbol ? 0 : 1;

    private static PrismStylePaintKind PaintKindCode(int symbol)
    {
        if (symbol == GradientSymbol)
        {
            return PrismStylePaintKind.Gradient;
        }
        if (symbol == PatternSymbol)
        {
            return PrismStylePaintKind.Pattern;
        }
        return symbol == ColorSymbol
            ? PrismStylePaintKind.Color
            : PrismStylePaintKind.Color;
    }

    private static int GradientMethodCode(int symbol) =>
        symbol == PerceptualSymbol ? 0 : 1;

    private static int GradientStyleCode(int symbol)
    {
        if (symbol == RadialSymbol)
        {
            return 1;
        }
        if (symbol == AngleSymbol)
        {
            return 2;
        }
        if (symbol == ReflectedSymbol)
        {
            return 3;
        }
        if (symbol == DiamondSymbol)
        {
            return 4;
        }
        return 0;
    }

    private static int BevelStyleCode(int symbol)
    {
        if (symbol == OuterBevelSymbol)
        {
            return 1;
        }
        if (symbol == EmbossSymbol)
        {
            return 2;
        }
        if (symbol == PillowEmbossSymbol)
        {
            return 3;
        }
        if (symbol == StrokeEmbossSymbol)
        {
            return 4;
        }
        return 0;
    }

    private static int StableSymbolCode(int symbol) =>
        (symbol & int.MaxValue) % 1024;

    private static float ResolveSpatialScale(
        PrismGraphScope scope)
    {
        float horizontal = MathF.Sqrt(
            (scope.EffectiveTransform.M11 *
                scope.EffectiveTransform.M11) +
            (scope.EffectiveTransform.M12 *
                scope.EffectiveTransform.M12));
        float vertical = MathF.Sqrt(
            (scope.EffectiveTransform.M21 *
                scope.EffectiveTransform.M21) +
            (scope.EffectiveTransform.M22 *
                scope.EffectiveTransform.M22));
        float scale =
            MathF.Max(horizontal, vertical) * scope.PixelScale;
        if (!float.IsFinite(scale))
        {
            throw new InvalidOperationException(
                "A Prism layer style produced a non-finite spatial scale.");
        }
        return scale;
    }

    private static DrawRect Inflate(
        DrawRect bounds,
        float amount)
    {
        return CreateBounds(
            bounds.X - amount,
            bounds.Y - amount,
            bounds.Right + amount,
            bounds.Bottom + amount);
    }

    private static DrawRect Translate(
        DrawRect bounds,
        float x,
        float y)
    {
        return CreateBounds(
            bounds.X + x,
            bounds.Y + y,
            bounds.Right + x,
            bounds.Bottom + y);
    }

    private static DrawRect Union(
        DrawRect left,
        DrawRect right)
    {
        return CreateBounds(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y),
            MathF.Max(left.Right, right.Right),
            MathF.Max(left.Bottom, right.Bottom));
    }

    private static DrawRect CreateBounds(
        float left,
        float top,
        float right,
        float bottom)
    {
        try
        {
            return new DrawRect(
                left,
                top,
                MathF.Max(0, right - left),
                MathF.Max(0, bottom - top));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException(
                "A Prism layer style produced unsupported bounds.",
                exception);
        }
    }

    private readonly ref struct ParameterReader
    {
        private readonly PrismGraphNode node;
        private readonly PrismCatalogPropertyDescriptor[] properties;

        public ParameterReader(
            PrismGraphNode node,
            PrismStyleId style)
        {
            this.node = node;
            properties =
                PrismCatalogRuntime.GetEntry((int)style).Properties;
        }

        public bool Boolean(string name)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Boolean);
            return value.BooleanValue;
        }

        public float Number(string name)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Number);
            return value.NumberValue;
        }

        public int Symbol(string name)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Symbol);
            return value.IntegerValue;
        }

        public PrismBlendMode BlendMode(string name)
        {
            int value = Symbol(name);
            if (Enum.IsDefined((PrismBlendMode)value))
            {
                return (PrismBlendMode)value;
            }

            foreach ((int symbol, PrismBlendMode mode) in
                HashedBlendModes)
            {
                if (symbol == value)
                {
                    return mode;
                }
            }

            throw new InvalidOperationException(
                $"Style property '{name}' has unknown blend mode '{value}'.");
        }

        public Vector4 Color(
            string name,
            PrismColorProfile profile =
                PrismColorProfile.LinearSrgb)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Color);
            return ConvertColor(value.ColorValue, profile);
        }

        public Vector4 ColorConstant(
            Color color,
            PrismColorProfile profile =
                PrismColorProfile.LinearSrgb) =>
            ConvertColor(color, profile);

        public Vector2 Vector2(string name)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Vector);
            return new Vector2(
                value.VectorValue.X,
                value.VectorValue.Y);
        }

        public PrismResourceId Resource(string name)
        {
            PrismGraphParameter value =
                Value(name, PrismGraphParameterValueKind.Resource);
            return value.ResourceValue;
        }

        private PrismGraphParameter Value(
            string name,
            PrismGraphParameterValueKind kind)
        {
            for (int index = 0;
                index < properties.Length;
                index++)
            {
                if (!string.Equals(
                    properties[index].Name,
                    name,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                PrismGraphParameter parameter =
                    node.Parameters[index];
                if (parameter.Index != index ||
                    parameter.Kind != kind)
                {
                    throw new InvalidOperationException(
                        $"Style property '{name}' does not match its generated slot.");
                }
                return parameter;
            }

            throw new InvalidOperationException(
                $"Style '{node.Style}' has no generated property '{name}'.");
        }

        private static Vector4 ConvertColor(
            Color color,
            PrismColorProfile profile)
        {
            double alpha = color.A / 255d;
            PrismPremultipliedColor converted =
                PrismColorPipeline.ConvertInputToWorking(
                    PrismPremultipliedColor.FromStraight(
                        color.R / 255d,
                        color.G / 255d,
                        color.B / 255d,
                        alpha),
                    profile);
            if (converted.Alpha == 0)
            {
                return Vector4.Zero;
            }

            return new Vector4(
                (float)(converted.Red / converted.Alpha),
                (float)(converted.Green / converted.Alpha),
                (float)(converted.Blue / converted.Alpha),
                (float)converted.Alpha);
        }
    }
}
