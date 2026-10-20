using LibTessDotNet;

namespace Cerneala.Drawing.Paths;

internal static class DrawPathMeshBuilder
{
    // Half the 1 px coverage ramp, in physical pixels.
    private const float FringeHalfWidth = 0.5f;
    // Longest miter, as a multiple of the fringe half width, before a convex corner bevels.
    private const float MaximumMiterLength = 2;

    // Builds the fill in physical pixels with a 1 px coverage ramp centred on every
    // boundary edge (coverage = clamp(0.5 - distance outside the edge, 0, 1)).
    public static DrawTriangleMesh Build(
        DrawPath path,
        DrawRect sourceBounds,
        float physicalWidth,
        float physicalHeight,
        float phaseX,
        float phaseY,
        DrawFillRule fillRule)
    {
        ArgumentNullException.ThrowIfNull(path);
        float scaleX = physicalWidth / sourceBounds.Width;
        float scaleY = physicalHeight / sourceBounds.Height;
        float tolerance = 0.05f / MathF.Max(scaleX, scaleY);
        IReadOnlyList<DrawPoint[]> contours = DrawPathFlattener.Flatten(path, tolerance);
        Tess boundary = new();
        foreach (DrawPoint[] contour in contours)
        {
            ContourVertex[] vertices = new ContourVertex[contour.Length];
            for (int index = 0; index < contour.Length; index++)
            {
                DrawPoint point = contour[index];
                vertices[index].Position = new Vec3(
                    phaseX + ((point.X - sourceBounds.X) * scaleX),
                    phaseY + ((point.Y - sourceBounds.Y) * scaleY),
                    0);
            }
            boundary.AddContour(vertices, ContourOrientation.Original);
        }

        // With the explicit +Z normal, LibTess returns the filled region's outlines
        // with the region on their left: outer contours have positive signed area
        // and holes negative, whatever the input orientation.
        boundary.Tessellate(
            fillRule == DrawFillRule.EvenOdd ? WindingRule.EvenOdd : WindingRule.NonZero,
            ElementType.BoundaryContours,
            3,
            null,
            new Vec3(0, 0, 1));
        if (boundary.ElementCount == 0)
        {
            return DrawTriangleMesh.Empty;
        }

        MeshBuilder mesh = new(boundary.VertexCount);
        Tess interior = new();
        for (int element = 0; element < boundary.ElementCount; element++)
        {
            int start = boundary.Elements[element * 2];
            int count = boundary.Elements[(element * 2) + 1];
            AddContour(mesh, interior, boundary.Vertices, start, count);
        }

        interior.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);
        int[] interiorIndices = new int[interior.VertexCount];
        for (int index = 0; index < interiorIndices.Length; index++)
        {
            ContourVertex vertex = interior.Vertices[index];
            interiorIndices[index] = vertex.Data is int inner
                ? inner
                : mesh.AddVertex(new DrawPoint(vertex.Position.X, vertex.Position.Y), default, 1);
        }
        foreach (int index in interior.Elements)
        {
            if (index != Tess.Undef)
            {
                mesh.AddIndex(interiorIndices[index]);
            }
        }

