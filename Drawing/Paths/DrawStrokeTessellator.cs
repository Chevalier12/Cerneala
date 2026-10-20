namespace Cerneala.Drawing.Paths;

internal readonly record struct DrawStrokeContour(
    IReadOnlyList<DrawPoint> Points,
    bool IsClosed);

// Vertices are logical and lie on the exact stroke edges. Extrusions are physical
// pixels and coverages 0..1, as in DrawTriangleMesh.
internal sealed record DrawStrokeMesh(
    DrawPoint[] Vertices,
    DrawPoint[] Extrusions,
    float[] Coverages,
    int[] Indices)
{
    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}

// Tessellates each stroke contour as one continuous strip of cross-sections with
// four rows: outer fringe (coverage 0), solid, solid, outer fringe (coverage 0).
// The fringe rows are pushed half a physical pixel outside the exact edges and
// the solid rows half a pixel inside, so the edge gets the 1 px coverage ramp
// coverage = clamp(0.5 - distance outside the edge, 0, 1).
internal static class DrawStrokeTessellator
{
    private const float FringeHalfWidth = 0.5f;
    private const float RoundStep = MathF.PI / 12;

    internal static IReadOnlyList<DrawStrokeContour> ApplyDashesForDiagnostics(
        DrawStrokeContour contour,
        DrawStrokeStyle style) =>
        ApplyDashes(contour, style);

    // pixelScale converts logical units to physical pixels. A stroke thinner than
    // one physical pixel keeps its exact edges but paints a trapezoid profile
    // whose integrated coverage equals its physical width.
    public static DrawStrokeMesh Tessellate(
        IReadOnlyList<DrawStrokeContour> contours,
        float thickness,
        DrawStrokeStyle style,
        float pixelScale = 1)
    {
        ArgumentNullException.ThrowIfNull(contours);
        ArgumentNullException.ThrowIfNull(style);
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        MeshBuilder mesh = new();
        foreach (DrawStrokeContour contour in contours)
        {
            (float leftOffset, float rightOffset) = ResolveOffsets(
                contour,
                thickness,
                style.Alignment);
            float width = (leftOffset + rightOffset) * pixelScale;
            float coverage = MathF.Min(width, 1);
            StrokeProfile profile = new(
                leftOffset,
                rightOffset,
                thickness,
                coverage,
                coverage - FringeHalfWidth,
                pixelScale);
            foreach (DrawStrokeContour stroke in ApplyDashes(contour, style))
            {
                TessellateContour(mesh, stroke, style, profile);
            }
        }

        return mesh.Build();
    }

    private static IReadOnlyList<DrawStrokeContour> ApplyDashes(
        DrawStrokeContour contour,
        DrawStrokeStyle style)
    {
        if (style.DashPattern.Count == 0)
        {
            return [contour];
        }

        float[] pattern = style.DashPattern.Count % 2 == 0
            ? style.DashPattern.ToArray()
            : style.DashPattern.Concat(style.DashPattern).ToArray();
        float patternLength = pattern.Sum();
        float offset = style.DashOffset % patternLength;
        if (offset < 0)
        {
            offset += patternLength;
        }

        int patternIndex = 0;
        while (offset >= pattern[patternIndex])
        {
            offset -= pattern[patternIndex];
            patternIndex = (patternIndex + 1) % pattern.Length;
        }
        float patternRemaining = pattern[patternIndex] - offset;
        bool drawing = patternIndex % 2 == 0;
        List<DrawStrokeContour> result = [];
        List<DrawPoint>? active = null;
        int edgeCount = contour.IsClosed
            ? contour.Points.Count
            : contour.Points.Count - 1;

        for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
        {
            DrawPoint edgeStart = contour.Points[edgeIndex];
            DrawPoint edgeEnd = contour.Points[(edgeIndex + 1) % contour.Points.Count];
            float edgeLength = Distance(edgeStart, edgeEnd);
            if (edgeLength <= float.Epsilon)
            {
                continue;
            }

            float consumed = 0;
            while (consumed < edgeLength - float.Epsilon)
            {
                float length = MathF.Min(patternRemaining, edgeLength - consumed);
                DrawPoint start = Lerp(edgeStart, edgeEnd, consumed / edgeLength);
                DrawPoint end = Lerp(edgeStart, edgeEnd, (consumed + length) / edgeLength);
                if (drawing)
                {
                    active ??= [start];
                    AddPoint(active, end);
                }
                else if (active is { Count: >= 2 })
                {
                    result.Add(new DrawStrokeContour(active.ToArray(), false));
                    active = null;
                }

                consumed += length;
                patternRemaining -= length;
                if (patternRemaining <= float.Epsilon)
                {
                    if (drawing && active is { Count: >= 2 })
                    {
                        result.Add(new DrawStrokeContour(active.ToArray(), false));
                        active = null;
                    }
                    patternIndex = (patternIndex + 1) % pattern.Length;
                    drawing = patternIndex % 2 == 0;
                    patternRemaining = pattern[patternIndex];
                }
            }
        }

        if (active is { Count: >= 2 })
        {
            result.Add(new DrawStrokeContour(active.ToArray(), false));
        }

        if (contour.IsClosed && result.Count > 1)
        {
            DrawStrokeContour first = result[0];
            DrawStrokeContour last = result[^1];
            if (last.Points[^1] == first.Points[0])
            {
                DrawPoint[] merged = last.Points
                    .Concat(first.Points.Skip(1))
                    .ToArray();
                result[0] = new DrawStrokeContour(merged, false);
                result.RemoveAt(result.Count - 1);
            }
        }

        return result;
    }

