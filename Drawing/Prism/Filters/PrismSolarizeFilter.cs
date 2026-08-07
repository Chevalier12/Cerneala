using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismSolarizeFilter
{
    public static Vector3 Apply(Vector3 color, float threshold) =>
        new(
            SolarizeChannel(color.X, threshold),
            SolarizeChannel(color.Y, threshold),
            SolarizeChannel(color.Z, threshold));

    private static float SolarizeChannel(
        float channel,
        float threshold) =>
        channel < threshold
            ? channel
            : 1 - channel;
}
