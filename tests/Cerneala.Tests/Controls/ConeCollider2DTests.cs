using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

// Contract for ConeCollider2D, the shape of a Vision (docs/plans/2026-10-25-scene2d-vision.md): which points,
// circles, boxes and polygons a cone touches, its bounds, and where a cone may be owned.
// Direction 0 is +X; Direction grows clockwise on screen (+Y is down): 90 looks down.
// A point at angle phi from the apex is inside when its distance is at most Radius and
// the angular distance between phi and Direction is at most Angle / 2.
public sealed class ConeCollider2DTests
{
    private const float Epsilon = CollisionNarrowPhase2D.Epsilon;

    private static readonly float[] ConeAngles = [30, 90, 180, 270, 300, 360];

    private static readonly float[] PointAngles =
        [0, 13, -13, 17, -17, 43, -43, 47, -47, 88, -88, 92, -92, 133, -133, 137, -137, 148, -148, 152, -152, 180];

    private static readonly string[] ShapeNames = ["Dot", "Circle", "Box", "Polygon"];

    public static TheoryData<float, float, float, bool> PointSetCases()
    {
        TheoryData<float, float, float, bool> data = new();
        foreach (float angle in ConeAngles)
        {
            foreach (float phi in PointAngles)
            {
                // Inside exactly when |phi| < Angle / 2. A 360 degree cone is a full circle.
                data.Add(angle, phi, 60, angle == 360 || MathF.Abs(phi) < angle / 2);
            }

            data.Add(angle, 0, 99, true);
            data.Add(angle, 0, 101, false);
        }

        return data;
    }

    public static TheoryData<string, float, float, bool> NinetyDegreePlacements()
    {
        // Cone: Radius 100, Angle 90, Direction 0, so it covers |y| <= x up to distance 100.
        (float X, float Y, bool Expected)[] placements =
        [
            (50, 0, true),      // in front
            (100, 0, true),     // on the arc
            (50, 50, true),     // on the 45 degree ray
            (-50, 0, false),    // behind the apex
            (150, 0, false),    // beyond the radius
            (0, 60, false)      // to the side, outside the 45 degree ray
        ];
        TheoryData<string, float, float, bool> data = new();
        foreach (string shape in ShapeNames)
        {
            foreach ((float x, float y, bool expected) in placements)
            {
                data.Add(shape, x, y, expected);
            }
        }

        return data;
    }

    public static TheoryData<string, float, float, bool> FullCirclePlacements()
    {
        (float X, float Y, bool Expected)[] placements =
        [
            (0, 0, true),
            (50, 0, true),
            (-50, 0, true),
            (0, 60, true),
            (0, -60, true),
            (100, 0, true),
            (-100, 0, true),
            (150, 0, false),
            (-150, 0, false),
            (0, 150, false)
        ];
        TheoryData<string, float, float, bool> data = new();
        foreach (string shape in ShapeNames)
        {
            foreach ((float x, float y, bool expected) in placements)
            {
                data.Add(shape, x, y, expected);
            }
        }

        return data;
    }

    [Fact]
    public void DefaultsAreRadius100Angle90AndDirection0()
    {
        ConeCollider2D cone = new();

        Assert.Equal(100f, cone.Radius);
        Assert.Equal(90f, cone.Angle);
        Assert.Equal(0f, cone.Direction);
        Assert.IsAssignableFrom<Collider2D>(cone);
        Assert.True(typeof(ConeCollider2D).IsSealed);
    }

    [Fact]
    public void AConeCoversAnAngleAroundItsDirectionAndTheLocalShapeIsInRadians()
    {
        ColliderLocalShape2D factory = ColliderLocalShape2D.Cone(100, 0.25f, 1.5f);
        Assert.Equal(ColliderShapeKind2D.Cone, factory.Kind);
        Assert.Equal(100f, factory.Radius);
        Assert.Equal(0.25f, factory.StartAngle);
        Assert.Equal(1.5f, factory.Sweep);

        ColliderLocalShape2D shape = Cone(100, 90, 30).GetLocalShape();
        Assert.Equal(ColliderShapeKind2D.Cone, shape.Kind);
        Assert.Equal(100f, shape.Radius);
        Assert.Equal(MathF.PI / 2, shape.Sweep, 1e-4f);
        // The start edge plus half the sweep is the direction: 30 degrees.
        Assert.Equal(30 * MathF.PI / 180, WrapRadians(shape.StartAngle + (shape.Sweep / 2)), 1e-4f);
    }

    [Theory]
    [MemberData(nameof(NinetyDegreePlacements))]
    public void ShapesInFrontOfTheConeIntersectAndShapesBehindOrBeyondItDoNot(
        string shape, float x, float y, bool expected)
    {
        ConeCollider2D cone = Cone(100, 90, 0);

        Assert.Equal(expected, Intersects(cone, Shape(shape, new Vector2(x, y))));
    }

