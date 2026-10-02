using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    private static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill)
    {
        bool outside = uv.X < 0 ||
            uv.X > 1 ||
            uv.Y < 0 ||
            uv.Y > 1;
        if (outside && edgeMode == 1)
        {
            return Vector4.Zero;
        }
        if (outside && edgeMode == 4)
        {
            return fill;
        }

        if (edgeMode == 2)
        {
            uv = Fract(uv);
        }
        else if (edgeMode == 3)
        {
            uv = new Vector2(
                Mirror(uv.X),
                Mirror(uv.Y));
        }

        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int x0 = (int)MathF.Floor(sampleX);
        int y0 = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - x0;
        float fractionY = sampleY - y0;
        Vector4 top = Vector4.Lerp(
            SamplePixel(
                source,
                width,
                height,
                x0,
                y0,
                edgeMode),
            SamplePixel(
                source,
                width,
                height,
                x0 + 1,
                y0,
                edgeMode),
            fractionX);
        Vector4 bottom = Vector4.Lerp(
            SamplePixel(
                source,
                width,
                height,
                x0,
                y0 + 1,
                edgeMode),
            SamplePixel(
                source,
                width,
                height,
                x0 + 1,
                y0 + 1,
                edgeMode),
            fractionX);
        return Vector4.Lerp(
            top,
            bottom,
            fractionY);
    }

    private static Vector4 SampleBicubic(
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill)
    {
        bool outside = uv.X < 0 ||
            uv.X > 1 ||
            uv.Y < 0 ||
            uv.Y > 1;
        if (outside && edgeMode == 1)
        {
            return Vector4.Zero;
        }
        if (outside && edgeMode == 4)
        {
            return fill;
        }

        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int baseX = (int)MathF.Floor(sampleX);
        int baseY = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - baseX;
        float fractionY = sampleY - baseY;
        int tapEdgeMode = edgeMode is 2 or 3
            ? edgeMode
            : 0;
        Vector4 total = Vector4.Zero;
        for (int offsetY = -1; offsetY <= 2; offsetY++)
        {
            float weightY = CubicWeight(
                offsetY - fractionY);
            Vector4 row = Vector4.Zero;
            for (int offsetX = -1; offsetX <= 2; offsetX++)
            {
                float weightX = CubicWeight(
                    offsetX - fractionX);
                row += SamplePixel(
                        source,
                        width,
                        height,
                        baseX + offsetX,
                        baseY + offsetY,
                        tapEdgeMode) *
                    weightX;
            }
            total += row * weightY;
        }
        return total;
    }

    private static float CubicWeight(float distance)
    {
        const float coefficient = -0.75f;
        float absolute = MathF.Abs(distance);
        if (absolute <= 1)
        {
            return
                ((coefficient + 2) *
                    absolute *
                    absolute *
                    absolute) -
                ((coefficient + 3) *
                    absolute *
                    absolute) +
                1;
        }
        if (absolute < 2)
        {
            return
                (coefficient *
                    absolute *
                    absolute *
                    absolute) -
                (5 * coefficient *
                    absolute *
                    absolute) +
                (8 * coefficient * absolute) -
                (4 * coefficient);
        }
        return 0;
    }

    internal static Vector4 SampleTransform(
        PrismResamplingPlan plan,
        MipLevel[] mipChain,
        Vector2 uv,
        Vector2 mapped,
        int width,
        int height,
        int edgeMode,
        Vector4 fill)
    {
        Vector2 sourceSize = new(width, height);
        Vector2 derivativeX =
            (MapTransform(
                plan,
                uv + new Vector2(1f / width, 0)) -
            mapped) * sourceSize;
        Vector2 derivativeY =
            (MapTransform(
                plan,
                uv + new Vector2(0, 1f / height)) -
            mapped) * sourceSize;
        float lengthX = derivativeX.Length();
        float lengthY = derivativeY.Length();
        Vector2 majorDerivative = lengthX >= lengthY
            ? derivativeX
            : derivativeY;
        float major = MathF.Max(lengthX, lengthY);
        float minor = MathF.Max(
            1,
            MathF.Min(lengthX, lengthY));
        float lod = MathF.Max(0, MathF.Log2(minor));
        int tapCount = Math.Clamp(
            (int)MathF.Ceiling(major / minor),
            1,
            4);
        if (tapCount == 1 || major <= 0)
        {
            return SampleMipmapped(
                mipChain,
                mapped,
                lod,
                edgeMode,
                fill);
        }

        Vector4 total = Vector4.Zero;
        Vector2 span =
            majorDerivative /
            sourceSize;
        for (int tap = 0; tap < tapCount; tap++)
        {
            float position =
                ((tap + 0.5f) / tapCount) -
                0.5f;
            total += SampleMipmapped(
                mipChain,
                mapped + (span * position),
                lod,
                edgeMode,
                fill);
        }

        return total / tapCount;
    }

    private static Vector4 SampleMipmapped(
        MipLevel[] mipChain,
        Vector2 uv,
        float lod,
        int edgeMode,
        Vector4 fill)
    {
        int lowerLevel = Math.Clamp(
            (int)MathF.Floor(lod),
            0,
            mipChain.Length - 1);
        int upperLevel = Math.Min(
            lowerLevel + 1,
            mipChain.Length - 1);
        MipLevel lower = mipChain[lowerLevel];
        Vector4 lowerSample = Sample(
            lower.Pixels,
            lower.Width,
            lower.Height,
            uv,
            edgeMode,
            fill);
        if (lowerLevel == upperLevel)
        {
            return lowerSample;
        }

        MipLevel upper = mipChain[upperLevel];
        Vector4 upperSample = Sample(
            upper.Pixels,
            upper.Width,
            upper.Height,
            uv,
            edgeMode,
            fill);
        return Vector4.Lerp(
            lowerSample,
            upperSample,
            lod - lowerLevel);
    }

    private static MipLevel[] BuildMipChain(
        Vector4[] source,
        int width,
        int height)
    {
        List<MipLevel> levels =
        [
            new MipLevel(source, width, height)
        ];
        Vector4[] current = source;
        int currentWidth = width;
        int currentHeight = height;
        while (currentWidth > 1 || currentHeight > 1)
        {
            int nextWidth = Math.Max(1, currentWidth / 2);
            int nextHeight = Math.Max(1, currentHeight / 2);
            Vector4[] next = new Vector4[nextWidth * nextHeight];
            for (int y = 0; y < nextHeight; y++)
            {
                int sourceY = y * 2;
                int sourceY1 = Math.Min(
                    sourceY + 1,
                    currentHeight - 1);
                for (int x = 0; x < nextWidth; x++)
                {
                    int sourceX = x * 2;
                    int sourceX1 = Math.Min(
                        sourceX + 1,
                        currentWidth - 1);
                    next[(y * nextWidth) + x] =
                        (current[(sourceY * currentWidth) + sourceX] +
                        current[(sourceY * currentWidth) + sourceX1] +
                        current[(sourceY1 * currentWidth) + sourceX] +
                        current[(sourceY1 * currentWidth) + sourceX1]) /
                        4;
                }
            }

            levels.Add(new MipLevel(next, nextWidth, nextHeight));
            current = next;
            currentWidth = nextWidth;
            currentHeight = nextHeight;
        }

        return [.. levels];
    }

    private static Vector4 SamplePixel(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        int edgeMode)
    {
        int sampleX = (int)MathF.Round(x);
        int sampleY = (int)MathF.Round(y);
        if (edgeMode == 1 &&
            (sampleX < 0 ||
                sampleX >= width ||
                sampleY < 0 ||
                sampleY >= height))
        {
            return Vector4.Zero;
        }

        sampleX = edgeMode switch
        {
            2 => Wrap(sampleX, width),
            3 => Mirror(sampleX, width),
            _ => Math.Clamp(sampleX, 0, width - 1)
        };
        sampleY = edgeMode switch
        {
            2 => Wrap(sampleY, height),
            3 => Mirror(sampleY, height),
            _ => Math.Clamp(sampleY, 0, height - 1)
        };
        return source[(sampleY * width) + sampleX];
    }

    internal readonly record struct MipLevel(
        Vector4[] Pixels,
        int Width,
        int Height);

}
