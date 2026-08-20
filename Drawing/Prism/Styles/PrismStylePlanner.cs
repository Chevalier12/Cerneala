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
                PrismDropShadowStyle.Create(parameters, scope),
            PrismStyleId.InnerShadow =>
                PrismInnerShadowStyle.Create(parameters, scope),
            PrismStyleId.OuterGlow =>
                PrismOuterGlowStyle.Create(parameters, scope),
            PrismStyleId.InnerGlow =>
                PrismInnerGlowStyle.Create(parameters, scope),
            PrismStyleId.BevelEmboss =>
                PrismBevelEmbossStyle.Create(parameters, scope),
            PrismStyleId.Satin =>
                PrismSatinStyle.Create(parameters, scope),
            PrismStyleId.ColorOverlay =>
                PrismColorOverlayStyle.Create(parameters, scope),
            PrismStyleId.GradientOverlay =>
                PrismGradientOverlayStyle.Create(parameters, scope),
            PrismStyleId.PatternOverlay =>
                PrismPatternOverlayStyle.Create(parameters),
            PrismStyleId.Stroke =>
                PrismStrokeStyle.Create(parameters, scope),
            _ => throw new ArgumentOutOfRangeException(
                nameof(node),
                style,
                "Unknown Prism layer style.")
        };
    }

    internal static PrismBlendMode ResolveBlendMode(
        int value,
        string propertyName)
    {
        if (Enum.IsDefined((PrismBlendMode)value))
        {
            return (PrismBlendMode)value;
        }

        foreach ((int symbol, PrismBlendMode mode) in HashedBlendModes)
        {
            if (symbol == value)
            {
                return mode;
            }
        }

        throw new InvalidOperationException(
            $"Style property '{propertyName}' has unknown blend mode '{value}'.");
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
                        checked(
                            MathF.Max(
                                MathF.Ceiling(
                                    geometry.Size * 1.5f),
                                1f) +
                            (geometry.Spread >= 0.5f
                                ? MathF.Ceiling(
                                    geometry.Spread)
                                : 0f))),
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

    internal static float ResolveAngle(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return parameters.Boolean("UseGlobalLight")
            ? scope.CompositionSettings.GlobalLightAngle
            : parameters.Number("Angle");
    }

    internal static float ResolveAltitude(
        ParameterReader parameters,
        PrismGraphScope scope)
    {
        return parameters.Boolean("UseGlobalLight")
            ? scope.CompositionSettings.GlobalLightAltitude
            : parameters.Number("Altitude");
    }

    internal static PrismStyleFlags Flag(
        bool condition,
        PrismStyleFlags flag) =>
        condition ? flag : PrismStyleFlags.None;

    internal static int ContourCode(int symbol)
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

    internal static int TechniqueCode(int symbol)
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

    internal static int PositionCode(int symbol)
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

    internal static int OriginCode(int symbol) =>
        symbol == EdgeSymbol ? 0 : 1;

    internal static int DirectionCode(int symbol) =>
        symbol == UpSymbol ? 0 : 1;

    internal static PrismStylePaintKind PaintKindCode(int symbol)
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

    internal static int GradientMethodCode(int symbol) =>
        symbol == PerceptualSymbol ? 0 : 1;

    internal static int GradientStyleCode(int symbol)
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

    internal static int BevelStyleCode(int symbol)
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

    internal static int StableSymbolCode(int symbol) =>
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

    internal readonly ref struct ParameterReader
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
            return ResolveBlendMode(Symbol(name), name);
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