    [Theory]
    [InlineData("X", -0.5f, true)] // the box overlaps the +X ray
    [InlineData("X", 0f, true)] // the box edge lies on the ray
    [InlineData("X", Epsilon / 2, true)] // a gap smaller than Epsilon still counts as touching
    [InlineData("X", Epsilon * 2, false)] // a gap larger than Epsilon does not
    [InlineData("Y", -0.5f, true)]
    [InlineData("Y", 0f, true)]
    [InlineData("Y", Epsilon / 2, true)]
    [InlineData("Y", Epsilon * 2, false)]
    public void ASideRayTouchIsAContactWithinEpsilon(string ray, float gap, bool expected)
    {
        // Direction 45 with Angle 90: the rays are the +X axis (phi 0) and the +Y axis (phi 90),
        // so the cone covers the quadrant x >= 0, y >= 0 up to distance 100.
        ConeCollider2D cone = Cone(100, 90, 45);
        BoxCollider2D box = ray == "X"
            ? new BoxCollider2D { Width = 10, Height = 1, TranslateX = 10, TranslateY = -1 - gap } // x 10..20, bottom edge at y = -gap
            : new BoxCollider2D { Width = 1, Height = 10, TranslateX = -1 - gap, TranslateY = 10 }; // y 10..20, right edge at x = -gap

        Assert.Equal(expected, Intersects(cone, box));
    }

    [Theory]
    [MemberData(nameof(PointSetCases))]
    public void APointIsInsideTheConeExactlyWhenItsAngleIsWithinHalfTheConeAngle(
        float angle, float phi, float distance, bool expected)
    {
        ConeCollider2D cone = Cone(100, angle, 0);
        Vector2 point = Polar(phi, distance);

        Assert.Equal(expected, SceneGeometry2D.ContainsPoint(Geometry(cone), point));
        Assert.Equal(expected, Intersects(cone, Dot(point)));
    }

    [Theory]
    [InlineData(270f)]
    [InlineData(300f)]
    public void AConeWiderThan180IsHitThroughBothSidesOfItsNotchButNotInsideTheNotch(float angle)
    {
        // The notch is centred on phi 180. The box crosses it at x -35..-25 but reaches y -80 and y 80,
        // which are outside the notch for both angles.
        ConeCollider2D cone = Cone(100, angle, 0);
        BoxCollider2D bridge = new() { Width = 10, Height = 160, TranslateX = -35, TranslateY = -80 };
        BoxCollider2D insideTheNotch = new() { Width = 10, Height = 10, TranslateX = -55, TranslateY = -5 };

        Assert.True(Intersects(cone, bridge));
        Assert.False(Intersects(cone, insideTheNotch));
    }

    [Theory]
    [MemberData(nameof(FullCirclePlacements))]
    public void ThreeHundredSixtyDegreesGivesTheSameResultAsACircleOfTheSameRadius(
        string shape, float x, float y, bool expected)
    {
        ConeCollider2D cone = Cone(100, 360, 0);
        CircleCollider2D circle = new() { Radius = 100 };

        Assert.Equal(expected, Intersects(circle, Shape(shape, new Vector2(x, y))));
        Assert.Equal(expected, Intersects(cone, Shape(shape, new Vector2(x, y))));
    }

    [Theory]
    [InlineData(90f, 90f, 0f)]
    [InlineData(180f, 180f, 0f)]
    [InlineData(-90f, -90f, 90f)]
    [InlineData(450f, 90f, 0f)] // 450 behaves exactly like 90
    [InlineData(-720f, 0f, 180f)] // -720 behaves like 0
    [InlineData(1e6f, 280f, 100f)] // 1e6 is 2777 turns plus 280
    [InlineData(0f, 0f, 180f)]
    public void DirectionPointsTheConeAndAnyFiniteValueIsNormalized(float direction, float seenAngle, float unseenAngle)
    {
        ConeCollider2D cone = Cone(100, 90, direction);

        Assert.Equal(direction, cone.Direction);
        Vector2 seen = Polar(seenAngle, 60);
        Vector2 unseen = Polar(unseenAngle, 60);
        Assert.True(SceneGeometry2D.ContainsPoint(Geometry(cone), seen));
        Assert.False(SceneGeometry2D.ContainsPoint(Geometry(cone), unseen));
        Assert.True(Intersects(cone, Dot(seen)));
        Assert.False(Intersects(cone, Dot(unseen)));
    }

    [Fact]
    public void DirectionNinetyLooksDownTheScreenNotRight()
    {
        ConeCollider2D cone = Cone(100, 90, 90);

        Assert.True(Intersects(cone, Dot(new Vector2(0, 60))));
        Assert.False(Intersects(cone, Dot(new Vector2(60, 0))));
    }