    private static void TessellateContour(
        MeshBuilder mesh,
        DrawStrokeContour contour,
        DrawStrokeStyle style,
        StrokeProfile profile)
    {
        DrawPoint[] points = RemoveRepeatedPoints(contour);
        int count = points.Length;
        if (count < 2)
        {
            return;
        }

        // A closed contour joins at every vertex and its last segment ends in the
        // first vertex's join, so the strip has no seam.
        Section firstEnd = default;
        Section previousStart;
        if (contour.IsClosed)
        {
            (firstEnd, previousStart) = AddJoin(mesh, points[^1], points[0], points[1], style, profile);
        }
        else
        {
            previousStart = AddCap(mesh, points[0], points[1], style.StartCap, profile, start: true);
        }

        for (int index = 1; index < count; index++)
        {
            if (!contour.IsClosed && index == count - 1)
            {
                Section end = AddCap(mesh, points[index], points[index - 1], style.EndCap, profile, start: false);
                Connect(mesh, previousStart, end);
                return;
            }

            (Section joinEnd, Section joinStart) = AddJoin(
                mesh,
                points[index - 1],
                points[index],
                points[(index + 1) % count],
                style,
                profile);
            Connect(mesh, previousStart, joinEnd);
            previousStart = joinStart;
        }

        Connect(mesh, previousStart, firstEnd);
    }

    private static DrawPoint[] RemoveRepeatedPoints(DrawStrokeContour contour)
    {
        IReadOnlyList<DrawPoint> source = contour.Points;
        List<DrawPoint> points = new(source.Count);
        foreach (DrawPoint point in source)
        {
            if (points.Count == 0 || Distance(points[^1], point) > float.Epsilon)
            {
                points.Add(point);
            }
        }
        if (contour.IsClosed)
        {
            while (points.Count > 1 && Distance(points[^1], points[0]) <= float.Epsilon)
            {
                points.RemoveAt(points.Count - 1);
            }
        }
        return points.ToArray();
    }

    private static (float Left, float Right) ResolveOffsets(
        DrawStrokeContour contour,
        float thickness,
        DrawStrokeAlignment alignment)
    {
        if (!contour.IsClosed || alignment == DrawStrokeAlignment.Center)
        {
            return (thickness / 2, thickness / 2);
        }

        float twiceArea = 0;
        for (int index = 0; index < contour.Points.Count; index++)
        {
            DrawPoint current = contour.Points[index];
            DrawPoint next = contour.Points[(index + 1) % contour.Points.Count];
            twiceArea += (current.X * next.Y) - (next.X * current.Y);
        }
        bool interiorOnLeft = twiceArea > 0;
        bool inside = alignment == DrawStrokeAlignment.Inside;
        if (inside == interiorOnLeft)
        {
            return (thickness, 0);
        }
        return (0, thickness);
    }

