using System.Numerics;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class CollisionStageOneContractTests
{
    [Fact]
    [Trait("CollisionStage", "1")]
    public void FormsExposeValidatedBindableUiProperties()
    {
        BoxCollider2D box = new();
        CircleCollider2D circle = new();
        PolygonCollider2D polygon = new();

        Assert.Equal(1, box.Width);
        Assert.Equal(1, box.Height);
        Assert.Equal(1, circle.Radius);
        Assert.Equal([new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1)], polygon.Vertices);
        Assert.Throws<ArgumentOutOfRangeException>(() => box.Width = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => box.Height = float.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => circle.Radius = float.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => circle.OffsetX = float.NegativeInfinity);
        Assert.Throws<ArgumentException>(() => polygon.Points = "0,0 1,1 2,2");
        Assert.Throws<ArgumentException>(() => polygon.Points = "0,0 2,0 1,1 2,2 0,2");

        polygon.Points = "0,0 0,2 2,2 2,0";
        Assert.Equal(
            [new Vector2(0, 0), new Vector2(0, 2), new Vector2(2, 2), new Vector2(2, 0)],
            polygon.Vertices);
    }

    [Fact]
    [Trait("CollisionStage", "1")]
    public void SharedGeometryHelperComposesNestedGroupAndColliderTransforms()
    {
        BoxCollider2D collider = new()
        {
            Width = 3,
            Height = 4,
            OffsetX = 1,
            OffsetY = 2,
            TranslateX = 5
        };
        Scene2D inner = new() { TranslateY = 7, Scale = 2 };
        inner.Children.Add(collider);
        Scene2D root = new() { TranslateX = 10 };
        root.Children.Add(inner);

        Assert.True(collider.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        Assert.Equal(new Cerneala.Drawing.DrawRect(22, 11, 6, 8), geometry.SceneBounds);
        Assert.Equal(ColliderShapeKind2D.Box, geometry.LocalShape.Kind);
    }

    [Fact]
    [Trait("CollisionStage", "1")]
    public void ShapeFilterParticipationTransformAndStructureEachPublishOneMutation()
    {
        Scene2D root = new();
        Scene2D group = new();
        BoxCollider2D collider = new();
        root.Children.Add(group);
        group.Children.Add(collider);
        List<SceneCollisionMutation2D> mutations = [];
        root.CollisionMutation += mutations.Add;
        long versionBeforeMutations = root.CollisionMutationVersion;

        collider.Width = 2;
        Assert.Single(mutations);
        collider.CollisionMask = 4;
        Assert.Equal(2, mutations.Count);
        collider.Enabled = false;
        Assert.Equal(3, mutations.Count);
        group.TranslateX = 8;
        Assert.Equal(4, mutations.Count);
        group.Children.Remove(collider);
        Assert.Equal(5, mutations.Count);
        group.Children.Add(collider);
        Assert.Equal(6, mutations.Count);

        Assert.Equal(
            [
                SceneCollisionMutationKind.Geometry,
                SceneCollisionMutationKind.Filter,
                SceneCollisionMutationKind.Participation,
                SceneCollisionMutationKind.Geometry,
                SceneCollisionMutationKind.Structure,
                SceneCollisionMutationKind.Structure
            ],
            mutations.Select(static mutation => mutation.Kind));
        Assert.Equal(mutations.Count, root.CollisionMutationVersion - versionBeforeMutations);
    }

    [Fact]
    [Trait("CollisionStage", "1")]
    public void AspectAndMotionUseTheSameColliderMutationPath()
    {
        ManualMotionClock clock = new();
        UIRoot uiRoot = new(motionClock: clock);
        Scene2D scene = new();
        BoxCollider2D collider = new();
        scene.Children.Add(collider);
        RenderSurface2D surface = new() { Scene = scene };
        uiRoot.VisualChildren.Add(surface);
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;

        collider.Aspect = new ElementAspect(
            [
                new ElementAspectValue(Collider2D.EnabledProperty, false),
                new ElementAspectValue(Collider2D.CollisionLayerProperty, 8u)
            ]);
        uiRoot.ProcessFrame();

        Assert.False(collider.Enabled);
        Assert.Equal(8u, collider.CollisionLayer);
        Assert.Equal(2, mutations.Count);

        MotionHandle handle = collider.Motion()
            .Animate(BoxCollider2D.WidthProperty)
            .To(9)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        uiRoot.ProcessFrame();
        int beforeSample = mutations.Count;
        clock.Advance(TimeSpan.FromMilliseconds(50));
        uiRoot.ProcessFrame();

        Assert.InRange(collider.Width, 1.01f, 8.99f);
        Assert.Equal(beforeSample + 1, mutations.Count);
        Assert.Equal(SceneCollisionMutationKind.Geometry, mutations[^1].Kind);
    }

    [Fact]
    [Trait("CollisionStage", "1")]
    public void MotionRegistryInterpolatesGeometryAndRejectsDiscreteColliderProperties()
    {
        AnimatablePropertyRegistry registry = new();

        Assert.True(registry.TryGet(Collider2D.OffsetXProperty, out _));
        Assert.True(registry.TryGet(Collider2D.OffsetYProperty, out _));
        Assert.True(registry.TryGet(BoxCollider2D.WidthProperty, out _));
        Assert.True(registry.TryGet(BoxCollider2D.HeightProperty, out _));
        Assert.True(registry.TryGet(CircleCollider2D.RadiusProperty, out _));
        Assert.False(registry.TryGet(Collider2D.EnabledProperty, out _));
        Assert.False(registry.TryGet(Collider2D.IsTriggerProperty, out _));
        Assert.False(registry.TryGet(Collider2D.CollisionLayerProperty, out _));
        Assert.False(registry.TryGet(Collider2D.CollisionMaskProperty, out _));
        Assert.False(registry.TryGet(PolygonCollider2D.PointsProperty, out _));
    }

    [Fact]
    [Trait("CollisionStage", "1")]
    public void SharedDoorStateBindingUpdatesVisualAndColliderWithoutChangingTheGroup()
    {
        Scene2D house = new();
        Sprite2D doorVisual = new();
        BoxCollider2D doorCollider = new() { Width = 16, Height = 4 };
        house.Children.Add(doorVisual);
        house.Children.Add(doorCollider);
        ObservableValue<bool> isClosed = new(true);
        using IDisposable visualBinding = BindingOperations.BindOneWay(
            doorVisual,
            UIElement.IsVisibleProperty,
            isClosed);
        using IDisposable colliderBinding = BindingOperations.BindOneWay(
            doorCollider,
            Collider2D.EnabledProperty,
            isClosed);

        Assert.True(doorVisual.IsVisible);
        Assert.True(doorCollider.TryGetActiveSceneGeometry(out _));
        isClosed.Value = false;

        Assert.False(doorVisual.IsVisible);
        Assert.False(doorCollider.TryGetActiveSceneGeometry(out _));
        Assert.Same(house, doorVisual.LogicalParent);
        Assert.Same(house, doorCollider.LogicalParent);
    }
}
