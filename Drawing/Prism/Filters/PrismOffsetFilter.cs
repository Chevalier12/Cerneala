using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismOffsetFilter
{
    public static Vector2 Map(
        Vector4 options,
        Vector2 uv,
        int width,
        int height) =>
        uv - new Vector2(
            options.X / width,
            options.Y / height);

    public static Vector4 Fill(
        Vector4 straight,
        PrismColorProfile workingProfile) =>
        PrismResamplingMath.AssociatedFill(
            straight,
            workingProfile);
}
