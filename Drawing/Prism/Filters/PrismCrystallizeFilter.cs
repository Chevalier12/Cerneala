using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCrystallizeFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float cell = MathF.Max(
            1,
            PrismCatalogFilterMath.Option(plan, "CellSize", 10));
        uint seed = PrismCatalogFilterMath.Seed(plan, "Seed");
        int cellX = (int)MathF.Floor(x / cell);
        int cellY = (int)MathF.Floor(y / cell);
        float nearestX = 0;
        float nearestY = 0;
        float nearestDistanceSquared = float.PositiveInfinity;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int candidateCellX = cellX + offsetX;
                int candidateCellY = cellY + offsetY;
                float candidateX =
                    (candidateCellX + PrismCatalogFilterMath.Hash(
                        candidateCellX,
                        candidateCellY,
                        seed)) * cell;
                float candidateY =
                    (candidateCellY + PrismCatalogFilterMath.Hash(
                        candidateCellX,
                        candidateCellY,
                        seed + 1)) * cell;
                float deltaX = x - candidateX;
                float deltaY = y - candidateY;
                float distanceSquared =
                    (deltaX * deltaX) + (deltaY * deltaY);
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestX = candidateX;
                nearestY = candidateY;
            }
        }
        return PrismCatalogFilterMath.SamplePixel(
            source,
            width,
            height,
            nearestX,
            nearestY);
    }
}
