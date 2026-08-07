using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Styles;

internal static class PrismPatternOverlayStyle
{
    public static PrismStylePlan Create(
        PrismStylePlanner.ParameterReader parameters)
    {
        PrismResourceId pattern = parameters.Resource("Pattern");
        return new PrismStylePlan(PrismStyleId.PatternOverlay, 8)
        {
            BlendMode = parameters.BlendMode("BlendMode"),
            Opacity = parameters.Number("Opacity"),
            PaintKind = PrismStylePaintKind.Pattern,
            Resource = pattern,
            ResourceEnabled = true,
            ResourceRequired = true,
            Scale = parameters.Number("Scale"),
            Offset = parameters.Vector2("Offset"),
            Flags = PrismStylePlanner.Flag(
                parameters.Boolean("LinkWithLayer"),
                PrismStyleFlags.ResourceLinked)
        };
    }
}
