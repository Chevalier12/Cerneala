using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismBevelEmbossStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope)
    {
        bool textureEnabled = parameters.Boolean("TextureEnabled");
        PrismResourceId pattern = parameters.Resource("Pattern");
        PrismStyleFlags flags =
            PrismStylePlanner.Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias) |
            PrismStylePlanner.Flag(
                parameters.Boolean("ContourEnabled"),
                PrismStyleFlags.ContourEnabled) |
            PrismStylePlanner.Flag(
                parameters.Boolean("ContourAntiAlias"),
                PrismStyleFlags.ContourAntiAlias) |
            PrismStylePlanner.Flag(
                textureEnabled,
                PrismStyleFlags.TextureEnabled) |
            PrismStylePlanner.Flag(
                parameters.Boolean("TextureInvert"),
                PrismStyleFlags.TextureInvert) |
            PrismStylePlanner.Flag(
                parameters.Boolean("TextureLinkWithLayer"),
                PrismStyleFlags.ResourceLinked);
        return new PrismStylePlan(PrismStyleId.BevelEmboss, 4)
        {
            BevelStyle = PrismStylePlanner.BevelStyleCode(
                parameters.Symbol("Style")),
            Technique = PrismStylePlanner.TechniqueCode(
                parameters.Symbol("Technique")),
            Depth = parameters.Number("Depth"),
            Direction = PrismStylePlanner.DirectionCode(
                parameters.Symbol("Direction")),
            Size = parameters.Number("Size"),
            Soften = parameters.Number("Soften"),
            Angle = PrismStylePlanner.ResolveAngle(parameters, scope),
            Altitude = PrismStylePlanner.ResolveAltitude(parameters, scope),
            Contour = PrismStylePlanner.ContourCode(
                parameters.Symbol("GlossContour")),
            BlendMode = parameters.BlendMode("HighlightMode"),
            PrimaryColor = parameters.Color(
                "HighlightColor",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("HighlightOpacity"),
            SecondaryBlendMode = parameters.BlendMode("ShadowMode"),
            SecondaryColor = parameters.Color(
                "ShadowColor",
                scope.CompositionSettings.WorkingColorProfile),
            SecondaryOpacity = parameters.Number("ShadowOpacity"),
            DetailContour = PrismStylePlanner.ContourCode(
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
}
