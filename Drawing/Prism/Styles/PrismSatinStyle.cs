using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismSatinStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope) =>
        new(PrismStyleId.Satin, 5)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            Angle = parameters.Number("Angle"),
            Distance = parameters.Number("Distance"),
            Size = parameters.Number("Size"),
            Contour = PrismStylePlanner.ContourCode(
                parameters.Symbol("Contour")),
            Flags =
                PrismStylePlanner.Flag(
                    parameters.Boolean("AntiAlias"),
                    PrismStyleFlags.AntiAlias) |
                PrismStylePlanner.Flag(
                    parameters.Boolean("Invert"),
                    PrismStyleFlags.Invert)
        };
}