        return mesh.Build();
    }

    // Adds one boundary contour's inner (coverage 1, extruded inward) and outer
    // (coverage 0, extruded outward) vertex copies and the fringe between them, and
    // feeds the inner copies to the interior tessellation.
    private static void AddContour(
        MeshBuilder mesh,
        Tess interior,
        ContourVertex[] source,
        int start,
        int count)
    {
        DrawPoint[] points = new DrawPoint[count];
        int pointCount = 0;
        for (int index = 0; index < count; index++)
        {
            Vec3 position = source[start + index].Position;
            DrawPoint point = new(position.X, position.Y);
            if (pointCount == 0 || !NearlyEqual(points[pointCount - 1], point))
            {
                points[pointCount++] = point;
            }
        }
        while (pointCount > 1 && NearlyEqual(points[pointCount - 1], points[0]))
        {
            pointCount--;
        }
        if (pointCount < 3)
        {
            return;
        }

        // Per vertex: inner copy, then the outer copy at the start of its next edge,
        // and the outer copy at the end of its previous edge (the same vertex unless beveled).
        int[] inner = new int[pointCount];
        int[] outerStart = new int[pointCount];
        int[] outerEnd = new int[pointCount];
        ContourVertex[] contour = new ContourVertex[pointCount];
        for (int index = 0; index < pointCount; index++)
        {
            DrawPoint previous = points[(index + pointCount - 1) % pointCount];
            DrawPoint point = points[index];
            DrawPoint next = points[(index + 1) % pointCount];
            DrawPoint previousNormal = OutwardNormal(previous, point);
            DrawPoint nextNormal = OutwardNormal(point, next);
            float averageX = (previousNormal.X + nextNormal.X) / 2;
            float averageY = (previousNormal.Y + nextNormal.Y) / 2;
            float squaredLength = (averageX * averageX) + (averageY * averageY);
            const float MinimumSquaredLength = 1 / (MaximumMiterLength * MaximumMiterLength);
            if (squaredLength >= MinimumSquaredLength)
            {
                // Miter: its component along each edge normal is exactly one.
                DrawPoint miter = new(averageX / squaredLength, averageY / squaredLength);
                inner[index] = mesh.AddVertex(point, Scale(miter, -FringeHalfWidth), 1);
                outerStart[index] = outerEnd[index] = mesh.AddVertex(point, Scale(miter, FringeHalfWidth), 0);
            }
            else
            {
                // Sharp corner: a clamped inner miter and a beveled outer fringe.
                float length = MathF.Sqrt(squaredLength);
                DrawPoint direction = length > 1e-6f
                    ? new DrawPoint(averageX / length, averageY / length)
                    : nextNormal;
                inner[index] = mesh.AddVertex(point, Scale(direction, -FringeHalfWidth * MaximumMiterLength), 1);
                outerEnd[index] = mesh.AddVertex(point, Scale(previousNormal, FringeHalfWidth), 0);
                outerStart[index] = mesh.AddVertex(point, Scale(nextNormal, FringeHalfWidth), 0);
                mesh.AddTriangle(inner[index], outerEnd[index], outerStart[index]);
            }
            contour[index].Position = new Vec3(point.X, point.Y, 0);
            contour[index].Data = inner[index];
        }

        for (int index = 0; index < pointCount; index++)
        {
            int next = (index + 1) % pointCount;
            mesh.AddTriangle(inner[index], inner[next], outerEnd[next]);
            mesh.AddTriangle(inner[index], outerEnd[next], outerStart[index]);
        }
        interior.AddContour(contour, ContourOrientation.Original);
    }

    // The filled region is on the left of every boundary edge, so the outward
    // normal of direction (dx, dy) is (dy, -dx).
    private static DrawPoint OutwardNormal(DrawPoint start, DrawPoint end)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float length = MathF.Sqrt((dx * dx) + (dy * dy));
        return new DrawPoint(dy / length, -dx / length);
    }

    private static bool NearlyEqual(DrawPoint first, DrawPoint second) =>
        MathF.Abs(first.X - second.X) <= 1e-5f && MathF.Abs(first.Y - second.Y) <= 1e-5f;

    private static DrawPoint Scale(DrawPoint vector, float amount) =>
        new(vector.X * amount, vector.Y * amount);

    private sealed class MeshBuilder(int capacity)
    {
        private readonly List<DrawPoint> vertices = new(capacity * 2);
        private readonly List<DrawPoint> extrusions = new(capacity * 2);
        private readonly List<float> coverages = new(capacity * 2);
        private readonly List<int> indices = new(capacity * 9);

        public int AddVertex(DrawPoint position, DrawPoint extrusion, float coverage)
        {
            vertices.Add(position);
            extrusions.Add(extrusion);
            coverages.Add(coverage);
            return vertices.Count - 1;
        }

        public void AddIndex(int index) => indices.Add(index);

        public void AddTriangle(int first, int second, int third)
        {
            indices.Add(first);
            indices.Add(second);
            indices.Add(third);
        }

        public DrawTriangleMesh Build() => new(
            vertices.ToArray(), extrusions.ToArray(), coverages.ToArray(), indices.ToArray());
    }
}
