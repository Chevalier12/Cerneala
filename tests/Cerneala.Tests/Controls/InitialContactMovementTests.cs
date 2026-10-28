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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlapNormalPointsFromReturnedColliderTowardQueriedCollider(bool leftIsBox)
    {
        Scene2D scene = new();
        Collider2D left = leftIsBox
            ? new BoxCollider2D { Width = 10, Height = 6 }
            : new SegmentCollider2D { EndX = 10, EndY = 0 };
        BoxCollider2D box = new() { Width = 4, Height = 6 };
        scene.Children.Add(new Sprite2D { X = 0, Y = leftIsBox ? -3 : 0, Collider = left });
        scene.Children.Add(new Sprite2D { X = 8, Y = -3, Collider = box });

        CollisionHit2D boxHit = Assert.Single(scene.CollisionWorld.Overlap(left));
        CollisionHit2D leftHit = Assert.Single(scene.CollisionWorld.Overlap(box));

        Assert.Same(box, boxHit.Collider);
        Assert.Same(left, leftHit.Collider);
        Assert.True(boxHit.Normal.X < -0.99f, $"Overlap(left) normal={boxHit.Normal}");
        Assert.True(leftHit.Normal.X > 0.99f, $"Overlap(box) normal={leftHit.Normal}");
    }

    // The segment tip is 2 units inside the box to its right, so the normal toward the moving collider is -X; the box is the SAT control.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialPenetrationNormalPointsFromReturnedColliderTowardMovingCollider(bool leftIsBox)
    {
        Scene2D scene = new();
        Collider2D left = leftIsBox
            ? new BoxCollider2D { Width = 10, Height = 6 }
            : new SegmentCollider2D { EndX = 10, EndY = 0 };
        BoxCollider2D box = new() { Width = 4, Height = 6 };
        scene.Children.Add(new Sprite2D { X = 0, Y = leftIsBox ? -3 : 0, Collider = left });
        scene.Children.Add(new Sprite2D { X = 8, Y = -3, Collider = box });

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(left, new Vector2(0, 1));

        Assert.NotNull(result.Collision);
        Assert.Same(box, result.Collision.Collider);
        Assert.True(result.Collision.Normal.X < -0.99f, $"MoveAndCollide normal={result.Collision.Normal}");
    }

    // A cone (a Vision, reaching x = 10) and an ellipse (x = -4..4) take the GJK/EPA path; each overlaps the
    // box at x = 8..12 (cone) or x = 2..6 (ellipse) by 2 units from the left, so the normal toward them is -X.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EpaOverlapNormalOfAConeOrAnEllipsePointsFromTheBoxTowardTheQueriedShape(bool cone)
    {
        Scene2D scene = new();
        BoxCollider2D box = new() { Width = 4, Height = 6 };
        Collider2D shape;
        if (cone)
        {
            ConeCollider2D vision = new() { Radius = 10, Angle = 30, Direction = 0 };
            scene.Children.Add(new Sprite2D { Vision = vision });
            scene.Children.Add(new Sprite2D { X = 8, Y = -3, Collider = box });
            shape = vision;
        }
        else
        {
            shape = new CircleCollider2D { Radius = 2, ScaleX = 2, ScaleY = 0.5f };
            scene.Children.Add(new Sprite2D { Collider = shape });
            scene.Children.Add(new Sprite2D { X = 2, Y = -3, Collider = box });
        }

        CollisionHit2D boxHit = Assert.Single(scene.CollisionWorld.Overlap(shape));

        Assert.Same(box, boxHit.Collider);
        Assert.True(boxHit.Normal.X < -0.99f, $"Overlap(shape) normal={boxHit.Normal}");
        if (!cone)
        {
            CollisionHit2D shapeHit = Assert.Single(scene.CollisionWorld.Overlap(box));
            Assert.True(shapeHit.Normal.X > 0.99f, $"Overlap(box) normal={shapeHit.Normal}");
            MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(shape, new Vector2(0, 1));
            Assert.NotNull(result.Collision);
            Assert.True(result.Collision.Normal.X < -0.99f, $"MoveAndCollide normal={result.Collision.Normal}");
        }
    }

    [Theory]
    [InlineData(127.99999237f, 128f)]
    [InlineData(63.99999619f, 64f)]
    public void EdgeContactWithinToleranceIsFoundAcrossAGridCellBoundary(float widthOfA, float xOfB)
    {
        Scene2D scene = new();
        BoxCollider2D a = new() { Width = widthOfA, Height = 10 };
        BoxCollider2D b = new() { Width = 10, Height = 10 };
        scene.Children.Add(new Sprite2D { X = 0, Y = 0, Collider = a });
        scene.Children.Add(new Sprite2D { X = xOfB, Y = 0, Collider = b });

        Assert.True(scene.CollisionWorld.Intersects(a, b));
        Assert.Same(b, Assert.Single(scene.CollisionWorld.Overlap(a)).Collider);
    }

    // The same contact across the y = 128 line and at the (128, 128) cell corner; b's Overlap must find a as well.
    [Theory]
    [InlineData(10f, 127.99999237f, 0f, 128f)]
    [InlineData(127.99999237f, 127.99999237f, 128f, 128f)]
    public void ContactWithinToleranceIsFoundAcrossAGridCellBoundaryInYAndAtACorner(
        float widthOfA, float heightOfA, float xOfB, float yOfB)
    {
        Scene2D scene = new();
        BoxCollider2D a = new() { Width = widthOfA, Height = heightOfA };
        BoxCollider2D b = new() { Width = 10, Height = 10 };
        scene.Children.Add(new Sprite2D { X = 0, Y = 0, Collider = a });
        scene.Children.Add(new Sprite2D { X = xOfB, Y = yOfB, Collider = b });

        Assert.True(scene.CollisionWorld.Intersects(a, b));
        Assert.Same(b, Assert.Single(scene.CollisionWorld.Overlap(a)).Collider);
        Assert.Same(a, Assert.Single(scene.CollisionWorld.Overlap(b)).Collider);
    }

    // Same geometry with b as a trigger and a moving along the shared edge; the cell-0 pair is the control.
    [Theory]
    [InlineData(127.99999237f, 128f)]
    [InlineData(63.99999619f, 64f)]
    public void TriggerTouchingAcrossAGridCellBoundaryIsListedInTriggerHits(float widthOfA, float xOfB)
    {
        Scene2D scene = new();
        BoxCollider2D a = new() { Width = widthOfA, Height = 10 };
        BoxCollider2D b = new() { Width = 10, Height = 10, IsTrigger = true };
        scene.Children.Add(new Sprite2D { X = 0, Y = 0, Collider = a });
        scene.Children.Add(new Sprite2D { X = xOfB, Y = 0, Collider = b });

        Assert.True(scene.CollisionWorld.Intersects(a, b));
        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(a, new Vector2(0, -20));

        Assert.True(result.TriggerHits.Count == 1, $"TriggerHits.Count={result.TriggerHits.Count}");
        Assert.Same(b, result.TriggerHits[0].Collider);
    }

    [Theory]
    [InlineData(-0.05f)]
    [InlineData(0f)]
    [InlineData(-3f)]
    public void OverlapNormalsOfOnePairAreOpposite(float thinY)
    {
        Scene2D scene = new();
        BoxCollider2D thin = new() { Width = 10, Height = 0.1f };
        BoxCollider2D box = new() { Width = 4, Height = 6 };
        scene.Children.Add(new Sprite2D { X = 0, Y = thinY, Collider = thin });
        scene.Children.Add(new Sprite2D { X = 8, Y = -3, Collider = box });

        CollisionHit2D hitOnBox = Assert.Single(scene.CollisionWorld.Overlap(thin));
        CollisionHit2D hitOnThin = Assert.Single(scene.CollisionWorld.Overlap(box));

        float dot = Vector2.Dot(hitOnBox.Normal, hitOnThin.Normal);
        Assert.True(dot < -0.99f, $"Overlap(thin)={hitOnBox.Normal}, Overlap(box)={hitOnThin.Normal}, dot={dot}");
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
