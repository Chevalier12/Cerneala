namespace Cerneala.Drawing.Paths;

internal readonly record struct DrawStrokeContour(
    IReadOnlyList<DrawPoint> Points,
    bool IsClosed);

internal sealed record DrawStrokeMesh(DrawPoint[] Vertices, int[] Indices)
{
    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}

internal static class DrawStrokeTessellator
{
    internal static IReadOnlyList<DrawStrokeContour> ApplyDashesForDiagnostics(
        DrawStrokeContour contour,
        DrawStrokeStyle style) =>
        ApplyDashes(contour, style);

    public static DrawStrokeMesh Tessellate(
        IReadOnlyList<DrawStrokeContour> contours,
        float thickness,
        DrawStrokeStyle style)
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
            foreach (DrawStrokeContour stroke in ApplyDashes(contour, style))
            {
                TessellateContour(
                    mesh,
                    stroke,
                    thickness,
                    style,
                    leftOffset,
                    rightOffset);
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
        float thickness,
        DrawStrokeStyle style,
        float leftOffset,
        float rightOffset)
    {
        IReadOnlyList<DrawPoint> points = contour.Points;
        if (points.Count < 2)
        {
            return;
        }

        int edgeCount = contour.IsClosed ? points.Count : points.Count - 1;
        for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
        {
            DrawPoint start = points[edgeIndex];
            DrawPoint end = points[(edgeIndex + 1) % points.Count];
            if (!TryDirection(start, end, out DrawPoint direction, out DrawPoint normal))
            {
                continue;
            }

            DrawPoint startLeft = Add(start, Multiply(normal, leftOffset));
            DrawPoint startRight = Add(start, Multiply(normal, -rightOffset));
            DrawPoint endLeft = Add(end, Multiply(normal, leftOffset));
            DrawPoint endRight = Add(end, Multiply(normal, -rightOffset));
            mesh.AddQuad(startLeft, endLeft, endRight, startRight);
        }

        int joinStart = contour.IsClosed ? 0 : 1;
        int joinEnd = contour.IsClosed ? points.Count : points.Count - 1;
        for (int pointIndex = joinStart; pointIndex < joinEnd; pointIndex++)
        {
            int previousIndex = (pointIndex - 1 + points.Count) % points.Count;
            int nextIndex = (pointIndex + 1) % points.Count;
            AddJoin(
                mesh,
                points[previousIndex],
                points[pointIndex],
                points[nextIndex],
                leftOffset,
                rightOffset,
                thickness,
                style);
        }

        if (!contour.IsClosed)
        {
            AddCap(mesh, points[0], points[1], leftOffset, rightOffset, thickness, style.StartCap, start: true);
            AddCap(mesh, points[^1], points[^2], leftOffset, rightOffset, thickness, style.EndCap, start: false);
        }
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

    private static void AddJoin(
        MeshBuilder mesh,
        DrawPoint previous,
        DrawPoint point,
        DrawPoint next,
        float leftOffset,
        float rightOffset,
        float thickness,
        DrawStrokeStyle style)
    {
        if (!TryDirection(previous, point, out DrawPoint previousDirection, out DrawPoint previousNormal) ||
            !TryDirection(point, next, out DrawPoint nextDirection, out DrawPoint nextNormal))
        {
            return;
        }

        float cross = Cross(previousDirection, nextDirection);
        if (MathF.Abs(cross) <= 0.00001f)
        {
            return;
        }

        float sideOffset = cross > 0 ? -rightOffset : leftOffset;
        if (MathF.Abs(sideOffset) <= float.Epsilon)
        {
            return;
        }
        DrawPoint outerPrevious = Add(point, Multiply(previousNormal, sideOffset));
        DrawPoint outerNext = Add(point, Multiply(nextNormal, sideOffset));

        if (style.Join == DrawLineJoin.Round)
        {
            float startAngle = MathF.Atan2(
                outerPrevious.Y - point.Y,
                outerPrevious.X - point.X);
            float endAngle = MathF.Atan2(
                outerNext.Y - point.Y,
                outerNext.X - point.X);
            float delta = NormalizeJoinSweep(endAngle - startAngle, cross);
            mesh.AddFan(point, MathF.Abs(sideOffset), startAngle, delta);
            return;
        }

        if (style.Join == DrawLineJoin.Miter &&
            TryLineIntersection(
                outerPrevious,
                previousDirection,
                outerNext,
                nextDirection,
                out DrawPoint miter) &&
            Distance(point, miter) <= style.MiterLimit * MathF.Max(thickness / 2, MathF.Abs(sideOffset)))
        {
            mesh.AddTriangle(outerPrevious, miter, outerNext);
            mesh.AddTriangle(point, outerPrevious, outerNext);
            return;
        }

        mesh.AddTriangle(point, outerPrevious, outerNext);
    }

    private static void AddCap(
        MeshBuilder mesh,
        DrawPoint point,
        DrawPoint adjacent,
        float leftOffset,
        float rightOffset,
        float thickness,
        DrawLineCap cap,
        bool start)
    {
        DrawPoint from = start ? point : adjacent;
        DrawPoint to = start ? adjacent : point;
        if (!TryDirection(from, to, out DrawPoint direction, out DrawPoint normal))
        {
            return;
        }
        if (!start)
        {
            direction = Multiply(direction, -1);
        }

        DrawPoint left = Add(point, Multiply(normal, leftOffset));
        DrawPoint right = Add(point, Multiply(normal, -rightOffset));
        float extension = thickness / 2;
        DrawPoint outward = Multiply(direction, -extension);
        switch (cap)
        {
            case DrawLineCap.Flat:
                return;
            case DrawLineCap.Square:
                mesh.AddQuad(left, right, Add(right, outward), Add(left, outward));
                return;
            case DrawLineCap.Triangle:
                mesh.AddTriangle(left, right, Add(point, outward));
                return;
            case DrawLineCap.Round:
                float startAngle = MathF.Atan2(left.Y - point.Y, left.X - point.X);
                mesh.AddFan(
                    point,
                    extension,
                    startAngle,
                    start ? MathF.PI : -MathF.PI);
                return;
        }
    }

    private static float NormalizeJoinSweep(float delta, float cross)
    {
        if (cross > 0)
        {
            while (delta < 0) delta += MathF.Tau;
            while (delta > MathF.PI) delta -= MathF.Tau;
        }
        else
        {
            while (delta > 0) delta -= MathF.Tau;
            while (delta < -MathF.PI) delta += MathF.Tau;
        }
        return delta;
    }

    private static bool TryLineIntersection(
        DrawPoint first,
        DrawPoint firstDirection,
        DrawPoint second,
        DrawPoint secondDirection,
        out DrawPoint intersection)
    {
        float denominator = Cross(firstDirection, secondDirection);
        if (MathF.Abs(denominator) <= 0.00001f)
        {
            intersection = default;
            return false;
        }
        DrawPoint difference = Subtract(second, first);
        float amount = Cross(difference, secondDirection) / denominator;
        intersection = Add(first, Multiply(firstDirection, amount));
        return true;
    }

    private static bool TryDirection(
        DrawPoint start,
        DrawPoint end,
        out DrawPoint direction,
        out DrawPoint normal)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= float.Epsilon)
        {
            direction = normal = default;
            return false;
        }
        direction = new DrawPoint(dx / length, dy / length);
        normal = new DrawPoint(-direction.Y, direction.X);
        return true;
    }

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

    private static DrawPoint Subtract(DrawPoint first, DrawPoint second) =>
        new(first.X - second.X, first.Y - second.Y);

    private static DrawPoint Multiply(DrawPoint point, float amount) =>
        new(point.X * amount, point.Y * amount);

    private static float Cross(DrawPoint first, DrawPoint second) =>
        (first.X * second.Y) - (first.Y * second.X);

    private static float Distance(DrawPoint first, DrawPoint second)
    {
        float dx = second.X - first.X;
        float dy = second.Y - first.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private sealed class MeshBuilder
    {
        private readonly List<DrawPoint> vertices = [];
        private readonly List<int> indices = [];

        public void AddTriangle(DrawPoint first, DrawPoint second, DrawPoint third)
        {
            int start = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
        }

        public void AddQuad(
            DrawPoint first,
            DrawPoint second,
            DrawPoint third,
            DrawPoint fourth)
        {
            int start = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            vertices.Add(fourth);
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }

        public void AddFan(
            DrawPoint center,
            float radius,
            float startAngle,
            float deltaAngle)
        {
            int segmentCount = Math.Max(
                2,
                (int)MathF.Ceiling(MathF.Abs(deltaAngle) / (MathF.PI / 12)));
            DrawPoint previous = new(
                center.X + (MathF.Cos(startAngle) * radius),
                center.Y + (MathF.Sin(startAngle) * radius));
            for (int index = 1; index <= segmentCount; index++)
            {
                float angle = startAngle + (deltaAngle * index / segmentCount);
                DrawPoint next = new(
                    center.X + (MathF.Cos(angle) * radius),
                    center.Y + (MathF.Sin(angle) * radius));
                AddTriangle(center, previous, next);
                previous = next;
            }
        }

        public DrawStrokeMesh Build() => new(vertices.ToArray(), indices.ToArray());
    }
}
