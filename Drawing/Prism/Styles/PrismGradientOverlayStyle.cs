using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using System.Numerics;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismGradientOverlayStyle
{
    internal static PrismGradientMapResource DefaultGradient { get; } =
        new(
        [
            new PrismGradientMapPoint(0, Vector3.Zero),
            new PrismGradientMapPoint(1, Vector3.One)
        ]);

    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismResourceId gradient = parameters.Resource("Gradient");
        return new PrismStylePlan(PrismStyleId.GradientOverlay, 7)
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
            GradientMethod = PrismStylePlanner.GradientMethodCode(
                parameters.Symbol("Method")),
            GradientStyle = PrismStylePlanner.GradientStyleCode(
                parameters.Symbol("Style")),
            Angle = parameters.Number("Angle"),
            Scale = parameters.Number("Scale"),
            Offset = parameters.Vector2("Offset"),
            Resource = gradient,
            ResourceEnabled = gradient.Value > 0,
            ResourceRequired = gradient.Value > 0,
            Flags =
                PrismStylePlanner.Flag(
                    parameters.Boolean("AlignWithLayer"),
                    PrismStyleFlags.AlignWithLayer) |
                PrismStylePlanner.Flag(
                    parameters.Boolean("Reverse"),
                    PrismStyleFlags.Reverse) |
                PrismStylePlanner.Flag(
                    parameters.Boolean("Dither"),
                    PrismStyleFlags.Dither)
        };
    }
}
