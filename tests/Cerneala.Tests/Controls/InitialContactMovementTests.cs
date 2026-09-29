using System.Numerics;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class InitialContactMovementTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void FaceContactAllowsAwayAndTangentButBlocksApproach(int shape)
    {
        (Scene2D scene, Collider2D actor, _) = FloorContact(shape);
        Assert.Single(scene.CollisionWorld.Overlap(actor));
        foreach (Vector2 displacement in new[] { new Vector2(0, -20), new Vector2(20, 0), new Vector2(-20, 0), new Vector2(20, -20) })
        {
            MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, displacement);
            Assert.Null(result.Collision);
            Assert.Equal(displacement, result.Travel);
            Assert.Equal(Vector2.Zero, result.Remainder);
        }
        foreach (Vector2 displacement in new[] { new Vector2(0, 20), new Vector2(20, 20), new Vector2(0, 0.001f), Vector2.Zero })
        {
            MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, displacement);
            Assert.NotNull(result.Collision);
            Assert.Equal(Vector2.Zero, result.Travel);
            Assert.Equal(0, result.Collision.Fraction);
        }
        Assert.Single(scene.CollisionWorld.Overlap(actor));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void InitialPenetrationStillBlocksEveryDirection(int shape)
    {
        (Scene2D scene, Collider2D actor, Sprite2D node) = FloorContact(shape);
        node.Y += 0.1f;
        foreach (Vector2 displacement in new[] { new Vector2(0, -20), new Vector2(20, 0), new Vector2(0, 20), Vector2.Zero })
        {
            MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, displacement);
            Assert.NotNull(result.Collision);
            Assert.Equal(Vector2.Zero, result.Travel);
        }
    }

    [Fact]
    public void TangentMovementStillHitsAnotherObstacle()
    {
        (Scene2D scene, Collider2D actor, _) = FloorContact(0);
        BoxCollider2D wall = new() { Width = 4, Height = 20 };
        scene.Children.Add(new Sprite2D { X = 10, Y = -20, Collider = wall });
        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(20, 0));
        Assert.NotNull(result.Collision);
        Assert.Same(wall, result.Collision.Collider);
        Assert.InRange(result.Travel.X, 5.999f, 6.001f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialTriggerContactRemainsObservable(bool movingIsTrigger)
    {
        (Scene2D scene, Collider2D actor, _) = FloorContact(0);
        Collider2D target = Assert.Single(scene.CollisionWorld.Overlap(actor)).Collider;
        if (movingIsTrigger) { actor.IsTrigger = true; } else { target.IsTrigger = true; }
        Vector2 displacement = new(0, -20);
        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, displacement);
        Assert.Null(result.Collision);
        Assert.Equal(displacement, result.Travel);
        Assert.Equal(0, Assert.Single(result.TriggerHits).Fraction);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    public void CurvedContactAllowsSeparationAndTangentAtObliqueNormals(float sx, float sy)
    {
        foreach (float angle in new[] { 0f, 0.3f, 0.8f, 1.7f, 2.4f })
        {
            Vector2 n = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 support = new(sx * sx * n.X, sy * sy * n.Y);
            support /= new Vector2(sx * n.X, sy * n.Y).Length();
            Scene2D scene = new();
            CircleCollider2D actor = new() { Radius = 1, ScaleX = sx, ScaleY = sy };
            scene.Children.Add(new Sprite2D { Collider = actor });
            Vector2 targetPosition = support + n;
            scene.Children.Add(new Sprite2D { X = targetPosition.X, Y = targetPosition.Y, Collider = new CircleCollider2D { Radius = 1 } });
            Assert.Single(scene.CollisionWorld.Overlap(actor));
            foreach (Vector2 displacement in new[] { -n * 2, new Vector2(-n.Y, n.X) * 2, new Vector2(n.Y, -n.X) * 2 })
            {
                MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, displacement);
                Assert.True(result.Collision is null, $"scale={sx},{sy}, angle={angle}, displacement={displacement}, normal={result.Collision?.Normal}");
                Assert.Equal(displacement, result.Travel);
            }
            Assert.Equal(Vector2.Zero, scene.CollisionWorld.MoveAndCollide(actor, n).Travel);
        }
    }

    [Theory]
    [InlineData(0.3f, false)]
    [InlineData(0.8f, true)]
    [InlineData(-1.7f, false)]
    public void TransformedFaceContactPreservesDirectionAndPenetration(float angle, bool mirrored)
    {
        Scene2D root = new();
        Scene2D group = new() { Rotation = angle, ScaleX = mirrored ? -1 : 1 };
        root.Children.Add(group);
        BoxCollider2D actor = new() { Width = 4, Height = 4 };
        Sprite2D node = new() { Y = -4, Collider = actor };
        group.Children.Add(node);
        group.Children.Add(new Sprite2D { X = -10, Collider = new BoxCollider2D { Width = 20, Height = 10 } });
        Matrix3x2 transform = Matrix3x2.CreateScale(mirrored ? -1 : 1, 1) * Matrix3x2.CreateRotation(angle);
        foreach (Vector2 local in new[] { new Vector2(0, -20), new Vector2(-20, 0), new Vector2(20, 0) })
        {
            Vector2 displacement = Vector2.TransformNormal(local, transform);
            Assert.Equal(displacement, root.CollisionWorld.MoveAndCollide(actor, displacement).Travel);
        }
        Assert.Equal(Vector2.Zero, root.CollisionWorld.MoveAndCollide(actor, Vector2.TransformNormal(new Vector2(0, 20), transform)).Travel);
        node.Y += 0.01f;
        Assert.Equal(Vector2.Zero, root.CollisionWorld.MoveAndCollide(actor, Vector2.TransformNormal(new Vector2(0, -20), transform)).Travel);
    }

    [Fact]
    public void ContactToleranceDoesNotTurnSmallPenetrationIntoFreeMovement()
    {
        (Scene2D scene, Collider2D actor, Sprite2D node) = FloorContact(0);
        node.Y += 0.0001f;
        Assert.Equal(Vector2.Zero, scene.CollisionWorld.MoveAndCollide(actor, new Vector2(0, -20)).Travel);
    }

    private static (Scene2D Scene, Collider2D Actor, Sprite2D Node) FloorContact(int shape)
    {
        Scene2D scene = new();
        Collider2D floor = shape == 4
            ? new SegmentCollider2D { EndX = 200, EndY = 0 }
            : new BoxCollider2D { Width = 200, Height = 10 };
        scene.Children.Add(new Sprite2D { X = -100, Collider = floor });
        Collider2D actor = shape switch
        {
            1 => new PolygonCollider2D { Points = "0,0 4,0 4,4 0,4" },
            2 => new CircleCollider2D { Radius = 2 },
            3 => new CircleCollider2D { Radius = 2, ScaleX = 2, ScaleY = 0.5f },
            _ => new BoxCollider2D { Width = 4, Height = 4 }
        };
        Sprite2D node = new() { Y = shape == 2 ? -2 : shape == 3 ? -1 : -4, Collider = actor };
        scene.Children.Add(node);
        return (scene, actor, node);
    }
}
