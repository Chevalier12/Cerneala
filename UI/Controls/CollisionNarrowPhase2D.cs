using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal static class CollisionNarrowPhase2D
{
    internal const float Epsilon = 1e-5f;
    private const int GjkIterations = 40;
    private const int EpaIterations = 48;
    private const int CastIterations = 64;

    internal static bool Intersects(ColliderGeometry2D first, ColliderGeometry2D second) =>
        TryContact(first, second, out _);

    internal static bool TryContact(
        ColliderGeometry2D first,
        ColliderGeometry2D second,
        out NarrowPhaseContact2D contact)
    {
        if (!BoundsOverlap(first.SceneBounds, second.SceneBounds))
        {
            contact = default;
            return false;
        }

        if (TryFastContact(first, second, out contact, out bool handled))
        {
            return true;
        }

        if (handled)
        {
            contact = default;
            return false;
        }

        DistanceResult distance = GetDistance(first, second);
        if (!distance.Intersects && distance.Distance > Epsilon)
        {
            contact = default;
            return false;
        }

        if (distance.Simplex.Count >= 3 &&
            TryEpa(first, second, distance.Simplex, out Vector2 normal, out Vector2 point))
        {
            contact = new NarrowPhaseContact2D(point, normal, 0, 0);
            return true;
        }

        Vector2 fallbackNormal = GetFallbackNormal(first, second, distance.Normal);
        Vector2 firstPoint = Support(first, -fallbackNormal, Vector2.Zero);
        Vector2 secondPoint = Support(second, fallbackNormal, Vector2.Zero);
        contact = new NarrowPhaseContact2D(
            (firstPoint + secondPoint) * 0.5f,
            fallbackNormal,
            0,
            0);
        return true;
    }

    internal static bool Raycast(
        ColliderGeometry2D geometry,
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        out NarrowPhaseContact2D contact)
    {
        Vector2 end = origin + (direction * maxDistance);
        if (geometry.LocalShape.Kind == ColliderShapeKind2D.Segment ||
            !Matrix3x2.Invert(geometry.ShapeToSceneTransform, out Matrix3x2 inverse))
        {
            return RaycastDegenerate(geometry, origin, end, direction, maxDistance, out contact);
        }

        Vector2 localOrigin = Vector2.Transform(origin, inverse);
        Vector2 localEnd = Vector2.Transform(end, inverse);
        Vector2 localDelta = localEnd - localOrigin;
        bool hit;
        float fraction;
        Vector2 localNormal;
        if (geometry.LocalShape.Kind == ColliderShapeKind2D.Circle)
        {
            hit = RaycastCircle(
                localOrigin,
                localDelta,
                geometry.LocalShape.Radius,
                out fraction,
                out localNormal);
        }
        else
        {
            Vector2[] vertices = GetLocalPolygonVertices(geometry.LocalShape);
            hit = RaycastPolygon(localOrigin, localDelta, vertices, out fraction, out localNormal);
        }

        if (!hit)
        {
            contact = default;
            return false;
        }

        Vector2 point = origin + ((end - origin) * fraction);
        Vector2 normal = TransformNormalFromLocal(localNormal, inverse);
        if (Vector2.Dot(normal, direction) > 0)
        {
            normal = -normal;
        }

        contact = new NarrowPhaseContact2D(
            point,
            normal,
            maxDistance * fraction,
            fraction);
        return true;
    }

    internal static bool ShapeCast(
        ColliderGeometry2D moving,
        Vector2 displacement,
        ColliderGeometry2D target,
        out NarrowPhaseContact2D contact)
    {
        float displacementLength = displacement.Length();
        if (TryContact(moving, target, out NarrowPhaseContact2D initial))
        {
            contact = initial;
            return true;
        }

        if (displacementLength <= Epsilon)
        {
            contact = default;
            return false;
        }

        float lower = 0;
        float fraction = 0;
        for (int iteration = 0; iteration < CastIterations; iteration++)
        {
            ColliderGeometry2D translated = Translate(moving, displacement * fraction);
            DistanceResult distance = GetDistance(translated, target);
            if (distance.Intersects || distance.Distance <= Epsilon)
            {
                float upper = fraction;
                for (int step = 0; step < 24 && upper - lower > Epsilon; step++)
                {
                    float middle = (lower + upper) * 0.5f;
                    DistanceResult middleDistance = GetDistance(
                        Translate(moving, displacement * middle),
                        target);
                    if (middleDistance.Intersects || middleDistance.Distance <= Epsilon)
                    {
                        upper = middle;
                    }
                    else
                    {
                        lower = middle;
                    }
                }

                fraction = upper;
                ColliderGeometry2D atImpact = Translate(moving, displacement * fraction);
                if (!TryContact(atImpact, target, out NarrowPhaseContact2D impact))
                {
                    Vector2 normal = GetFallbackNormal(atImpact, target, distance.Normal);
                    impact = new NarrowPhaseContact2D(
                        Support(target, normal, Vector2.Zero),
                        normal,
                        0,
                        0);
                }

                contact = impact with
                {
                    Distance = displacementLength * fraction,
                    Fraction = fraction
                };
                return true;
            }

            float closingSpeed = -Vector2.Dot(displacement, distance.Normal);
            if (closingSpeed <= Epsilon)
            {
                contact = default;
                return false;
            }

            float advance = MathF.Max(distance.Distance - Epsilon, Epsilon) / closingSpeed;
            if (!float.IsFinite(advance) || advance <= 0)
            {
                contact = default;
                return false;
            }

            lower = fraction;
            fraction += advance;
            if (fraction > 1 + Epsilon)
            {
                contact = default;
                return false;
            }

            fraction = MathF.Min(fraction, 1);
        }

        contact = default;
        return false;
    }

    internal static DrawRect GetSweptBounds(DrawRect bounds, Vector2 displacement)
    {
        float left = MathF.Min(bounds.X, bounds.X + displacement.X);
        float top = MathF.Min(bounds.Y, bounds.Y + displacement.Y);
        float right = MathF.Max(bounds.Right, bounds.Right + displacement.X);
        float bottom = MathF.Max(bounds.Bottom, bounds.Bottom + displacement.Y);
        return new DrawRect(left, top, right - left, bottom - top);
    }

    private static bool TryFastContact(
        ColliderGeometry2D first,
        ColliderGeometry2D second,
        out NarrowPhaseContact2D contact,
        out bool handled)
    {
        // A segment has finite endpoints but no interior. Polygon SAT assumes
        // area; the existing support-mapped distance path handles both endpoints.
        if (first.LocalShape.Kind == ColliderShapeKind2D.Segment || second.LocalShape.Kind == ColliderShapeKind2D.Segment)
        {
            handled = false;
            contact = default;
            return false;
        }
        bool firstCircle = TryGetSimilarityCircle(first, out Vector2 firstCenter, out float firstRadius);
        bool secondCircle = TryGetSimilarityCircle(second, out Vector2 secondCenter, out float secondRadius);
        if (firstCircle && secondCircle)
        {
            handled = true;
            Vector2 delta = firstCenter - secondCenter;
            float distance = delta.Length();
            float radii = firstRadius + secondRadius;
            if (distance > radii + Epsilon)
            {
                contact = default;
                return false;
            }

            Vector2 normal = distance > Epsilon ? delta / distance : Vector2.UnitX;
            Vector2 firstPoint = firstCenter - (normal * firstRadius);
            Vector2 secondPoint = secondCenter + (normal * secondRadius);
            contact = new NarrowPhaseContact2D(
                (firstPoint + secondPoint) * 0.5f,
                normal,
                0,
                0);
            return true;
        }

        bool firstPolygon = first.LocalShape.Kind != ColliderShapeKind2D.Circle;
        bool secondPolygon = second.LocalShape.Kind != ColliderShapeKind2D.Circle;
        if (firstPolygon && secondPolygon)
        {
            handled = true;
            return TryPolygonContact(
                GetWorldPolygonVertices(first),
                GetWorldPolygonVertices(second),
                out contact);
        }

        if (firstCircle && secondPolygon)
        {
            handled = true;
            return TryCirclePolygonContact(
                firstCenter,
                firstRadius,
                GetWorldPolygonVertices(second),
                circleIsFirst: true,
                out contact);
        }

        if (secondCircle && firstPolygon)
        {
            handled = true;
            return TryCirclePolygonContact(
                secondCenter,
                secondRadius,
                GetWorldPolygonVertices(first),
                circleIsFirst: false,
                out contact);
        }

        handled = false;
        contact = default;
        return false;
    }

    private static bool TryPolygonContact(
        Vector2[] first,
        Vector2[] second,
        out NarrowPhaseContact2D contact)
    {
        Vector2 firstCenter = GetCentroid(first);
        Vector2 secondCenter = GetCentroid(second);
        float minimumOverlap = float.PositiveInfinity;
        Vector2 minimumAxis = Vector2.UnitX;
        if (!TestPolygonAxes(first, second, firstCenter, secondCenter, ref minimumOverlap, ref minimumAxis) ||
            !TestPolygonAxes(second, first, firstCenter, secondCenter, ref minimumOverlap, ref minimumAxis))
        {
            contact = default;
            return false;
        }

        Vector2 firstPoint = GetSupport(first, -minimumAxis);
        Vector2 secondPoint = GetSupport(second, minimumAxis);
        contact = new NarrowPhaseContact2D(
            (firstPoint + secondPoint) * 0.5f,
            minimumAxis,
            0,
            0);
        return true;
    }

    private static bool TestPolygonAxes(
        Vector2[] axisSource,
        Vector2[] other,
        Vector2 firstCenter,
        Vector2 secondCenter,
        ref float minimumOverlap,
        ref Vector2 minimumAxis)
    {
        for (int index = 0; index < axisSource.Length; index++)
        {
            Vector2 edge = axisSource[(index + 1) % axisSource.Length] - axisSource[index];
            if (edge.LengthSquared() <= Epsilon * Epsilon)
            {
                continue;
            }

            Vector2 axis = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            Project(axisSource, axis, out float sourceMin, out float sourceMax);
            Project(other, axis, out float otherMin, out float otherMax);
            float overlap = MathF.Min(sourceMax, otherMax) - MathF.Max(sourceMin, otherMin);
            if (overlap < -Epsilon)
            {
                return false;
            }

            if (Vector2.Dot(firstCenter - secondCenter, axis) < 0)
            {
                axis = -axis;
            }

            if (IsPreferredAxis(overlap, axis, minimumOverlap, minimumAxis))
            {
                minimumOverlap = MathF.Max(0, overlap);
                minimumAxis = axis;
            }
        }

        return true;
    }

    private static bool TryCirclePolygonContact(
        Vector2 circleCenter,
        float radius,
        Vector2[] polygon,
        bool circleIsFirst,
        out NarrowPhaseContact2D contact)
    {
        Vector2 polygonCenter = GetCentroid(polygon);
        float minimumOverlap = float.PositiveInfinity;
        Vector2 minimumAxis = Vector2.UnitX;
        for (int index = 0; index < polygon.Length; index++)
        {
            Vector2 edge = polygon[(index + 1) % polygon.Length] - polygon[index];
            if (edge.LengthSquared() <= Epsilon * Epsilon)
            {
                continue;
            }

            if (!TestCircleAxis(circleCenter, radius, polygon, edge, ref minimumOverlap, ref minimumAxis))
            {
                contact = default;
                return false;
            }
        }

        Vector2 closest = polygon[0];
        float closestSquared = Vector2.DistanceSquared(circleCenter, closest);
        for (int index = 1; index < polygon.Length; index++)
        {
            float candidate = Vector2.DistanceSquared(circleCenter, polygon[index]);
            if (candidate < closestSquared)
            {
                closestSquared = candidate;
                closest = polygon[index];
            }
        }

        Vector2 vertexAxis = circleCenter - closest;
        if (vertexAxis.LengthSquared() > Epsilon * Epsilon &&
            !TestCircleAxis(circleCenter, radius, polygon, new Vector2(-vertexAxis.Y, vertexAxis.X), ref minimumOverlap, ref minimumAxis, vertexAxis))
        {
            contact = default;
            return false;
        }

        Vector2 desired = circleIsFirst
            ? circleCenter - polygonCenter
            : polygonCenter - circleCenter;
        if (Vector2.Dot(minimumAxis, desired) < 0)
        {
            minimumAxis = -minimumAxis;
        }

        Vector2 circleNormal = circleIsFirst ? minimumAxis : -minimumAxis;
        Vector2 circlePoint = circleCenter - (circleNormal * radius);
        Vector2 polygonPoint = GetSupport(polygon, circleNormal);
        contact = new NarrowPhaseContact2D(
            (circlePoint + polygonPoint) * 0.5f,
            minimumAxis,
            0,
            0);
        return true;
    }

    private static bool TestCircleAxis(
        Vector2 center,
        float radius,
        Vector2[] polygon,
        Vector2 edge,
        ref float minimumOverlap,
        ref Vector2 minimumAxis,
        Vector2? explicitAxis = null)
    {
        Vector2 axis = explicitAxis ?? new Vector2(edge.Y, -edge.X);
        if (axis.LengthSquared() <= Epsilon * Epsilon)
        {
            return true;
        }

        axis = Vector2.Normalize(axis);
        Project(polygon, axis, out float polygonMin, out float polygonMax);
        float circleProjection = Vector2.Dot(center, axis);
        float overlap = MathF.Min(polygonMax, circleProjection + radius) -
            MathF.Max(polygonMin, circleProjection - radius);
        if (overlap < -Epsilon)
        {
            return false;
        }

        if (IsPreferredAxis(overlap, axis, minimumOverlap, minimumAxis))
        {
            minimumOverlap = MathF.Max(0, overlap);
            minimumAxis = axis;
        }

        return true;
    }

    private static bool IsPreferredAxis(
        float overlap,
        Vector2 axis,
        float currentOverlap,
        Vector2 currentAxis)
    {
        if (overlap < currentOverlap - Epsilon)
        {
            return true;
        }

        return MathF.Abs(overlap - currentOverlap) <= Epsilon &&
            MathF.Abs(axis.X) > MathF.Abs(currentAxis.X) + Epsilon;
    }

    private static DistanceResult GetDistance(
        ColliderGeometry2D first,
        ColliderGeometry2D second)
    {
        List<SupportVertex> simplex = new(3);
        Vector2 direction = GetCenter(first) - GetCenter(second);
        if (direction.LengthSquared() <= Epsilon * Epsilon)
        {
            direction = Vector2.UnitX;
        }

        simplex.Add(GetMinkowskiSupport(first, second, direction));
        Vector2 closest = simplex[0].Point;
        Vector2 witnessFirst = simplex[0].First;
        Vector2 witnessSecond = simplex[0].Second;
        for (int iteration = 0; iteration < GjkIterations; iteration++)
        {
            bool containsOrigin = ReduceSimplex(
                simplex,
                out closest,
                out witnessFirst,
                out witnessSecond);
            float distanceSquared = closest.LengthSquared();
            if (containsOrigin || distanceSquared <= Epsilon * Epsilon)
            {
                return new DistanceResult(
                    true,
                    0,
                    GetFallbackNormal(first, second, Vector2.Zero),
                    witnessFirst,
                    witnessSecond,
                    simplex);
            }

            direction = -closest;
            SupportVertex support = GetMinkowskiSupport(first, second, direction);
            float improvement = Vector2.Dot(support.Point, direction) - Vector2.Dot(closest, direction);
            if (improvement <= Epsilon * MathF.Max(1, direction.Length()) ||
                simplex.Any(vertex => Vector2.DistanceSquared(vertex.Point, support.Point) <= Epsilon * Epsilon))
            {
                float distance = MathF.Sqrt(distanceSquared);
                return new DistanceResult(
                    false,
                    distance,
                    closest / distance,
                    witnessFirst,
                    witnessSecond,
                    simplex);
            }

            simplex.Add(support);
        }

        float fallbackDistance = closest.Length();
        return new DistanceResult(
            fallbackDistance <= Epsilon,
            fallbackDistance,
            fallbackDistance > Epsilon ? closest / fallbackDistance : GetFallbackNormal(first, second, Vector2.Zero),
            witnessFirst,
            witnessSecond,
            simplex);
    }

    private static bool ReduceSimplex(
        List<SupportVertex> simplex,
        out Vector2 closest,
        out Vector2 witnessFirst,
        out Vector2 witnessSecond)
    {
        if (simplex.Count == 1)
        {
            SupportVertex only = simplex[0];
            closest = only.Point;
            witnessFirst = only.First;
            witnessSecond = only.Second;
            return false;
        }

        if (simplex.Count == 2)
        {
            ClosestOnSegment(simplex[0], simplex[1], out float weight, out closest, out witnessFirst, out witnessSecond);
            if (weight <= Epsilon)
            {
                simplex.RemoveAt(1);
            }
            else if (weight >= 1 - Epsilon)
            {
                simplex.RemoveAt(0);
            }

            return false;
        }

        SupportVertex a = simplex[0];
        SupportVertex b = simplex[1];
        SupportVertex c = simplex[2];
        Vector2 ab = b.Point - a.Point;
        Vector2 ac = c.Point - a.Point;
        Vector2 ap = -a.Point;
        float d1 = Vector2.Dot(ab, ap);
        float d2 = Vector2.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0)
        {
            simplex.Clear();
            simplex.Add(a);
            closest = a.Point;
            witnessFirst = a.First;
            witnessSecond = a.Second;
            return false;
        }

        Vector2 bp = -b.Point;
        float d3 = Vector2.Dot(ab, bp);
        float d4 = Vector2.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3)
        {
            simplex.Clear();
            simplex.Add(b);
            closest = b.Point;
            witnessFirst = b.First;
            witnessSecond = b.Second;
            return false;
        }

        float vc = (d1 * d4) - (d3 * d2);
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            float weight = d1 / (d1 - d3);
            SetSegment(simplex, a, b, weight, out closest, out witnessFirst, out witnessSecond);
            return false;
        }

        Vector2 cp = -c.Point;
        float d5 = Vector2.Dot(ab, cp);
        float d6 = Vector2.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6)
        {
            simplex.Clear();
            simplex.Add(c);
            closest = c.Point;
            witnessFirst = c.First;
            witnessSecond = c.Second;
            return false;
        }

        float vb = (d5 * d2) - (d1 * d6);
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            float weight = d2 / (d2 - d6);
            SetSegment(simplex, a, c, weight, out closest, out witnessFirst, out witnessSecond);
            return false;
        }

        float va = (d3 * d6) - (d5 * d4);
        float d43 = d4 - d3;
        float d56 = d5 - d6;
        if (va <= 0 && d43 >= 0 && d56 >= 0)
        {
            float weight = d43 / (d43 + d56);
            SetSegment(simplex, b, c, weight, out closest, out witnessFirst, out witnessSecond);
            return false;
        }

        float denominator = 1 / (va + vb + vc);
        float v = vb * denominator;
        float w = vc * denominator;
        float u = 1 - v - w;
        closest = Vector2.Zero;
        witnessFirst = (a.First * u) + (b.First * v) + (c.First * w);
        witnessSecond = (a.Second * u) + (b.Second * v) + (c.Second * w);
        return true;
    }

    private static void ClosestOnSegment(
        SupportVertex first,
        SupportVertex second,
        out float weight,
        out Vector2 closest,
        out Vector2 witnessFirst,
        out Vector2 witnessSecond)
    {
        Vector2 segment = second.Point - first.Point;
        float lengthSquared = segment.LengthSquared();
        weight = lengthSquared <= Epsilon * Epsilon
            ? 0
            : Math.Clamp(-Vector2.Dot(first.Point, segment) / lengthSquared, 0, 1);
        closest = Vector2.Lerp(first.Point, second.Point, weight);
        witnessFirst = Vector2.Lerp(first.First, second.First, weight);
        witnessSecond = Vector2.Lerp(first.Second, second.Second, weight);
    }

    private static void SetSegment(
        List<SupportVertex> simplex,
        SupportVertex first,
        SupportVertex second,
        float weight,
        out Vector2 closest,
        out Vector2 witnessFirst,
        out Vector2 witnessSecond)
    {
        simplex.Clear();
        simplex.Add(first);
        simplex.Add(second);
        closest = Vector2.Lerp(first.Point, second.Point, weight);
        witnessFirst = Vector2.Lerp(first.First, second.First, weight);
        witnessSecond = Vector2.Lerp(first.Second, second.Second, weight);
    }

    private static bool TryEpa(
        ColliderGeometry2D first,
        ColliderGeometry2D second,
        IReadOnlyList<SupportVertex> simplex,
        out Vector2 normal,
        out Vector2 point)
    {
        List<SupportVertex> polygon = [simplex[0], simplex[1], simplex[2]];
        if (Cross(polygon[1].Point - polygon[0].Point, polygon[2].Point - polygon[0].Point) < 0)
        {
            (polygon[1], polygon[2]) = (polygon[2], polygon[1]);
        }

        for (int iteration = 0; iteration < EpaIterations; iteration++)
        {
            float minimumDistance = float.PositiveInfinity;
            int insertionIndex = 0;
            Vector2 edgeNormal = Vector2.UnitX;
            for (int index = 0; index < polygon.Count; index++)
            {
                int next = (index + 1) % polygon.Count;
                Vector2 edge = polygon[next].Point - polygon[index].Point;
                if (edge.LengthSquared() <= Epsilon * Epsilon)
                {
                    continue;
                }

                Vector2 candidate = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
                float candidateDistance = Vector2.Dot(candidate, polygon[index].Point);
                if (candidateDistance < 0)
                {
                    candidate = -candidate;
                    candidateDistance = -candidateDistance;
                }

                if (candidateDistance < minimumDistance)
                {
                    minimumDistance = candidateDistance;
                    edgeNormal = candidate;
                    insertionIndex = next;
                }
            }

            SupportVertex support = GetMinkowskiSupport(first, second, edgeNormal);
            float supportDistance = Vector2.Dot(edgeNormal, support.Point);
            if (supportDistance - minimumDistance <= Epsilon)
            {
                int previous = insertionIndex == 0 ? polygon.Count - 1 : insertionIndex - 1;
                ClosestOnSegment(
                    polygon[previous],
                    polygon[insertionIndex],
                    out _,
                    out _,
                    out Vector2 witnessFirst,
                    out Vector2 witnessSecond);
                normal = edgeNormal;
                point = (witnessFirst + witnessSecond) * 0.5f;
                return true;
            }

            if (polygon.Any(vertex => Vector2.DistanceSquared(vertex.Point, support.Point) <= Epsilon * Epsilon))
            {
                break;
            }

            polygon.Insert(insertionIndex, support);
        }

        normal = default;
        point = default;
        return false;
    }

    private static SupportVertex GetMinkowskiSupport(
        ColliderGeometry2D first,
        ColliderGeometry2D second,
        Vector2 direction)
    {
        Vector2 firstPoint = Support(first, direction, Vector2.Zero);
        Vector2 secondPoint = Support(second, -direction, Vector2.Zero);
        return new SupportVertex(firstPoint - secondPoint, firstPoint, secondPoint);
    }

    private static Vector2 Support(
        ColliderGeometry2D geometry,
        Vector2 worldDirection,
        Vector2 translation)
    {
        Matrix3x2 transform = geometry.ShapeToSceneTransform;
        Vector2 localDirection = new(
            (worldDirection.X * transform.M11) + (worldDirection.Y * transform.M12),
            (worldDirection.X * transform.M21) + (worldDirection.Y * transform.M22));
        Vector2 local;
        switch (geometry.LocalShape.Kind)
        {
            case ColliderShapeKind2D.Box:
                local = new Vector2(
                    localDirection.X >= 0 ? geometry.LocalShape.Width : 0,
                    localDirection.Y >= 0 ? geometry.LocalShape.Height : 0);
                break;
            case ColliderShapeKind2D.Circle:
                local = localDirection.LengthSquared() <= Epsilon * Epsilon
                    ? new Vector2(geometry.LocalShape.Radius, 0)
                    : Vector2.Normalize(localDirection) * geometry.LocalShape.Radius;
                break;
            case ColliderShapeKind2D.Polygon:
            case ColliderShapeKind2D.Segment:
                local = GetSupport(geometry.LocalShape.Vertices, localDirection);
                break;
            default:
                local = default;
                break;
        }

        return Vector2.Transform(local, transform) + translation;
    }

    private static bool TryGetSimilarityCircle(
        ColliderGeometry2D geometry,
        out Vector2 center,
        out float radius)
    {
        if (geometry.LocalShape.Kind != ColliderShapeKind2D.Circle)
        {
            center = default;
            radius = 0;
            return false;
        }

        Matrix3x2 transform = geometry.ShapeToSceneTransform;
        Vector2 xBasis = new(transform.M11, transform.M12);
        Vector2 yBasis = new(transform.M21, transform.M22);
        float xLength = xBasis.Length();
        float yLength = yBasis.Length();
        float scale = MathF.Max(1, MathF.Max(xLength, yLength));
        if (xLength <= Epsilon ||
            yLength <= Epsilon ||
            MathF.Abs(xLength - yLength) > Epsilon * scale ||
            MathF.Abs(Vector2.Dot(xBasis, yBasis)) > Epsilon * scale * scale)
        {
            center = default;
            radius = 0;
            return false;
        }

        center = Vector2.Transform(Vector2.Zero, transform);
        radius = geometry.LocalShape.Radius * ((xLength + yLength) * 0.5f);
        return true;
    }

    private static Vector2[] GetWorldPolygonVertices(ColliderGeometry2D geometry)
    {
        Vector2[] local = GetLocalPolygonVertices(geometry.LocalShape);
        for (int index = 0; index < local.Length; index++)
        {
            local[index] = Vector2.Transform(local[index], geometry.ShapeToSceneTransform);
        }

        return local;
    }

    private static Vector2[] GetLocalPolygonVertices(ColliderLocalShape2D shape) =>
        shape.Kind == ColliderShapeKind2D.Box
            ?
            [
                Vector2.Zero,
                new Vector2(shape.Width, 0),
                new Vector2(shape.Width, shape.Height),
                new Vector2(0, shape.Height)
            ]
            : shape.Vertices.ToArray();

    private static bool RaycastCircle(
        Vector2 origin,
        Vector2 delta,
        float radius,
        out float fraction,
        out Vector2 normal)
    {
        float originSquared = origin.LengthSquared();
        if (originSquared <= (radius + Epsilon) * (radius + Epsilon))
        {
            fraction = 0;
            normal = originSquared > Epsilon * Epsilon
                ? Vector2.Normalize(origin)
                : delta.LengthSquared() > Epsilon * Epsilon ? -Vector2.Normalize(delta) : Vector2.UnitX;
            return true;
        }

        float a = delta.LengthSquared();
        if (a <= Epsilon * Epsilon)
        {
            fraction = 0;
            normal = default;
            return false;
        }

        float b = Vector2.Dot(origin, delta);
        float c = originSquared - (radius * radius);
        float discriminant = (b * b) - (a * c);
        if (discriminant < -Epsilon)
        {
            fraction = 0;
            normal = default;
            return false;
        }

        float candidate = (-b - MathF.Sqrt(MathF.Max(0, discriminant))) / a;
        if (candidate < -Epsilon || candidate > 1 + Epsilon)
        {
            fraction = 0;
            normal = default;
            return false;
        }

        fraction = Math.Clamp(candidate, 0, 1);
        normal = Vector2.Normalize(origin + (delta * fraction));
        return true;
    }

    private static bool RaycastPolygon(
        Vector2 origin,
        Vector2 delta,
        Vector2[] vertices,
        out float fraction,
        out Vector2 normal)
    {
        float winding = SignedArea(vertices);
        float lower = 0;
        float upper = 1;
        Vector2 entryNormal = delta.LengthSquared() > Epsilon * Epsilon
            ? -Vector2.Normalize(delta)
            : Vector2.UnitX;
        for (int index = 0; index < vertices.Length; index++)
        {
            Vector2 edge = vertices[(index + 1) % vertices.Length] - vertices[index];
            if (edge.LengthSquared() <= Epsilon * Epsilon)
            {
                continue;
            }

            Vector2 outward = winding >= 0
                ? Vector2.Normalize(new Vector2(edge.Y, -edge.X))
                : Vector2.Normalize(new Vector2(-edge.Y, edge.X));
            float numerator = Vector2.Dot(outward, vertices[index] - origin);
            float denominator = Vector2.Dot(outward, delta);
            if (MathF.Abs(denominator) <= Epsilon)
            {
                if (numerator < -Epsilon)
                {
                    fraction = 0;
                    normal = default;
                    return false;
                }

                continue;
            }

            float candidate = numerator / denominator;
            if (denominator < 0)
            {
                if (candidate > lower)
                {
                    lower = candidate;
                    entryNormal = outward;
                }
            }
            else
            {
                upper = MathF.Min(upper, candidate);
            }

            if (lower - upper > Epsilon)
            {
                fraction = 0;
                normal = default;
                return false;
            }
        }

        if (lower < -Epsilon || lower > 1 + Epsilon)
        {
            fraction = 0;
            normal = default;
            return false;
        }

        fraction = Math.Clamp(lower, 0, 1);
        normal = entryNormal;
        return true;
    }

    private static bool RaycastDegenerate(
        ColliderGeometry2D geometry,
        Vector2 origin,
        Vector2 end,
        Vector2 direction,
        float maxDistance,
        out NarrowPhaseContact2D contact)
    {
        Vector2[] points = geometry.LocalShape.Kind == ColliderShapeKind2D.Circle
            ? GetDegenerateCircleSegment(geometry)
            : GetWorldPolygonVertices(geometry);
        float best = float.PositiveInfinity;
        Vector2 bestPoint = default;
        Vector2 bestNormal = default;
        int edgeCount = points.Length == 2 ? 1 : points.Length;
        for (int index = 0; index < edgeCount; index++)
        {
            Vector2 a = points[index];
            Vector2 b = points[(index + 1) % points.Length];
            if (TrySegmentIntersection(origin, end, a, b, out float fraction) && fraction < best)
            {
                best = fraction;
                bestPoint = Vector2.Lerp(origin, end, fraction);
                Vector2 edge = b - a;
                bestNormal = edge.LengthSquared() > Epsilon * Epsilon
                    ? Vector2.Normalize(new Vector2(edge.Y, -edge.X))
                    : -direction;
                if (Vector2.Dot(bestNormal, direction) > 0)
                {
                    bestNormal = -bestNormal;
                }
            }
        }

        if (!float.IsFinite(best))
        {
            contact = default;
            return false;
        }

        contact = new NarrowPhaseContact2D(bestPoint, bestNormal, maxDistance * best, best);
        return true;
    }

    private static Vector2[] GetDegenerateCircleSegment(ColliderGeometry2D geometry)
    {
        Matrix3x2 transform = geometry.ShapeToSceneTransform;
        Vector2 center = Vector2.Transform(Vector2.Zero, transform);
        Vector2 firstBasis = new(transform.M11, transform.M12);
        Vector2 secondBasis = new(transform.M21, transform.M22);
        Vector2 direction = firstBasis.LengthSquared() >= secondBasis.LengthSquared()
            ? firstBasis
            : secondBasis;
        if (direction.LengthSquared() <= Epsilon * Epsilon)
        {
            return [center, center];
        }

        direction = Vector2.Normalize(direction);
        Vector2 localDirection = new(
            (direction.X * transform.M11) + (direction.Y * transform.M12),
            (direction.X * transform.M21) + (direction.Y * transform.M22));
        float extent = geometry.LocalShape.Radius * localDirection.Length();
        return [center - (direction * extent), center + (direction * extent)];
    }

    private static bool TrySegmentIntersection(
        Vector2 p,
        Vector2 p2,
        Vector2 q,
        Vector2 q2,
        out float fraction)
    {
        Vector2 r = p2 - p;
        Vector2 s = q2 - q;
        float denominator = Cross(r, s);
        Vector2 delta = q - p;
        if (MathF.Abs(denominator) <= Epsilon)
        {
            if (MathF.Abs(Cross(delta, r)) > Epsilon || r.LengthSquared() <= Epsilon * Epsilon)
            {
                fraction = 0;
                return Vector2.DistanceSquared(p, q) <= Epsilon * Epsilon;
            }

            float first = Vector2.Dot(delta, r) / r.LengthSquared();
            float second = first + (Vector2.Dot(s, r) / r.LengthSquared());
            float entry = MathF.Max(0, MathF.Min(first, second));
            float exit = MathF.Min(1, MathF.Max(first, second));
            fraction = entry;
            return entry <= exit + Epsilon;
        }

        float t = Cross(delta, s) / denominator;
        float u = Cross(delta, r) / denominator;
        fraction = Math.Clamp(t, 0, 1);
        return t >= -Epsilon && t <= 1 + Epsilon && u >= -Epsilon && u <= 1 + Epsilon;
    }

    private static ColliderGeometry2D Translate(ColliderGeometry2D geometry, Vector2 translation)
    {
        DrawRect bounds = geometry.SceneBounds;
        return geometry with
        {
            ShapeToSceneTransform = geometry.ShapeToSceneTransform * Matrix3x2.CreateTranslation(translation),
            SceneBounds = new DrawRect(bounds.X + translation.X, bounds.Y + translation.Y, bounds.Width, bounds.Height)
        };
    }

    private static Vector2 TransformNormalFromLocal(Vector2 local, Matrix3x2 inverse)
    {
        Vector2 transformed = new(
            (local.X * inverse.M11) + (local.Y * inverse.M12),
            (local.X * inverse.M21) + (local.Y * inverse.M22));
        return transformed.LengthSquared() > Epsilon * Epsilon
            ? Vector2.Normalize(transformed)
            : Vector2.UnitX;
    }

    private static Vector2 GetCenter(ColliderGeometry2D geometry)
    {
        Vector2 local = geometry.LocalShape.Kind switch
        {
            ColliderShapeKind2D.Box => new Vector2(geometry.LocalShape.Width * 0.5f, geometry.LocalShape.Height * 0.5f),
            ColliderShapeKind2D.Circle => Vector2.Zero,
            ColliderShapeKind2D.Polygon => GetCentroid(geometry.LocalShape.Vertices),
            ColliderShapeKind2D.Segment => GetCentroid(geometry.LocalShape.Vertices),
            _ => Vector2.Zero
        };
        return Vector2.Transform(local, geometry.ShapeToSceneTransform);
    }

    private static Vector2 GetFallbackNormal(
        ColliderGeometry2D first,
        ColliderGeometry2D second,
        Vector2 candidate)
    {
        if (candidate.LengthSquared() > Epsilon * Epsilon)
        {
            return Vector2.Normalize(candidate);
        }

        Vector2 centers = GetCenter(first) - GetCenter(second);
        if (MathF.Abs(centers.X) > Epsilon)
        {
            return new Vector2(MathF.Sign(centers.X), 0);
        }

        if (MathF.Abs(centers.Y) > Epsilon)
        {
            return new Vector2(0, MathF.Sign(centers.Y));
        }

        return Vector2.UnitX;
    }

    private static void Project(Vector2[] vertices, Vector2 axis, out float minimum, out float maximum)
    {
        minimum = Vector2.Dot(vertices[0], axis);
        maximum = minimum;
        for (int index = 1; index < vertices.Length; index++)
        {
            float projection = Vector2.Dot(vertices[index], axis);
            minimum = MathF.Min(minimum, projection);
            maximum = MathF.Max(maximum, projection);
        }
    }

    private static Vector2 GetSupport(IReadOnlyList<Vector2> vertices, Vector2 direction)
    {
        Vector2 result = vertices[0];
        float maximum = Vector2.Dot(result, direction);
        for (int index = 1; index < vertices.Count; index++)
        {
            float projection = Vector2.Dot(vertices[index], direction);
            if (projection > maximum)
            {
                maximum = projection;
                result = vertices[index];
            }
        }

        return result;
    }

    private static Vector2 GetCentroid(IReadOnlyList<Vector2> vertices)
    {
        Vector2 center = Vector2.Zero;
        foreach (Vector2 vertex in vertices)
        {
            center += vertex;
        }

        return center / vertices.Count;
    }

    private static float SignedArea(IReadOnlyList<Vector2> vertices)
    {
        float area = 0;
        for (int index = 0; index < vertices.Count; index++)
        {
            area += Cross(vertices[index], vertices[(index + 1) % vertices.Count]);
        }

        return area * 0.5f;
    }

    private static bool BoundsOverlap(DrawRect a, DrawRect b) =>
        a.X <= b.Right + Epsilon &&
        a.Right + Epsilon >= b.X &&
        a.Y <= b.Bottom + Epsilon &&
        a.Bottom + Epsilon >= b.Y;

    private static float Cross(Vector2 first, Vector2 second) =>
        (first.X * second.Y) - (first.Y * second.X);

    private readonly record struct SupportVertex(
        Vector2 Point,
        Vector2 First,
        Vector2 Second);

    private readonly record struct DistanceResult(
        bool Intersects,
        float Distance,
        Vector2 Normal,
        Vector2 FirstPoint,
        Vector2 SecondPoint,
        List<SupportVertex> Simplex);
}

internal readonly record struct NarrowPhaseContact2D(
    Vector2 Point,
    Vector2 Normal,
    float Distance,
    float Fraction);
