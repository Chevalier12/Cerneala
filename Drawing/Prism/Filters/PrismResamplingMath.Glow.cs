using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    internal static Vector4 BloomHorizontal(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        return GaussianAxis(
            plan,
            source,
            width,
            height,
            x,
            y,
            plan.Options0.W,
            horizontal: true,
            brightPass: true);
    }

    internal static Vector4 BloomVerticalComposite(
        PrismResamplingPlan plan,
        Vector4[] source,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 bloom = GaussianAxis(
            plan,
            source,
            width,
            height,
            x,
            y,
            plan.Options0.W,
            horizontal: false,
            brightPass: false);
        Vector4 basePixel = original[(y * width) + x];
        float strength =
            Math.Clamp(plan.Options0.Y, 0, 1) *
            Math.Clamp(plan.Options1.W, 0, 1);
        Vector3 contribution = new(
            bloom.X * plan.Options1.X * strength,
            bloom.Y * plan.Options1.Y * strength,
            bloom.Z * plan.Options1.Z * strength);
        Vector3 combined = Vector3.Min(
            new Vector3(basePixel.W),
            new Vector3(
                basePixel.X,
                basePixel.Y,
                basePixel.Z) + contribution);
        return new Vector4(combined, basePixel.W);
    }

    internal static Vector4 GaussianAxis(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius,
        bool horizontal,
        bool brightPass)
    {
        radius = MathF.Max(radius, 0.5f);
        Vector4 total = Vector4.Zero;
        total += BloomSample(0, 0.38774f);
        total += BloomSample(-radius, 0.24477f);
        total += BloomSample(radius, 0.24477f);
        total += BloomSample(-radius * 2, 0.06136f);
        total += BloomSample(radius * 2, 0.06136f);
        return total;

        Vector4 BloomSample(float offset, float weight)
        {
            Vector4 sample = SamplePixel(
                source,
                width,
                height,
                horizontal ? x + offset : x,
                horizontal ? y : y + offset,
                0);
            if (brightPass &&
                Vector3.Dot(
                    Unpremultiply(sample),
                    new Vector3(0.2126f, 0.7152f, 0.0722f)) <
                plan.Options0.Z)
            {
                sample = Vector4.Zero;
            }
            return sample * weight;
        }
    }

    internal static Vector4 NeonGlowEdge(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float topLeft = Signal(-1, -1);
        float top = Signal(0, -1);
        float topRight = Signal(1, -1);
        float left = Signal(-1, 0);
        float right = Signal(1, 0);
        float bottomLeft = Signal(-1, 1);
        float bottom = Signal(0, 1);
        float bottomRight = Signal(1, 1);
        float gradientX =
            -topLeft + topRight -
            (2 * left) + (2 * right) -
            bottomLeft + bottomRight;
        float gradientY =
            -topLeft - (2 * top) - topRight +
            bottomLeft + (2 * bottom) + bottomRight;
        float edge = Math.Clamp(
            MathF.Sqrt(
                (gradientX * gradientX) +
                (gradientY * gradientY)) / 4,
            0,
            1);
        return new Vector4(edge);

        float Signal(int offsetX, int offsetY)
        {
            Vector4 sample = SamplePixel(
                source,
                width,
                height,
                x + offsetX,
                y + offsetY,
                0);
            float luminance = Vector3.Dot(
                new Vector3(sample.X, sample.Y, sample.Z),
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            return MathF.Max(
                luminance,
                sample.W * 0.25f);
        }
    }

    internal static Vector4 NeonGlowPyramidComposite(
        PrismResamplingPlan plan,
        Vector4 original,
        MipLevel[] mipChain,
        Vector2 uv)
    {
        float maximumLod = Math.Clamp(
            plan.Options0.Z,
            0,
            mipChain.Length - 1);
        float mask =
            (Mask(0) * 0.32f) +
            (Mask(maximumLod * 0.25f) * 0.25f) +
            (Mask(maximumLod * 0.5f) * 0.19f) +
            (Mask(maximumLod * 0.75f) * 0.14f) +
            (Mask(maximumLod) * 0.10f);
        float strength =
            Math.Clamp(plan.Options0.W, 0, 8) *
            Math.Clamp(plan.Options1.W, 0, 1) *
            mask;
        Vector3 contribution = new(
            plan.Options1.X * strength,
            plan.Options1.Y * strength,
            plan.Options1.Z * strength);
        Vector3 combined = Vector3.Min(
            new Vector3(original.W),
            new Vector3(
                original.X,
                original.Y,
                original.Z) + contribution);
        return new Vector4(combined, original.W);

        float Mask(float lod) =>
            SampleMipmapped(
                mipChain,
                uv,
                lod,
                0,
                Vector4.Zero).X;
    }

    internal static Vector4 Grain(
        PrismResamplingPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        float noise = PrismCatalogFilterMath.Hash(x, y, 9173) - 0.5f;
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                new Vector3(
                    noise * Math.Clamp(
                        plan.Options0.X,
                        0,
                        1)),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * center.W,
            center.W);
    }

}
