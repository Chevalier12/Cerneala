using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    internal static Vector4 SampleLiquify(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector4 bilinear = Sample(
            source,
            width,
            height,
            mapped,
            edgeMode,
            fill);
        float cubicConfidence = LiquifyCubicConfidence(
            plan,
            uv,
            width,
            height,
            primaryResource,
            auxiliaryResource);
        if (cubicConfidence <= 0.001f)
        {
            return bilinear;
        }

        Vector4 bicubic = SampleBicubic(
            source,
            width,
            height,
            mapped,
            edgeMode,
            fill);
        return cubicConfidence >= 0.999f
            ? bicubic
            : Vector4.Lerp(
                bilinear,
                bicubic,
                cubicConfidence);
    }

    private static float LiquifyCubicConfidence(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector2 pixelSize = new(
            1f / width,
            1f / height);
        Vector2 leftUv = Vector2.Clamp(
            uv - new Vector2(pixelSize.X, 0),
            Vector2.Zero,
            Vector2.One);
        Vector2 rightUv = Vector2.Clamp(
            uv + new Vector2(pixelSize.X, 0),
            Vector2.Zero,
            Vector2.One);
        Vector2 topUv = Vector2.Clamp(
            uv - new Vector2(0, pixelSize.Y),
            Vector2.Zero,
            Vector2.One);
        Vector2 bottomUv = Vector2.Clamp(
            uv + new Vector2(0, pixelSize.Y),
            Vector2.Zero,
            Vector2.One);
        Vector2 sourceSize = new(width, height);
        Vector2 derivativeX =
            (MapLiquifyResource(
                plan,
                rightUv,
                primaryResource,
                auxiliaryResource) -
            MapLiquifyResource(
                plan,
                leftUv,
                primaryResource,
                auxiliaryResource)) *
            sourceSize /
            MathF.Max(
                (rightUv.X - leftUv.X) * width,
                0.000001f);
        Vector2 derivativeY =
            (MapLiquifyResource(
                plan,
                bottomUv,
                primaryResource,
                auxiliaryResource) -
            MapLiquifyResource(
                plan,
                topUv,
                primaryResource,
                auxiliaryResource)) *
            sourceSize /
            MathF.Max(
                (bottomUv.Y - topUv.Y) * height,
                0.000001f);
        if (!float.IsFinite(derivativeX.X) ||
            !float.IsFinite(derivativeX.Y) ||
            !float.IsFinite(derivativeY.X) ||
            !float.IsFinite(derivativeY.Y))
        {
            return 0;
        }

        float determinant =
            (derivativeX.X * derivativeY.Y) -
            (derivativeX.Y * derivativeY.X);
        float maximumAxis = MathF.Max(
            derivativeX.Length(),
            derivativeY.Length());
        float orientationConfidence = SmoothStep(
            0.05f,
            0.25f,
            determinant);
        float footprintConfidence =
            1 -
            SmoothStep(
                2,
                4,
                maximumAxis);
        return Math.Clamp(
            orientationConfidence * footprintConfidence,
            0,
            1);
    }

    private static Vector2 MapLiquifyResource(
        PrismResamplingPlan plan,
        Vector2 uv,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource) =>
        MapLiquify(
            plan,
            uv,
            primaryResource?.Invoke(uv) ??
                new Vector4(0.5f, 0.5f, 0, 1),
            auxiliaryResource?.Invoke(uv));

}