    [Fact]
    public void ARotationOfAQuarterTurnMovesTheConeFromRightToDown()
    {
        ConeCollider2D cone = Cone(100, 90, 0);
        cone.Rotation = MathF.PI / 2;

        Assert.True(SceneGeometry2D.ContainsPoint(Geometry(cone), new Vector2(0, 60)));
        Assert.False(SceneGeometry2D.ContainsPoint(Geometry(cone), new Vector2(60, 0)));
        Assert.True(Intersects(cone, Dot(new Vector2(0, 60))));
        Assert.False(Intersects(cone, Dot(new Vector2(60, 0))));
    }

    [Theory]
    [InlineData(0f, 180f, 180f, 0f)] // Rotation pi turns a cone that looks right so that it looks left
    [InlineData(0f, -90f, -90f, 90f)] // a negative rotation turns it up
    [InlineData(90f, 90f, 180f, 90f)] // Rotation adds to Direction: 90 + 90 looks left
    public void RotationAddsToDirection(float direction, float rotationDegrees, float seenAngle, float unseenAngle)
    {
        ConeCollider2D cone = Cone(100, 90, direction);
        cone.Rotation = rotationDegrees * MathF.PI / 180;

        Assert.True(Intersects(cone, Dot(Polar(seenAngle, 60))));
        Assert.False(Intersects(cone, Dot(Polar(unseenAngle, 60))));
    }

    [Fact]
    public void OffsetsMoveTheApex()
    {
        ConeCollider2D cone = Cone(100, 90, 0);
        cone.OffsetX = 30;
        cone.OffsetY = 10;

        Assert.False(Intersects(cone, Dot(new Vector2(20, 10)))); // behind the moved apex
        Assert.True(Intersects(cone, Dot(new Vector2(40, 10)))); // in front of the moved apex
        Assert.True(Intersects(cone, Dot(new Vector2(130, 10)))); // the radius is measured from the apex
        Assert.False(Intersects(cone, Dot(new Vector2(135, 10))));
    }

    [Theory]
    [InlineData(RenderSurface2DSpriteFlip.None, 30f)]
    [InlineData(RenderSurface2DSpriteFlip.Horizontal, 150f)] // d becomes 180 - d
    [InlineData(RenderSurface2DSpriteFlip.Vertical, -30f)] // d becomes -d
    [InlineData(RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical, 210f)] // d becomes d + 180
    public void FlipMirrorsTheDirectionOfTheLocalShape(RenderSurface2DSpriteFlip flip, float expectedAngle)
    {
        ConeCollider2D cone = Cone(100, 90, 30);
        ColliderLocalShape2D shape = cone.GetLocalShape(flip);
        Assert.True(SceneGeometry2D.TryGetColliderBounds(shape, Matrix3x2.Identity, out DrawRect bounds));
        ColliderGeometry2D geometry = new(cone, shape, Matrix3x2.Identity, bounds);

        foreach (float angle in new[] { 30f, 150f, -30f, 210f })
        {
            Vector2 point = Polar(angle, 60);
            bool expected = angle == expectedAngle;
            Assert.Equal(expected, SceneGeometry2D.ContainsPoint(geometry, point));
            Assert.Equal(expected, CollisionNarrowPhase2D.Intersects(geometry, Geometry(Dot(point))));
        }
    }

    [Fact]
    public void GetLocalShapeWithoutAFlipArgumentUsesNoFlipForAConeWithoutAnOwner()
    {
        ConeCollider2D cone = Cone(100, 90, 30);

        Assert.True(Intersects(cone, Dot(Polar(30, 60))));
        Assert.False(Intersects(cone, Dot(Polar(150, 60))));
    }

    [Fact]
    public void ANarrowConeHasBoundsAroundItsApexAndArcNotAroundTheWholeCircle()
    {
        DrawRect bounds = Geometry(Cone(100, 30, 0)).SceneBounds;
        DrawRect circle = Geometry(new CircleCollider2D { Radius = 100 }).SceneBounds;

        Assert.Equal(0, bounds.X, 0.01);
        Assert.Equal(100, bounds.Width, 0.01);
        Assert.Equal(-25.88, bounds.Y, 0.01);
        Assert.Equal(51.76, bounds.Height, 0.01);
        Assert.Equal(200, circle.Width, 0.01);
        Assert.Equal(200, circle.Height, 0.01);
        Assert.True(bounds.Width * bounds.Height < circle.Width * circle.Height);
    }

