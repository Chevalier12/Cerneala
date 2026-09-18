using System.Numerics;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ContinuousImpactNormalTests
{
    [Fact]
    public void RotatedMirroredWholeFloorStillProducesAContact()
    {
        (Scene2D scene, Sprite2D actor) = Floor(false, false, 0.6f, true);
        actor.X = -155;
        Vector2 displacement = Vector2.TransformNormal(new Vector2(0, 60), Matrix3x2.CreateRotation(0.6f));
        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor.Collider!, displacement);
        Assert.NotNull(result.Collision);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(0.6f, false)]
    [InlineData(0.6f, true)]
    [InlineData(-1.2f, false)]
    [InlineData(-1.2f, true)]
    public void LandingNormalDoesNotTurnAlongIntroducedFloorSeams(float rotation, bool mirrored)
    {
        Matrix3x2 transform = Matrix3x2.CreateScale(mirrored ? -1 : 1, 1) *
            Matrix3x2.CreateRotation(rotation);
        Vector2 displacement = Vector2.TransformNormal(new Vector2(0, 60), transform);
        Vector2 expectedNormal = Vector2.TransformNormal(-Vector2.UnitY, transform);
        Vector2 expectedTravel = Vector2.TransformNormal(new Vector2(0, 26), transform);
        List<string> failures = [];
        foreach (bool reversed in new[] { false, true })
        {
            (Scene2D whole, Sprite2D wholeActor) = Floor(false, reversed, rotation, mirrored);
            (Scene2D pieces, Sprite2D pieceActor) = Floor(true, reversed, rotation, mirrored);
            foreach (int x in new[] { -165, -162, -160, -155, -5, -2, 0, 5, 155, 158, 160, 165 })
            {
                wholeActor.X = pieceActor.X = x;
                MoveCollisionResult2D reference = whole.CollisionWorld.MoveAndCollide(wholeActor.Collider!, displacement);
                MoveCollisionResult2D actual = pieces.CollisionWorld.MoveAndCollide(pieceActor.Collider!, displacement);
                if (reference.Collision is null || actual.Collision is null)
                {
                    failures.Add($"x={x}, reversed={reversed}: missing hit; " +
                        $"whole={reference.Collision is not null}, pieces={actual.Collision is not null}");
                    continue;
                }
                Assert.InRange(Vector2.Distance(expectedTravel, reference.Travel), 0, 0.002f);
                Assert.InRange(Vector2.Distance(expectedTravel, actual.Travel), 0, 0.002f);
                if (Vector2.Distance(expectedNormal, reference.Collision.Normal) > 0.001f ||
                    Vector2.Distance(expectedNormal, actual.Collision.Normal) > 0.001f)
                {
                    failures.Add($"x={x}, reversed={reversed}: expected={expectedNormal}, " +
                        $"whole={reference.Collision.Normal}, pieces={actual.Collision.Normal}");
                }
                Assert.Equal(-30, pieceActor.Y);
                Assert.Equal(x, pieceActor.X);
            }
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ObliqueApproachKeepsSurfaceNormalRatherThanOppositeDisplacement(int shape)
    {
        Collider2D collider = shape switch
        {
            0 => new BoxCollider2D { Width = 4, Height = 4 },
            1 => new PolygonCollider2D { Points = "0,0 4,0 4,4 0,4" },
            2 => new CircleCollider2D { Radius = 2 },
            _ => new CircleCollider2D { Radius = 2, ScaleX = 2, ScaleY = 0.5f }
        };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = -100, Collider = new BoxCollider2D { Width = 200, Height = 10 } });
        Sprite2D actor = new() { Y = -30, Collider = collider };
        scene.Children.Add(actor);
        Vector2 displacement = new(30, 60);

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(collider, displacement);

        Assert.NotNull(result.Collision);
        Assert.InRange(Vector2.Distance(-Vector2.UnitY, result.Collision.Normal), 0, 0.002f);
        Assert.InRange(result.Collision.Fraction, 0.43f, 0.49f);
        Assert.Equal(displacement, result.Travel + result.Remainder);
        Assert.Equal(-30, actor.Y);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(-20)]
    public void InitialTouchRetainsStaticContactNormalIncludingZeroAndSeparatingMovement(float dy)
    {
        Scene2D scene = new();
        BoxCollider2D target = new() { Width = 160, Height = 10 };
        BoxCollider2D actor = new() { Width = 4, Height = 4 };
        scene.Children.Add(new Sprite2D { X = -160, Collider = target });
        scene.Children.Add(new Sprite2D { Y = -4, Collider = actor });
        CollisionHit2D overlap = Assert.Single(scene.CollisionWorld.Overlap(actor));

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(0, dy));

        Assert.Equal(Vector2.UnitX, overlap.Normal);
        Assert.NotNull(result.Collision);
        Assert.Equal(overlap.Normal, result.Collision.Normal);
        Assert.Equal(overlap.Point, result.Collision.Point);
        Assert.Equal(0, result.Collision.Fraction);
        Assert.Equal(0, result.Collision.Distance);
        Assert.Equal(Vector2.Zero, result.Travel);
    }

    [Fact]
    public void ObliqueLandingAtASeamKeepsTheFloorNormal()
    {
        (Scene2D scene, Sprite2D actor) = Floor(true, false, 0, false);
        actor.X = -173;

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor.Collider!, new Vector2(30, 60));

        Assert.NotNull(result.Collision);
        Assert.Equal(-Vector2.UnitY, result.Collision.Normal);
        Assert.InRange(Vector2.Distance(new Vector2(13, 26), result.Travel), 0, 0.002f);
        Assert.InRange(MathF.Abs(result.Collision.Point.Y), 0, 0.002f);
    }

    [Fact]
    public void CurvedContactKeepsTheAnalyticSurfaceNormal()
    {
        Scene2D scene = new();
        CircleCollider2D actor = new() { Radius = 2 };
        CircleCollider2D target = new() { Radius = 5 };
        scene.Children.Add(new Sprite2D { X = -30, Y = 3, Collider = actor });
        scene.Children.Add(new Sprite2D { Collider = target });
        Vector2 expectedNormal = new(-MathF.Sqrt(40) / 7, 3f / 7);

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(60, 0));

        Assert.NotNull(result.Collision);
        Assert.Same(target, result.Collision.Collider);
        Assert.InRange(Vector2.Distance(expectedNormal, result.Collision.Normal), 0, 0.002f);
        Assert.InRange(Vector2.Distance(expectedNormal * 5, result.Collision.Point), 0, 0.02f);
        Assert.InRange(MathF.Abs(result.Travel.X - (30 - MathF.Sqrt(40))), 0, 0.002f);
    }

    [Fact]
    public void SegmentEndpointImpactUsesTheGeometricApproachNormal()
    {
        Scene2D scene = new();
        CircleCollider2D actor = new() { Radius = 5 };
        SegmentCollider2D target = new() { EndX = 20, EndY = 0 };
        scene.Children.Add(new Sprite2D { X = -3, Y = -30, Collider = actor });
        scene.Children.Add(new Sprite2D { Collider = target });

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(0, 60));

        Assert.NotNull(result.Collision);
        Assert.Same(target, result.Collision.Collider);
        Assert.InRange(Vector2.Distance(new Vector2(-0.6f, -0.8f), result.Collision.Normal), 0, 0.002f);
        Assert.InRange(result.Collision.Point.Length(), 0, 0.002f);
        Assert.InRange(MathF.Abs(result.Travel.Y - 26), 0, 0.002f);
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(0, -60)]
    [InlineData(0, 0)]
    public void SeparatedTangentialOrAwayMovementDoesNotCreateContact(float dx, float dy)
    {
        (Scene2D scene, Sprite2D actor) = Floor(true, false, 0, false);
        Vector2 displacement = new(dx, dy);

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor.Collider!, displacement);

        Assert.Null(result.Collision);
        Assert.Equal(displacement, result.Travel);
    }

    private static (Scene2D Scene, Sprite2D Actor) Floor(bool divided, bool reversed, float rotation, bool mirrored)
    {
        Scene2D root = new();
        Scene2D group = new() { Rotation = rotation, ScaleX = mirrored ? -1 : 1 };
        root.Children.Add(group);
        IEnumerable<int> origins = divided ? new[] { -320, -160, 0, 160 } : new[] { -320 };
        foreach (int x in reversed ? origins.Reverse() : origins)
        {
            group.Children.Add(new Sprite2D
            {
                X = x,
                Collider = new BoxCollider2D { Width = divided ? 160 : 640, Height = 10 }
            });
        }
        Sprite2D actor = new() { Y = -30, Collider = new BoxCollider2D { Width = 4, Height = 4 } };
        group.Children.Add(actor);
        return (root, actor);
    }
}
