using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismInnerShadowStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope) =>
        new(PrismStyleId.InnerShadow, 1)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            Angle = PrismStylePlanner.ResolveAngle(parameters, scope),
            Distance = parameters.Number("Distance"),
            Spread = parameters.Number("Choke"),
            Size = parameters.Number("Size"),
            Contour = PrismStylePlanner.ContourCode(
                parameters.Symbol("Contour")),
            Noise = parameters.Number("Noise"),
            Flags = PrismStylePlanner.Flag(
                parameters.Boolean("AntiAlias"),
                PrismStyleFlags.AntiAlias)
        };
}
