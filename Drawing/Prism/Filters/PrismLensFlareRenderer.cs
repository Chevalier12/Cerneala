using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLensFlareRenderer
{
    private const int TileSize = 16;
    private const float MaximumIncidenceAngleDegrees = 60;
    private const float MaximumRadiance = 64;

    private static readonly (float Wavelength, Vector3 Color)[] Channels =
    [
        (650, new Vector3(1, 0, 0)),
        (550, new Vector3(0, 1, 0)),
        (450, new Vector3(0, 0, 1))
    ];

    public static Vector4[] Render(
        PrismLensProfileResource profile,
        int width,
        int height,
        Vector2 lightPosition,
        float brightness)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Lens-flare dimensions must be positive.");
        }
        if (!float.IsFinite(brightness) || brightness < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(brightness));
        }

        Vector4[] radiance = new Vector4[checked(width * height)];
        if (brightness == 0)
        {
            return radiance;
        }

        Vector2 offset = lightPosition - new Vector2(0.5f);
        float fieldRadius = Math.Clamp(
            offset.Length() / MathF.Sqrt(0.5f),
            0,
            1);
        float incidenceAngle = fieldRadius *
            MaximumIncidenceAngleDegrees;
        float rotation = offset.LengthSquared() > 1e-10f
            ? MathF.Atan2(offset.Y, offset.X)
            : 0;
        float cosine = MathF.Cos(rotation);
        float sine = MathF.Sin(rotation);

        int tileColumns = (width + TileSize - 1) / TileSize;
        int tileRows = (height + TileSize - 1) / TileSize;
        List<int>[] tileTriangles =
            Enumerable.Range(0, tileColumns * tileRows)
                .Select(_ => new List<int>())
                .ToArray();
        List<Triangle> triangles = [];

        foreach (PrismLensFlareGhost ghost in profile.Ghosts)
        {
            PrismLensFlarePolynomialRegion region =
                SelectRegion(ghost, incidenceAngle);
            foreach ((float wavelength, Vector3 color) in Channels)
            {
                AddGhostTriangles(
                    profile.PupilGridSize,
                    region,
                    incidenceAngle,
                    wavelength,
                    color,
                    cosine,
                    sine,
                    width,
                    height,
                    triangles,
                    tileTriangles,
                    tileColumns,
                    tileRows);
            }
        }

        Rasterize(
            triangles,
            tileTriangles,
            tileColumns,
            tileRows,
            width,
            height,
            brightness,
            radiance);
        return radiance;
    }

    private static PrismLensFlarePolynomialRegion SelectRegion(
        PrismLensFlareGhost ghost,
        float incidenceAngle)
    {
        foreach (PrismLensFlarePolynomialRegion region in ghost.Regions)
        {
            if (incidenceAngle >= region.MinimumIncidenceAngleDegrees &&
                incidenceAngle < region.MaximumIncidenceAngleDegrees)
            {
                return region;
            }
        }

        return ghost.Regions
            .MinBy(region => MathF.Min(
                MathF.Abs(incidenceAngle -
                    region.MinimumIncidenceAngleDegrees),
                MathF.Abs(incidenceAngle -
                    region.MaximumIncidenceAngleDegrees)))!;
    }

    private static void AddGhostTriangles(
        int gridSize,
        PrismLensFlarePolynomialRegion region,
        float incidenceAngle,
        float wavelength,
        Vector3 color,
        float cosine,
        float sine,
        int width,
        int height,
        List<Triangle> triangles,
        List<int>[] tileTriangles,
        int tileColumns,
        int tileRows)
    {
        Vertex[] vertices = new Vertex[gridSize * gridSize];
        for (int row = 0; row < gridSize; row++)
        {
            float pupilY = -1 + (2f * row / (gridSize - 1));
            for (int column = 0; column < gridSize; column++)
            {
                float pupilX =
                    -1 + (2f * column / (gridSize - 1));
                Vector2 pupil = new(pupilX, pupilY);
                float radius = pupil.Length();
                if (radius > 1)
                {
                    continue;
                }

                PrismLensFlarePolynomialInput input =
                    PrismLensProfileFitter.Normalize(
                        pupil,
                        incidenceAngle,
                        wavelength);
                Vector2 aperture = new(
                    region.ApertureX.Evaluate(input),
                    region.ApertureY.Evaluate(input));
                float relativeRadius =
                    region.RelativeRadius.Evaluate(input);
                float transmission =
                    region.Transmission.Evaluate(input);
                Vector2 sensor = new(
                    region.SensorX.Evaluate(input),
                    region.SensorY.Evaluate(input));
                if (!IsFinite(aperture) ||
                    !IsFinite(sensor) ||
                    !float.IsFinite(relativeRadius) ||
                    !float.IsFinite(transmission) ||
                    aperture.LengthSquared() > 1 ||
                    relativeRadius is < 0 or > 1 ||
                    transmission <= 0)
                {
                    continue;
                }

                Vector2 rotated = new(
                    (sensor.X * cosine) - (sensor.Y * sine),
                    (sensor.X * sine) + (sensor.Y * cosine));
                Vector2 screen = new(
                    ((rotated.X * 0.5f) + 0.5f) * width,
                    ((rotated.Y * 0.5f) + 0.5f) * height);
                vertices[(row * gridSize) + column] = new Vertex(
                    screen,
                    Math.Clamp(transmission, 0, MaximumRadiance),
                    true);
            }
        }

        for (int row = 0; row < gridSize - 1; row++)
        {
            for (int column = 0; column < gridSize - 1; column++)
            {
                int topLeft = (row * gridSize) + column;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + gridSize;
                int bottomRight = bottomLeft + 1;
                AddTriangle(
                    vertices[topLeft],
                    vertices[topRight],
                    vertices[bottomRight],
                    color,
                    width,
                    height,
                    triangles,
                    tileTriangles,
                    tileColumns,
                    tileRows);
                AddTriangle(
                    vertices[topLeft],
                    vertices[bottomRight],
                    vertices[bottomLeft],
                    color,
                    width,
                    height,
                    triangles,
                    tileTriangles,
                    tileColumns,
                    tileRows);
            }
        }
    }

    private static void AddTriangle(
        Vertex a,
        Vertex b,
        Vertex c,
        Vector3 color,
        int width,
        int height,
        List<Triangle> triangles,
        List<int>[] tileTriangles,
        int tileColumns,
        int tileRows)
    {
        if (!a.IsValid || !b.IsValid || !c.IsValid)
        {
            return;
        }

        float area = Edge(a.Position, b.Position, c.Position);
        if (MathF.Abs(area) < 1e-5f)
        {
            return;
        }

        float minimumX = MathF.Min(
            a.Position.X,
            MathF.Min(b.Position.X, c.Position.X));
        float maximumX = MathF.Max(
            a.Position.X,
            MathF.Max(b.Position.X, c.Position.X));
        float minimumY = MathF.Min(
            a.Position.Y,
            MathF.Min(b.Position.Y, c.Position.Y));
        float maximumY = MathF.Max(
            a.Position.Y,
            MathF.Max(b.Position.Y, c.Position.Y));
        if (maximumX < 0 ||
            maximumY < 0 ||
            minimumX >= width ||
            minimumY >= height)
        {
            return;
        }

        int triangleIndex = triangles.Count;
        triangles.Add(new Triangle(a, b, c, color, area));
        int minimumTileX = Math.Clamp(
            (int)MathF.Floor(minimumX / TileSize),
            0,
            tileColumns - 1);
        int maximumTileX = Math.Clamp(
            (int)MathF.Floor(maximumX / TileSize),
            0,
            tileColumns - 1);
        int minimumTileY = Math.Clamp(
            (int)MathF.Floor(minimumY / TileSize),
            0,
            tileRows - 1);
        int maximumTileY = Math.Clamp(
            (int)MathF.Floor(maximumY / TileSize),
            0,
            tileRows - 1);
        for (int tileY = minimumTileY;
            tileY <= maximumTileY;
            tileY++)
        {
            for (int tileX = minimumTileX;
                tileX <= maximumTileX;
                tileX++)
            {
                tileTriangles[(tileY * tileColumns) + tileX]
                    .Add(triangleIndex);
            }
        }
    }

    private static void Rasterize(
        IReadOnlyList<Triangle> triangles,
        IReadOnlyList<int>[] tileTriangles,
        int tileColumns,
        int tileRows,
        int width,
        int height,
        float brightness,
        Vector4[] destination)
    {
        for (int tileY = 0; tileY < tileRows; tileY++)
        {
            int startY = tileY * TileSize;
            int endY = Math.Min(startY + TileSize, height);
            for (int tileX = 0; tileX < tileColumns; tileX++)
            {
                IReadOnlyList<int> indices =
                    tileTriangles[(tileY * tileColumns) + tileX];
                if (indices.Count == 0)
                {
                    continue;
                }

                int startX = tileX * TileSize;
                int endX = Math.Min(startX + TileSize, width);
                foreach (int triangleIndex in indices)
                {
                    Triangle triangle = triangles[triangleIndex];
                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            Vector2 point = new(x + 0.5f, y + 0.5f);
                            float a = Edge(
                                triangle.B.Position,
                                triangle.C.Position,
                                point) / triangle.Area;
                            float b = Edge(
                                triangle.C.Position,
                                triangle.A.Position,
                                point) / triangle.Area;
                            float c = 1 - a - b;
                            const float edgeTolerance = -1e-5f;
                            if (a < edgeTolerance ||
                                b < edgeTolerance ||
                                c < edgeTolerance)
                            {
                                continue;
                            }

                            float transmission =
                                (a * triangle.A.Transmission) +
                                (b * triangle.B.Transmission) +
                                (c * triangle.C.Transmission);
                            Vector3 contribution = triangle.Color *
                                transmission *
                                brightness;
                            int pixelIndex = (y * width) + x;
                            Vector4 current = destination[pixelIndex];
                            Vector3 rgb = Vector3.Min(
                                new Vector3(MaximumRadiance),
                                new Vector3(current.X, current.Y, current.Z) +
                                    contribution);
                            destination[pixelIndex] =
                                new Vector4(rgb, 1);
                        }
                    }
                }
            }
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 point) =>
        ((point.X - a.X) * (b.Y - a.Y)) -
        ((point.Y - a.Y) * (b.X - a.X));

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private readonly record struct Vertex(
        Vector2 Position,
        float Transmission,
        bool IsValid);

    private readonly record struct Triangle(
        Vertex A,
        Vertex B,
        Vertex C,
        Vector3 Color,
        float Area);
}
