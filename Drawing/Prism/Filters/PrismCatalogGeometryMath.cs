using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogColorMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogTextureMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogGeometryMath
{
    internal static Vector4 EdgeDetection(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Emboss)
        {
            return PrismEmbossFilter.ApplyPixel(
                plan, pass, source, width, height, x, y, center);
        }

        if (plan.Filter == PrismFilterId.FindEdges)
        {
            return PrismFindEdgesFilter.ApplyPixel(
                plan, source, width, height, x, y, center);
        }

        float edge = Sobel(source, width, height, x, y);

        Vector4 foreground = OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = OptionVector(
            plan,
            "Background",
            new Vector4(1, 1, 1, 1));
        float mix = Math.Clamp(
            Luminance(center) + edge * 0.5f,
            0,
            1);
        Vector3 sketch = Vector3.Lerp(
            new Vector3(
                foreground.X,
                foreground.Y,
                foreground.Z),
            new Vector3(
                background.X,
                background.Y,
                background.Z),
            mix);
        return Associated(
            Vector3.Lerp(
                straight,
                sketch,
                Math.Clamp(
                    0.35f + (ParameterMagnitude(plan) * 0.01f),
                    0.35f,
                    1)),
            center.W);
    }







    internal static Vector4 Extrude(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float size = MathF.Max(
            1,
            Option(plan, "Size", 30));
        float depth = Math.Clamp(
            Option(plan, "Depth", 30),
            0,
            size);
        int cellX = (int)MathF.Floor(x / size);
        int cellY = (int)MathF.Floor(y / size);
        int type = Symbol(plan, "Type");
        bool maskIncompleteBlocks =
            Option(plan, "MaskIncompleteBlocks", 0) >= 0.5f;
        bool solidFrontFaces =
            Option(plan, "SolidFrontFaces", 1) >= 0.5f;
        bool complete = IsCompleteExtrudeCell(
            cellX,
            cellY,
            size,
            width,
            height);
        Vector4 front = maskIncompleteBlocks && !complete
            ? Vector4.Zero
            : type == 0 && solidFrontFaces
                ? ExtrudeCellSample(
                    source,
                    width,
                    height,
                    cellX,
                    cellY,
                    size)
                : source[(y * width) + x];

        return type == 1
            ? ExtrudePyramid(
                plan,
                source,
                width,
                height,
                x + 0.5f,
                y + 0.5f,
                cellX,
                cellY,
                size,
                depth,
                maskIncompleteBlocks,
                solidFrontFaces,
                front)
            : ExtrudeBlock(
                plan,
                source,
                width,
                height,
                x + 0.5f,
                y + 0.5f,
                cellX,
                cellY,
                size,
                depth,
                maskIncompleteBlocks,
                front);
    }

    private static Vector4 ExtrudeBlock(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        bool maskIncompleteBlocks,
        Vector4 front)
    {
        float bestScore = -1;
        Vector4 result = front;
        int firstCellX = Math.Max(0, cellX - 2);
        int firstCellY = Math.Max(0, cellY - 2);
        for (int candidateY = firstCellY;
            candidateY <= cellY;
            candidateY++)
        {
            for (int candidateX = firstCellX;
                candidateX <= cellX;
                candidateX++)
            {
                if (!IsCompleteExtrudeCell(
                        candidateX,
                        candidateY,
                        size,
                        width,
                        height) &&
                    maskIncompleteBlocks)
                {
                    continue;
                }

                float candidateDepth = ExtrudeCellDepth(
                    plan,
                    candidateX,
                    candidateY,
                    size,
                    depth);
                if (!TryExtrudeBlockSide(
                        pixelX,
                        pixelY,
                        candidateX,
                        candidateY,
                        size,
                        candidateDepth,
                        out float sideScore,
                        out float shade))
                {
                    continue;
                }

                float score = candidateDepth + sideScore * 0.001f;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                result = ShadeExtrudeFace(
                    ExtrudeCellSample(
                        source,
                        width,
                        height,
                        candidateX,
                        candidateY,
                        size),
                    shade);
            }
        }

        return result;
    }

    private static Vector4 ExtrudePyramid(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        bool maskIncompleteBlocks,
        bool solidFrontFaces,
        Vector4 front)
    {
        float bestScore = -1;
        Vector4 result = front;
        int firstCellX = Math.Max(0, cellX - 2);
        int firstCellY = Math.Max(0, cellY - 2);
        for (int candidateY = firstCellY;
            candidateY <= cellY;
            candidateY++)
        {
            for (int candidateX = firstCellX;
                candidateX <= cellX;
                candidateX++)
            {
                if (!IsCompleteExtrudeCell(
                        candidateX,
                        candidateY,
                        size,
                        width,
                        height) &&
                    maskIncompleteBlocks)
                {
                    continue;
                }

                float candidateDepth = ExtrudeCellDepth(
                    plan,
                    candidateX,
                    candidateY,
                    size,
                    depth);
                float left = candidateX * size;
                float top = candidateY * size;
                float right = MathF.Min(
                    (candidateX + 1) * size,
                    width);
                float bottom = MathF.Min(
                    (candidateY + 1) * size,
                    height);
                Vector2 apex = new(
                    (left + right) * 0.5f + candidateDepth * 0.75f,
                    (top + bottom) * 0.5f + candidateDepth * 0.75f);
                int face = ExtrudePyramidFace(
                    new Vector2(pixelX, pixelY),
                    new(left, top),
                    new(right, top),
                    new(right, bottom),
                    new(left, bottom),
                    apex);
                if (face < 0)
                {
                    continue;
                }

                bool currentCell =
                    candidateX == cellX && candidateY == cellY;
                float score = currentCell
                    ? 0
                    : 1 + candidateDepth;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                Vector4 candidate = ExtrudeCellSample(
                    source,
                    width,
                    height,
                    candidateX,
                    candidateY,
                    size);
                if (currentCell && !solidFrontFaces)
                {
                    candidate = source[
                        ((int)pixelY * width) + (int)pixelX];
                }
                result = ShadeExtrudeFace(
                    candidate,
                    face switch
                    {
                        0 => 0.84f,
                        1 => 0.68f,
                        2 => 0.54f,
                        _ => 0.74f
                    });
            }
        }

        return result;
    }

    private static float ExtrudeCellDepth(
        PrismCatalogFilterPlan plan,
        int cellX,
        int cellY,
        float size,
        float depth)
    {
        if (depth <= 0)
        {
            return 0;
        }

        int depthMode = Symbol(plan, "DepthMode");
        float level = depthMode == 1
            ? 1
            : 0.45f +
                ExtrudeHash(
                    cellX,
                    cellY,
                    Seed(plan, "Seed")) *
                0.55f;
        return depth * level;
    }

    private static float ExtrudeHash(
        int cellX,
        int cellY,
        uint seed)
    {
        float value =
            (cellX * 127.1f) +
            (cellY * 311.7f);
        float hashed = MathF.Sin(
                value +
                (seed * 0.00006103515625f)) *
            43758.5453123f;
        return hashed - MathF.Floor(hashed);
    }

    private static bool IsCompleteExtrudeCell(
        int cellX,
        int cellY,
        float size,
        int width,
        int height)
    {
        if (cellX < 0 || cellY < 0)
        {
            return false;
        }

        float left = cellX * size;
        float top = cellY * size;
        return left + size <= width + 0.0001f &&
            top + size <= height + 0.0001f;
    }

    private static Vector4 ExtrudeCellSample(
        Vector4[] source,
        int width,
        int height,
        int cellX,
        int cellY,
        float size)
    {
        float left = cellX * size;
        float top = cellY * size;
        return SamplePixel(
            source,
            width,
            height,
            left + (size * 0.5f),
            top + (size * 0.5f));
    }

    private static bool TryExtrudeBlockSide(
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        out float score,
        out float shade)
    {
        score = 0;
        shade = 0;
        if (depth <= 0)
        {
            return false;
        }

        float left = cellX * size;
        float top = cellY * size;
        float right = left + size;
        float bottom = top + size;
        float offset = depth * 0.75f;
        bool hit = false;
        float bestShade = float.PositiveInfinity;
        float bestScore = 0;

        float rightT = (pixelX - right) / offset;
        if (rightT is >= 0 and <= 1)
        {
            float minimumY = top + (rightT * offset);
            float maximumY = bottom + (rightT * offset);
            if (pixelY >= minimumY && pixelY <= maximumY)
            {
                hit = true;
                bestScore = rightT;
                bestShade = 0.76f - (rightT * 0.16f);
            }
        }

        float bottomT = (pixelY - bottom) / offset;
        if (bottomT is >= 0 and <= 1)
        {
            float minimumX = left + (bottomT * offset);
            float maximumX = right + (bottomT * offset);
            if (pixelX >= minimumX && pixelX <= maximumX)
            {
                float bottomShade = 0.58f - (bottomT * 0.14f);
                if (!hit || bottomShade < bestShade)
                {
                    hit = true;
                    bestScore = bottomT;
                    bestShade = bottomShade;
                }
            }
        }

        score = bestScore;
        shade = bestShade;
        return hit;
    }

    private static int ExtrudePyramidFace(
        Vector2 point,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft,
        Vector2 apex)
    {
        if (PointInTriangle(point, topLeft, topRight, apex))
        {
            return 0;
        }
        if (PointInTriangle(point, topRight, bottomRight, apex))
        {
            return 1;
        }
        if (PointInTriangle(point, bottomRight, bottomLeft, apex))
        {
            return 2;
        }
        return PointInTriangle(point, bottomLeft, topLeft, apex)
            ? 3
            : -1;
    }

    private static bool PointInTriangle(
        Vector2 point,
        Vector2 first,
        Vector2 second,
        Vector2 third)
    {
        float firstCross = Cross(second - first, point - first);
        float secondCross = Cross(third - second, point - second);
        float thirdCross = Cross(first - third, point - third);
        bool hasNegative =
            firstCross < -0.0001f ||
            secondCross < -0.0001f ||
            thirdCross < -0.0001f;
        bool hasPositive =
            firstCross > 0.0001f ||
            secondCross > 0.0001f ||
            thirdCross > 0.0001f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 left, Vector2 right) =>
        (left.X * right.Y) - (left.Y * right.X);

    private static Vector4 ShadeExtrudeFace(
        Vector4 color,
        float shade)
    {
        return Associated(
            Vector3.Clamp(
                Unpremultiply(color) * shade,
                Vector3.Zero,
                Vector3.One),
            color.W);
    }

    private static int Symbol(
        PrismCatalogFilterPlan plan,
        string name) =>
        IntegerBits(plan.GetOption(name));

    internal static Vector4 Tiling(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (plan.Filter == PrismFilterId.ChromaticAberration)
        {
            return PrismChromaticAberrationFilter.ApplyPixel(
                plan, source, width, height, x, y);
        }
        return PrismTilesFilter.ApplyPixel(
            plan,
            source,
            width,
            height,
            x,
            y);
    }

    internal static Vector4 Texture(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource)
    {
        if (plan.Filter == PrismFilterId.OilPaint)
        {
            return PrismOilPaintFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }

        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        float texture = primaryResource is null
            ? Hash(x, y, Seed(plan, "Seed"))
            : Luminance(primaryResource(uv));
        float relief = Option(
            plan,
            "Relief",
            Option(plan, "Intensity", 20) * 0.01f);
        float edge = Sobel(source, width, height, x, y);
        float variant =
            (((int)plan.Filter - 123) % 4) * 0.08f;
        Vector3 straight = Unpremultiply(center);
        Vector3 textured = straight +
            new Vector3(
                (texture - 0.5f) * relief,
                (edge - 0.5f) * relief * 0.5f,
                (texture - edge) * (relief + variant) * 0.35f);
        return Associated(
            Vector3.Clamp(textured, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 OilPaint(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float stylization = Math.Clamp(
            Option(plan, "Stylization", 0.5f),
            0,
            1);
        float cleanliness = Math.Clamp(
            Option(plan, "Cleanliness", 0.5f),
            0,
            1);
        float bristleDetail = Math.Clamp(
            Option(plan, "BristleDetail", 0.5f),
            0,
            1);
        bool lighting = Option(plan, "Lighting", 1) >= 0.5f;
        float angle = Option(plan, "Angle", 0) *
            (MathF.PI / 180);
        float shine = Math.Clamp(
            Option(plan, "Shine", 0.5f),
            0,
            1);
        float radius = Math.Clamp(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1,
            12);
        float sharpness =
            1.5f +
            (8 * stylization) +
            (2 * cleanliness);
        float roughness =
            (1 - cleanliness) *
            bristleDetail *
            0.65f;
        Vector4 painted = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            1.1f + (0.5f * stylization),
            1 - (0.35f * stylization),
            roughness,
            0,
            0,
            0,
            false,
            0x6f696c50u);
        Vector3 straight = Unpremultiply(center);
        Vector3 result = Vector3.Lerp(
            straight,
            Unpremultiply(painted),
            0.35f + (0.65f * stylization));

        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float along = (x * cosine) + (y * sine);
        float across = (-x * sine) + (y * cosine);
        float ridge = 0.5f +
            (0.5f * MathF.Cos(
                along * (0.75f + (1.5f * bristleDetail)) +
                (MathF.Sin(across * 0.35f) * 1.2f)));
        float grain = Hash(
            (int)MathF.Floor(x / MathF.Max(radius * 0.75f, 1)),
            (int)MathF.Floor(y / MathF.Max(radius * 0.75f, 1)),
            0x62726973u);
        float bristle =
            (((0.65f * ridge) + (0.35f * grain)) - 0.5f) *
            bristleDetail *
            (1 - (0.65f * cleanliness)) *
            0.12f;
        result *= 1 + bristle;

        if (lighting)
        {
            float sampleOffset = MathF.Max(1, radius * 0.35f);
            float left = Luminance(SamplePixel(
                source,
                width,
                height,
                x - sampleOffset,
                y));
            float right = Luminance(SamplePixel(
                source,
                width,
                height,
                x + sampleOffset,
                y));
            float up = Luminance(SamplePixel(
                source,
                width,
                height,
                x,
                y - sampleOffset));
            float down = Luminance(SamplePixel(
                source,
                width,
                height,
                x,
                y + sampleOffset));
            float heightScale = 0.8f + (1.6f * stylization);
            Vector3 normal = Vector3.Normalize(new Vector3(
                (left - right) * heightScale,
                (up - down) * heightScale,
                1));
            Vector3 light = Vector3.Normalize(new Vector3(
                -cosine * 0.55f,
                -sine * 0.55f,
                0.85f));
            float diffuse = MathF.Max(Vector3.Dot(normal, light), 0);
            Vector3 halfVector = Vector3.Normalize(
                light + Vector3.UnitZ);
            float specular = MathF.Pow(
                MathF.Max(Vector3.Dot(normal, halfVector), 0),
                8 + (24 * (1 - shine))) *
                shine *
                0.16f;
            result =
                (result * (0.86f + (0.22f * diffuse))) +
                new Vector3(specular);
        }

        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }
}