    [Theory]
    [InlineData(180f, 0.0, 100.0, -100.0, 200.0)]
    [InlineData(270f, -70.71, 170.71, -100.0, 200.0)]
    public void WideConeBoundsCoverTheApexTheArcAndItsExtremePoints(
        float angle, double x, double width, double y, double height)
    {
        DrawRect bounds = Geometry(Cone(100, angle, 0)).SceneBounds;

        Assert.Equal(x, bounds.X, 0.01);
        Assert.Equal(width, bounds.Width, 0.01);
        Assert.Equal(y, bounds.Y, 0.01);
        Assert.Equal(height, bounds.Height, 0.01);
    }

    [Fact]
    public void BoundsFollowTheTransformOfTheCone()
    {
        ConeCollider2D cone = Cone(100, 30, 0);
        cone.Rotation = MathF.PI / 2;

        DrawRect bounds = Geometry(cone).SceneBounds;

        Assert.Equal(-25.88, bounds.X, 0.01);
        Assert.Equal(51.76, bounds.Width, 0.01);
        Assert.Equal(0, bounds.Y, 0.01);
        Assert.Equal(100, bounds.Height, 0.01);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidRadiusThrowsAndKeepsTheOldValue(float value)
    {
        ConeCollider2D cone = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => cone.Radius = value);
        Assert.Equal(100f, cone.Radius);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    [InlineData(360.5f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidAngleThrowsAndKeepsTheOldValue(float value)
    {
        ConeCollider2D cone = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => cone.Angle = value);
        Assert.Equal(90f, cone.Angle);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteDirectionThrowsAndKeepsTheOldValue(float value)
    {
        ConeCollider2D cone = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => cone.Direction = value);
        Assert.Equal(0f, cone.Direction);
    }

    [Theory]
    [InlineData(360f, 0f)]
    [InlineData(0.5f, -720f)]
    [InlineData(90f, 1e6f)]
    public void BoundaryAngleAndAnyFiniteDirectionAreAccepted(float angle, float direction)
    {
        ConeCollider2D cone = new() { Angle = angle, Direction = direction };

        Assert.Equal(angle, cone.Angle);
        Assert.Equal(direction, cone.Direction);
    }

    [Fact]
    public void ASpriteBodySlotRejectsAConeWithAMessageThatNamesVision()
    {
        Sprite2D sprite = new();
        ConeCollider2D cone = new();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => sprite.Collider = cone);

        Assert.Contains("Vision", error.Message);
        Assert.Null(sprite.Collider);
        Assert.Empty(sprite.LogicalChildren);
        Assert.Null(cone.LogicalParent);
    }

    [Fact]
    public void ARejectedConeLeavesTheExistingBodyInPlace()
    {
        Sprite2D sprite = new();
        BoxCollider2D body = new() { Width = 10, Height = 10 };
        sprite.Collider = body;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => sprite.Collider = new ConeCollider2D());

        Assert.Contains("Vision", error.Message);
        Assert.Same(body, sprite.Collider);
        Assert.Same(sprite, body.LogicalParent);
        Assert.Same(body, Assert.Single(sprite.LogicalChildren));
    }

    private static ConeCollider2D Cone(float radius, float angle, float direction) =>
        new() { Radius = radius, Angle = angle, Direction = direction };

    private static ColliderGeometry2D Geometry(Collider2D collider)
    {
        Assert.True(collider.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        return geometry;
    }

    // The contact rule is symmetric: both orders must agree.
    private static bool Intersects(Collider2D first, Collider2D second)
    {
        ColliderGeometry2D a = Geometry(first);
        ColliderGeometry2D b = Geometry(second);
        bool result = CollisionNarrowPhase2D.Intersects(a, b);
        Assert.Equal(result, CollisionNarrowPhase2D.Intersects(b, a));
        return result;
    }

    private static Vector2 Polar(float degrees, float distance)
    {
        double radians = degrees * Math.PI / 180;
        return new Vector2((float)(Math.Cos(radians) * distance), (float)(Math.Sin(radians) * distance));
    }

    private static float WrapRadians(float radians)
    {
        float turn = 2 * MathF.PI;
        float wrapped = radians % turn;
        return wrapped > MathF.PI ? wrapped - turn : wrapped <= -MathF.PI ? wrapped + turn : wrapped;
    }

    private static CircleCollider2D Dot(Vector2 center) =>
        new() { Radius = 0.5f, TranslateX = center.X, TranslateY = center.Y };

    // Each shape is at most 10 px across and is centred on the point.
    private static Collider2D Shape(string name, Vector2 center) => name switch
    {
        "Dot" => Dot(center),
        "Circle" => new CircleCollider2D { Radius = 10, TranslateX = center.X, TranslateY = center.Y },
        "Box" => new BoxCollider2D { Width = 10, Height = 10, TranslateX = center.X - 5, TranslateY = center.Y - 5 },
        "Polygon" => new PolygonCollider2D { Points = "0,0 10,0 5,10", TranslateX = center.X - 5, TranslateY = center.Y - 5 },
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };
}
