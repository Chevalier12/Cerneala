using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismStainedGlassFilter
{
    private static readonly Vector2[] BorderDirections =
    [
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(0, 1),
        Vector2.Normalize(new Vector2(-1, -1)),
        Vector2.Normalize(new Vector2(1, -1)),
        Vector2.Normalize(new Vector2(-1, 1)),
        Vector2.Normalize(new Vector2(1, 1))
    ];

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float cellSize = Math.Clamp(
            Option(plan, "CellSize", 2),
            2,
            16384);
        float borderThickness = Math.Clamp(
            Option(plan, "BorderThickness", 0),
            0,
            1024);
        float lightIntensity = Math.Clamp(
            Option(plan, "LightIntensity", 0),
            0,
            10);
        Vector4 borderColor = OptionVector(
            plan,
            "BorderColor",
            new Vector4(0, 0, 0, 1));
        uint seed = Seed(plan);

        Vector2[] labels = SeedLabels(
            width,
            height,
            cellSize,
            seed);
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            if (pass.Kind != PrismCatalogFilterPassKind.Iteration)
            {
                continue;
            }

            labels = Flood(
                labels,
                width,
                height,
                Math.Max(1, (int)MathF.Round(pass.RadiusX)));
        }

        Vector4[] output = new Vector4[source.Length];
        Vector2 lightDirection = Vector2.Normalize(
            new Vector2(-0.65f, -0.75f));
        Vector3 borderStraight = Unpremultiply(borderColor);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 original = source[index];
                if (original.W <= 0)
                {
                    output[index] = Vector4.Zero;
                    continue;
                }

                Vector2 label = labels[index];
                if (!IsValid(label))
                {
                    label = new Vector2(x, y);
                }
                Vector4 sampled = SampleBilinear(
                    source,
                    width,
                    height,
                    label.X,
                    label.Y);
                Vector3 straight = sampled.W <= 0
                    ? Unpremultiply(original)
                    : Unpremultiply(sampled);
                Vector2 local =
                    (new Vector2(x, y) - label) / cellSize;
                float facet = Vector2.Dot(local, lightDirection);
                float shade = MathF.Max(
                    0,
                    1 +
                        (facet * (lightIntensity / 10) * 1.5f));
                straight = Vector3.Clamp(
                    straight * shade,
                    Vector3.Zero,
                    Vector3.One);

                float edge = IsBorder(
                    labels,
                    width,
                    height,
                    x,
                    y,
                    label,
                    borderThickness)
                    ? 1
                    : 0;
                float borderWeight =
                    edge * Math.Clamp(borderColor.W, 0, 1);
                straight = Vector3.Lerp(
                    straight,
                    borderStraight,
                    borderWeight);
                float alpha = Math.Clamp(original.W, 0, 1);
                output[index] = new Vector4(
                    Vector3.Clamp(straight, Vector3.Zero, Vector3.One) *
                        alpha,
                    alpha);
            }
        }
        return output;
    }

    private static Vector2[] SeedLabels(
        int width,
        int height,
        float cellSize,
        uint seed)
    {
        Vector2[] labels = new Vector2[width * height];
        Array.Fill(labels, new Vector2(-1, -1));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int baseCellX = (int)MathF.Floor(x / cellSize);
                int baseCellY = (int)MathF.Floor(y / cellSize);
                Vector2 best = new(-1, -1);
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int cellX = baseCellX + offsetX;
                        int cellY = baseCellY + offsetY;
                        if (!Intersects(
                                cellX,
                                cellY,
                                cellSize,
                                width,
                                height))
                        {
                            continue;
                        }

                        Vector2 feature = Feature(
                            cellX,
                            cellY,
                            cellSize,
                            seed,
                            width,
                            height);
                        int seedX = (int)MathF.Floor(feature.X + 0.5f);
                        int seedY = (int)MathF.Floor(feature.Y + 0.5f);
                        if (seedX != x || seedY != y ||
                            IsValid(best) && !LexicographicallyBefore(
                                feature,
                                best))
                        {
                            continue;
                        }
                        best = feature;
                    }
                }
                labels[(y * width) + x] = best;
            }
        }
        return labels;
    }

    private static Vector2[] Flood(
        Vector2[] labels,
        int width,
        int height,
        int jump)
    {
        Vector2[] output = new Vector2[labels.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 best = new(-1, -1);
                float bestDistance = float.PositiveInfinity;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int sampleY = y + (offsetY * jump);
                    if ((uint)sampleY >= (uint)height)
                    {
                        continue;
                    }
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        int sampleX = x + (offsetX * jump);
                        if ((uint)sampleX >= (uint)width)
                        {
                            continue;
                        }

                        Vector2 candidate =
                            labels[(sampleY * width) + sampleX];
                        if (!IsValid(candidate))
                        {
                            continue;
                        }
                        float distance = Vector2.DistanceSquared(
                            new Vector2(x, y),
                            candidate);
                        if (distance > bestDistance ||
                            distance == bestDistance &&
                            !LexicographicallyBefore(candidate, best))
                        {
                            continue;
                        }
                        bestDistance = distance;
                        best = candidate;
                    }
                }
                output[(y * width) + x] = best;
            }
        }
        return output;
    }

    private static bool IsBorder(
        Vector2[] labels,
        int width,
        int height,
        int x,
        int y,
        Vector2 label,
        float thickness)
    {
        if (thickness <= 0)
        {
            return false;
        }

        float radius = MathF.Max(thickness, 1);
        foreach (Vector2 direction in BorderDirections)
        {
            int sampleX = Math.Clamp(
                (int)MathF.Round(x + (direction.X * radius)),
                0,
                width - 1);
            int sampleY = Math.Clamp(
                (int)MathF.Round(y + (direction.Y * radius)),
                0,
                height - 1);
            Vector2 other = labels[(sampleY * width) + sampleX];
            if (IsValid(other) &&
                Vector2.DistanceSquared(label, other) > 0.0625f)
            {
                return true;
            }
        }
        return false;
    }

    private static Vector2 Feature(
        int cellX,
        int cellY,
        float cellSize,
        uint seed,
        int width,
        int height)
    {
        float x = (cellX + 0.15f +
            (0.7f * Random(cellX, cellY, seed ^ 0x13579bdfu))) *
            cellSize;
        float y = (cellY + 0.15f +
            (0.7f * Random(cellX, cellY, seed ^ 0x2468ace0u))) *
            cellSize;
        return new Vector2(
            Math.Clamp(x, 0, width - 1),
            Math.Clamp(y, 0, height - 1));
    }

    private static bool Intersects(
        int cellX,
        int cellY,
        float cellSize,
        int width,
        int height) =>
        (cellX * cellSize) < width &&
        ((cellX + 1) * cellSize) > 0 &&
        (cellY * cellSize) < height &&
        ((cellY + 1) * cellSize) > 0;

    private static float Random(
        int x,
        int y,
        uint seed) =>
        (Hash(x, y, seed) & 0x00ffffffu) / 16777215f;

    private static uint Hash(int x, int y, uint seed)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed;
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return value;
    }

    private static Vector4 SampleBilinear(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        float clampedX = Math.Clamp(x, 0, width - 1);
        float clampedY = Math.Clamp(y, 0, height - 1);
        int left = (int)MathF.Floor(clampedX);
        int top = (int)MathF.Floor(clampedY);
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float horizontal = clampedX - left;
        float vertical = clampedY - top;
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(upper, lower, vertical);
    }

    private static bool IsValid(Vector2 value) =>
        value.X >= 0 && value.Y >= 0;

    private static bool LexicographicallyBefore(
        Vector2 candidate,
        Vector2 current) =>
        !IsValid(current) ||
        candidate.Y < current.Y ||
        candidate.Y == current.Y && candidate.X < current.X;

    private static float Option(
        PrismCatalogFilterPlan plan,
        string name,
        float fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value.X
            : fallback;

    private static Vector4 OptionVector(
        PrismCatalogFilterPlan plan,
        string name,
        Vector4 fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value
            : fallback;

    private static uint Seed(PrismCatalogFilterPlan plan)
    {
        Vector4 value = OptionVector(plan, "Seed", Vector4.Zero);
        return ((uint)value.Y << 16) |
            ((uint)value.X & 0xffffu);
    }

    private static Vector3 Unpremultiply(Vector4 color) =>
        color.W <= 0
            ? Vector3.Zero
            : new Vector3(color.X, color.Y, color.Z) / color.W;
}
