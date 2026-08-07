using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismInnerGlowStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismResourceId gradient = parameters.Resource("Gradient");
        return new PrismStylePlan(PrismStyleId.InnerGlow, 3)
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
            Technique = PrismStylePlanner.TechniqueCode(
                parameters.Symbol("Technique")),
            Origin = PrismStylePlanner.OriginCode(
                parameters.Symbol("Origin")),
            Spread = parameters.Number("Choke"),
            Size = parameters.Number("Size"),
            Contour = PrismStylePlanner.ContourCode(
                parameters.Symbol("Contour")),
            Range = parameters.Number("Range"),
            Jitter = parameters.Number("Jitter"),
            Flags = PrismStylePlanner.Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias),
            Resource = gradient,
            ResourceEnabled = gradient.Value > 0,
            ResourceRequired = gradient.Value > 0
        };
    }
}
