using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismColorOverlayStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope) =>
        new(PrismStyleId.ColorOverlay, 6)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            PrimaryColor = parameters.Color(
                "Color",
                scope.CompositionSettings.WorkingColorProfile),
            Opacity = parameters.Number("Opacity"),
            PaintKind = PrismStylePaintKind.Color
        };
}
