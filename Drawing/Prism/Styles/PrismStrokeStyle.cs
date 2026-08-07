using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismStrokeStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters,
        PrismGraphScope scope)
    {
        PrismStylePaintKind paintKind =
            PrismStylePlanner.PaintKindCode(
                parameters.Symbol("FillType"));
        PrismResourceId pattern = parameters.Resource("Pattern");
        PrismStylePlan plan = new(PrismStyleId.Stroke, 9)
        {
            Size = parameters.Number("Size"),
            Position = PrismStylePlanner.PositionCode(
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
            DetailContour = PrismStylePlanner.StableSymbolCode(
                parameters.Symbol("Gradient")),
            GradientMethod = PrismStylePlanner.GradientMethodCode(
                parameters.Symbol("GradientMethod")),
            GradientStyle = PrismStylePlanner.GradientStyleCode(
                parameters.Symbol("GradientStyle")),
            Angle = parameters.Number("GradientAngle"),
            Scale = paintKind == PrismStylePaintKind.Pattern
                ? parameters.Number("PatternScale")
                : parameters.Number("GradientScale"),
            Offset = paintKind == PrismStylePaintKind.Pattern
                ? parameters.Vector2("PatternOffset")
                : parameters.Vector2("GradientOffset"),
            Resource = pattern,
            ResourceEnabled = paintKind == PrismStylePaintKind.Pattern,
            ResourceRequired = paintKind == PrismStylePaintKind.Pattern
        };
        PrismStyleFlags flags = PrismStyleFlags.None;
        if (paintKind == PrismStylePaintKind.Gradient)
        {
            flags |= PrismStylePlanner.Flag(
                parameters.Boolean("GradientAlignWithLayer"),
                PrismStyleFlags.AlignWithLayer);
            flags |= PrismStylePlanner.Flag(
                parameters.Boolean("GradientReverse"),
                PrismStyleFlags.Reverse);
            flags |= PrismStylePlanner.Flag(
                parameters.Boolean("GradientDither"),
                PrismStyleFlags.Dither);
        }
        else
        {
            flags |= PrismStylePlanner.Flag(
                parameters.Boolean("PatternLinkWithLayer"),
                PrismStyleFlags.ResourceLinked);
        }
        return plan with { Flags = flags };
    }
}