    // Returns the cross-section that ends the incoming segment and the one that
    // starts the outgoing segment (the same section for a straight or mitered join).
    private static (Section End, Section Start) AddJoin(
        MeshBuilder mesh,
        DrawPoint previous,
        DrawPoint point,
        DrawPoint next,
        DrawStrokeStyle style,
        StrokeProfile profile)
    {
        DrawPoint previousDirection = Direction(previous, point);
        DrawPoint nextDirection = Direction(point, next);
        DrawPoint previousNormal = Normal(previousDirection);
        DrawPoint nextNormal = Normal(nextDirection);
        float cross = Cross(previousDirection, nextDirection);
        if (MathF.Abs(cross) <= 0.00001f && Dot(previousDirection, nextDirection) > 0)
        {
            Section straight = AddSection(mesh, point, previousNormal, profile);
            return (straight, straight);
        }

        // A turn toward the left normal has its outer side on the right.
        bool outerIsLeft = cross < 0;
        float sign = outerIsLeft ? 1 : -1;
        float outerOffset = outerIsLeft ? profile.Left : profile.Right;
        float innerOffset = outerIsLeft ? profile.Right : profile.Left;
        DrawPoint average = Multiply(Add(previousNormal, nextNormal), 0.5f);
        float squaredLength = Dot(average, average);
        bool hasMiter = squaredLength > 1e-12f;
        // The miter's component along each segment normal is exactly one.
        DrawPoint miter = hasMiter ? Multiply(average, 1 / squaredLength) : default;
        float miterLength = hasMiter ? 1 / MathF.Sqrt(squaredLength) : float.PositiveInfinity;
        // The inner miter point lies innerReach * sqrt(miterLength^2 - 1) along each
        // segment from the joint. The joins at both ends of a segment share its
        // length, so each may reach at most half of it before the inner edges cross.
        float innerReach = innerOffset + (FringeHalfWidth / profile.PixelScale);
        float alongSegment = innerReach * MathF.Sqrt(MathF.Max((miterLength * miterLength) - 1, 0));
        float shortestSegment = MathF.Min(Distance(previous, point), Distance(point, next));
        bool innerBevel = !hasMiter || alongSegment > shortestSegment / 2;
        bool outerMiter = style.Join == DrawLineJoin.Miter && hasMiter &&
            miterLength * outerOffset <= style.MiterLimit * MathF.Max(profile.Thickness / 2, outerOffset);
        if (!innerBevel && outerMiter)
        {
            Section mitered = AddSection(mesh, point, miter, profile);
            return (mitered, mitered);
        }

        DrawPoint previousOuter = Multiply(previousNormal, sign);
        DrawPoint nextOuter = Multiply(nextNormal, sign);
        Side outerEnd = AddSide(mesh, point, previousOuter, outerOffset, profile);
        Side outerStart = AddSide(mesh, point, nextOuter, outerOffset, profile);
        Side innerEnd;
        Side innerStart;
        int pivot;
        if (innerBevel)
        {
            // The inner miter would pass the neighbouring segments: both segments
            // keep square ends that overlap on the inner side.
            innerEnd = AddSide(mesh, point, Multiply(previousOuter, -1), innerOffset, profile);
            innerStart = AddSide(mesh, point, Multiply(nextOuter, -1), innerOffset, profile);
            pivot = mesh.AddVertex(point, default, profile.Coverage);
        }
        else
        {
            innerEnd = innerStart = AddSide(mesh, point, Multiply(miter, -sign), innerOffset, profile);
            pivot = innerEnd.Solid;
        }

        Side previousSide = outerEnd;
        if (style.Join == DrawLineJoin.Round)
        {
            float sweep = MathF.Atan2(Cross(previousOuter, nextOuter), Dot(previousOuter, nextOuter));
            int steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(sweep) / RoundStep));
            float startAngle = MathF.Atan2(previousOuter.Y, previousOuter.X);
            for (int step = 1; step < steps; step++)
            {
                float angle = startAngle + (sweep * step / steps);
                Side arc = AddSide(mesh, point, new DrawPoint(MathF.Cos(angle), MathF.Sin(angle)), outerOffset, profile);
                AddWedge(mesh, pivot, previousSide, arc);
                previousSide = arc;
            }
        }
        else if (outerMiter)
        {
            Side tip = AddSide(mesh, point, Multiply(miter, sign), outerOffset, profile);
            AddWedge(mesh, pivot, previousSide, tip);
            previousSide = tip;
        }
        AddWedge(mesh, pivot, previousSide, outerStart);

        return outerIsLeft
            ? (new Section(outerEnd, innerEnd), new Section(outerStart, innerStart))
            : (new Section(innerEnd, outerEnd), new Section(innerStart, outerStart));
    }

    // Returns the cross-section where the contour's first or last segment starts or ends.
    private static Section AddCap(
        MeshBuilder mesh,
        DrawPoint point,
        DrawPoint adjacent,
        DrawLineCap cap,
        StrokeProfile profile,
        bool start)
    {
        DrawPoint direction = start ? Direction(point, adjacent) : Direction(adjacent, point);
        DrawPoint normal = Normal(direction);
        DrawPoint outward = start ? Multiply(direction, -1) : direction;
        float extension = profile.Thickness / 2;
        // An Inside/Outside band is offset from the contour line, so round and
        // triangle caps are built around the middle of the band, not the contour point.
        DrawPoint middle = Add(point, Multiply(normal, (profile.Left - profile.Right) / 2));
        switch (cap)
        {
            case DrawLineCap.Square:
                return AddButt(mesh, Add(point, Multiply(outward, extension)), normal, outward, profile);
            case DrawLineCap.Triangle:
            {
                Section section = AddSection(mesh, point, normal, profile);
                DrawPoint tip = Add(middle, Multiply(outward, extension));
                // The tip's miter for two 45 degree edges is sqrt(2) long.
                float miterLength = MathF.Sqrt(2);
                int tipSolid = mesh.AddVertex(
                    tip,
                    Multiply(outward, -profile.Inset * miterLength),
                    profile.Coverage);
                int tipFringe = mesh.AddVertex(
                    tip,
                    Multiply(outward, FringeHalfWidth * miterLength),
                    0);
                mesh.AddTriangle(section.Left.Solid, section.Right.Solid, tipSolid);
                mesh.AddQuad(section.Left.Fringe, section.Left.Solid, tipSolid, tipFringe);
                mesh.AddQuad(section.Right.Solid, section.Right.Fringe, tipFringe, tipSolid);
                return section;
            }
            case DrawLineCap.Round:
            {
                Section section = AddSection(mesh, point, normal, profile);
                int center = mesh.AddVertex(middle, default, profile.Coverage);
                float sweep = start ? MathF.PI : -MathF.PI;
                int steps = Math.Max(2, (int)MathF.Ceiling(MathF.PI / RoundStep));
                float startAngle = MathF.Atan2(normal.Y, normal.X);
                Side previousSide = section.Left;
                for (int step = 1; step < steps; step++)
                {
                    float angle = startAngle + (sweep * step / steps);
                    Side arc = AddSide(mesh, middle, new DrawPoint(MathF.Cos(angle), MathF.Sin(angle)), extension, profile);
                    AddWedge(mesh, center, previousSide, arc);
                    previousSide = arc;
                }
                AddWedge(mesh, center, previousSide, section.Right);
                return section;
            }
            default:
                return AddButt(mesh, point, normal, outward, profile);
        }
    }

    // A flat end through point: coverage ramps from zero half a pixel outside it to
    // full half a pixel inside it.
    private static Section AddButt(
        MeshBuilder mesh,
        DrawPoint point,
        DrawPoint normal,
        DrawPoint outward,
        StrokeProfile profile)
    {
        DrawPoint outside = Multiply(outward, FringeHalfWidth);
        DrawPoint inside = Multiply(outward, -FringeHalfWidth);
        Section outer = new(
            AddSide(mesh, point, normal, profile.Left, profile, outside, 0),
            AddSide(mesh, point, Multiply(normal, -1), profile.Right, profile, outside, 0));
        Section inner = new(
            AddSide(mesh, point, normal, profile.Left, profile, inside, profile.Coverage),
            AddSide(mesh, point, Multiply(normal, -1), profile.Right, profile, inside, profile.Coverage));
        Connect(mesh, outer, inner);
        return inner;
    }

    private static Section AddSection(MeshBuilder mesh, DrawPoint point, DrawPoint left, StrokeProfile profile) =>
        new(
            AddSide(mesh, point, left, profile.Left, profile),
            AddSide(mesh, point, Multiply(left, -1), profile.Right, profile));

    // One side of a cross-section: the edge point offset along outward (a unit
    // normal or a miter), with a solid vertex pulled inward by the profile inset
    // and a fringe vertex pushed out half a pixel.
    private static Side AddSide(
        MeshBuilder mesh,
        DrawPoint point,
        DrawPoint outward,
        float offset,
        StrokeProfile profile) =>
        AddSide(mesh, point, outward, offset, profile, default, profile.Coverage);

    private static Side AddSide(
        MeshBuilder mesh,
        DrawPoint point,
        DrawPoint outward,
        float offset,
        StrokeProfile profile,
        DrawPoint shift,
        float solidCoverage)
    {
        DrawPoint edge = Add(point, Multiply(outward, offset));
        int solid = mesh.AddVertex(edge, Add(Multiply(outward, -profile.Inset), shift), solidCoverage);
        int fringe = mesh.AddVertex(edge, Add(Multiply(outward, FringeHalfWidth), shift), 0);
        return new Side(solid, fringe);
    }

    private static void Connect(MeshBuilder mesh, Section first, Section second)
    {
        mesh.AddQuad(first.Left.Fringe, second.Left.Fringe, second.Left.Solid, first.Left.Solid);
        mesh.AddQuad(first.Left.Solid, second.Left.Solid, second.Right.Solid, first.Right.Solid);
        mesh.AddQuad(first.Right.Solid, second.Right.Solid, second.Right.Fringe, first.Right.Fringe);
    }

    // The solid triangle from the pivot to one outer edge step, and its fringe.
    private static void AddWedge(MeshBuilder mesh, int pivot, Side first, Side second)
    {
        mesh.AddTriangle(pivot, first.Solid, second.Solid);
        mesh.AddQuad(first.Solid, second.Solid, second.Fringe, first.Fringe);
    }

    private static DrawPoint Direction(DrawPoint start, DrawPoint end)
    {
        float length = Distance(start, end);
        return new DrawPoint((end.X - start.X) / length, (end.Y - start.Y) / length);
    }

    private static DrawPoint Normal(DrawPoint direction) => new(-direction.Y, direction.X);

    private static void AddPoint(List<DrawPoint> points, DrawPoint point)
    {
        if (points.Count == 0 || points[^1] != point)
        {
            points.Add(point);
        }
    }

    private static DrawPoint Lerp(DrawPoint first, DrawPoint second, float amount) =>
        new(
            first.X + ((second.X - first.X) * amount),
            first.Y + ((second.Y - first.Y) * amount));

    private static DrawPoint Add(DrawPoint first, DrawPoint second) =>
        new(first.X + second.X, first.Y + second.Y);

    private static DrawPoint Multiply(DrawPoint point, float amount) =>
        new(point.X * amount, point.Y * amount);

    private static float Cross(DrawPoint first, DrawPoint second) =>
        (first.X * second.Y) - (first.Y * second.X);

    private static float Dot(DrawPoint first, DrawPoint second) =>
        (first.X * second.X) + (first.Y * second.Y);

    private static float Distance(DrawPoint first, DrawPoint second)
    {
        float dx = second.X - first.X;
        float dy = second.Y - first.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    // Left and Right are the exact edge offsets (logical). Coverage is the solid
    // rows' coverage, min(physical width, 1); Inset moves the solid rows inside
    // the exact edges by Coverage - 0.5 physical pixels.
    private readonly record struct StrokeProfile(
        float Left,
        float Right,
        float Thickness,
        float Coverage,
        float Inset,
        float PixelScale);

    private readonly record struct Side(int Solid, int Fringe);

    private readonly record struct Section(Side Left, Side Right);

    private sealed class MeshBuilder
    {
        private readonly List<DrawPoint> vertices = [];
        private readonly List<DrawPoint> extrusions = [];
        private readonly List<float> coverages = [];
        private readonly List<int> indices = [];

        public int AddVertex(DrawPoint position, DrawPoint extrusion, float coverage)
        {
            vertices.Add(position);
            extrusions.Add(extrusion);
            coverages.Add(coverage);
            return vertices.Count - 1;
        }

        public void AddTriangle(int first, int second, int third)
        {
            indices.Add(first);
            indices.Add(second);
            indices.Add(third);
        }

        public void AddQuad(int first, int second, int third, int fourth)
        {
            AddTriangle(first, second, third);
            AddTriangle(first, third, fourth);
        }

        public DrawStrokeMesh Build() => new(
            vertices.ToArray(), extrusions.ToArray(), coverages.ToArray(), indices.ToArray());
    }
}
