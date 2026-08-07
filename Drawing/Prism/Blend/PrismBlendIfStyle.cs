using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismBlendIfStyle
{
    internal static double Evaluate(double value, PrismBlendRange range)
    {
        double black = range.BlackEnd > range.BlackStart
            ? Math.Clamp(
                (value - range.BlackStart) /
                    (range.BlackEnd - range.BlackStart),
                0,
                1)
            : value >= range.BlackStart ? 1 : 0;
        double white = range.WhiteEnd > range.WhiteStart
            ? 1 - Math.Clamp(
                (value - range.WhiteStart) /
                    (range.WhiteEnd - range.WhiteStart),
                0,
                1)
            : value <= range.WhiteStart ? 1 : 0;
        return black * white;
    }

    internal static double SelectChannel(
        PrismBlendColor color,
        PrismBlendIfChannel channel) => channel switch
        {
            PrismBlendIfChannel.Gray => PrismBlendMath.Luminosity(color),
            PrismBlendIfChannel.Red => color.Red,
            PrismBlendIfChannel.Green => color.Green,
            PrismBlendIfChannel.Blue => color.Blue,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "Unknown Blend If channel.")
        };
}
