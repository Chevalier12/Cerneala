using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Contract: Motion animates Radius, Angle and Direction of a ConeCollider2D like CircleCollider2D.Radius
// (docs/plans/2026-10-25-scene2d-vision.md). An invalid animated value is rejected the way it is for the circle.
// Layout: a bee at (100, 100) with a cone of Radius 80, Angle 40 and Direction 0, so it looks right. A fox box at
// x 150..160 is straight ahead; another at y 150..160 is straight below.
// A value assigned with a property setter has the Local source, which outranks the Animation source of Motion, so a
// test that animates a property leaves it unassigned (or assigns it from a lower source, as markup does).
public sealed class ConeColliderMotionTests
{
    [Fact]
    public void TheRegistryInterpolatesTheThreeConePropertiesAndDoesNotAnimateThemImplicitly()
    {
        AnimatablePropertyRegistry registry = new();
        Assert.True(registry.TryGet(CircleCollider2D.RadiusProperty, out MotionPropertyOptions? circle));

        foreach (UiProperty property in new UiProperty[]
                 { ConeCollider2D.RadiusProperty, ConeCollider2D.AngleProperty, ConeCollider2D.DirectionProperty })
        {
            Assert.True(registry.TryGet(property, out MotionPropertyOptions? options), property.DiagnosticName);
            Assert.False(options!.IsSafeForImplicitAnimation, property.DiagnosticName);
            Assert.Equal(circle!.MixerType, options.MixerType);
            Assert.Equal(circle.InvalidationCategory, options.InvalidationCategory);
            Assert.Equal(circle.DefaultSpec.GetType(), options.DefaultSpec.GetType());
        }
    }

    [Fact]
    public void MotionTurnsTheDirectionOfAVisionAndChangesWhatItSeesAtTheNextContactDelivery()
    {
        ManualMotionClock clock = new();
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene, clock);
        Sprite2D bee = Bee();
        Sprite2D ahead = Fox(150, 95);
        Sprite2D below = Fox(95, 150);
        scene.Children.Add(bee);
        scene.Children.Add(ahead);
        scene.Children.Add(below);
        ConeCollider2D cone = bee.Vision!;
        List<Collider2D> entered = [];
        List<Collider2D> exited = [];
        cone.Entered += (_, e) => entered.Add(e.Other);
        cone.Exited += (_, e) => exited.Add(e.Other);
        UiHost host = VisionHostRig.CreateHost(root);
        Assert.Same(ahead.Collider, Assert.Single(entered));

        MotionHandle handle = cone.Motion()
            .Animate(ConeCollider2D.DirectionProperty)
            .To(90)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.InRange(cone.Direction, 0.01f, 89.99f); // in flight

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        VisionHostRig.Update(host, TimeSpan.Zero);

        Assert.Equal(90f, cone.Direction);
        Assert.Same(below.Collider, Assert.Single(entered, other => ReferenceEquals(other, below.Collider)));
        Assert.Same(ahead.Collider, Assert.Single(exited));
    }

    [Theory]
    [InlineData(350f, 10f, 11f, 349f)] // linear like Rotation: 350 to 10 sweeps 340 degrees through 180
    [InlineData(350f, 370f, 351f, 369f)] // 350 to 370 takes the short way
    public void DirectionInterpolatesLinearlyWithoutTakingTheShortWayAround(float from, float to, float lowest, float highest)
    {
        ManualMotionClock clock = new();
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene, clock);
        Sprite2D bee = Bee();
        scene.Children.Add(bee);
        ConeCollider2D cone = bee.Vision!;
        cone.SetValue(ConeCollider2D.DirectionProperty, from, UiPropertyValueSource.MarkupBase);

        using MotionHandle handle = cone.Motion()
            .Animate(ConeCollider2D.DirectionProperty)
            .To(to)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(cone.Direction, lowest, highest);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        Assert.Equal(to, cone.Direction); // the value is kept as written, not normalized
    }

    // The registry is the allow-list of Motion transactions: a registered property animates, any other is set at once.
    [Theory]
    [InlineData("Radius", 100f, 20f)]
    [InlineData("Angle", 90f, 180f)]
    [InlineData("Direction", 0f, 90f)]
    public void ATransactionAnimatesTheConeProperties(string name, float start, float target)
    {
        UiProperty<float> property = name switch
        {
            "Radius" => ConeCollider2D.RadiusProperty,
            "Angle" => ConeCollider2D.AngleProperty,
            _ => ConeCollider2D.DirectionProperty
        };
        ManualMotionClock clock = new();
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene, clock);
        ConeCollider2D cone = new();
        scene.Children.Add(new Sprite2D { Vision = cone });
        root.ProcessFrame();

        using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
        {
            cone.SetValue(property, target, UiPropertyValueSource.AspectBase);
        }

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, cone.GetValueSource(property));
        float inFlight = cone.GetValue(property);
        Assert.InRange(inFlight, MathF.Min(start, target) + 0.01f, MathF.Max(start, target) - 0.01f);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        Assert.Equal(target, cone.GetValue(property));
    }

    [Fact]
    public void MotionAnimatesTheRadiusAndTheAngleOfAVision()
    {
        ManualMotionClock clock = new();
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene, clock);
        ConeCollider2D cone = new(); // Radius 100 and Angle 90 are defaults, not Local values
        scene.Children.Add(new Sprite2D { Vision = cone });

        using MotionHandle radius = cone.Motion().Animate(ConeCollider2D.RadiusProperty).To(20)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        using MotionHandle angle = cone.Motion().Animate(ConeCollider2D.AngleProperty).To(180)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(150));
        root.ProcessFrame();

        Assert.Equal(20f, cone.Radius);
        Assert.Equal(180f, cone.Angle);
    }

    [Theory]
    [InlineData("Radius", -1f)]
    [InlineData("Radius", 0f)]
    [InlineData("Radius", float.NaN)]
    [InlineData("Angle", 400f)]
    [InlineData("Angle", 0f)]
    [InlineData("Angle", float.PositiveInfinity)]
    [InlineData("Direction", float.NaN)]
    [InlineData("Direction", float.NegativeInfinity)]
    public void AnInvalidAnimatedValueIsRejectedLikeTheRadiusOfACircle(string name, float invalid)
    {
        UiProperty<float> property = name switch
        {
            "Radius" => ConeCollider2D.RadiusProperty,
            "Angle" => ConeCollider2D.AngleProperty,
            _ => ConeCollider2D.DirectionProperty
        };
        ConeCollider2D cone = new() { Radius = 50, Angle = 90, Direction = 10 };
        float before = cone.GetValue(property);

        // An animated sample the owner refuses is skipped: the value stays.
        Assert.False(cone.TrySetAnimationValueUntyped(property, invalid));
        Assert.Equal(before, cone.GetValue(property));
        // An explicit write is refused with an exception.
        Assert.Throws<ArgumentOutOfRangeException>(() => cone.SetValue(property, invalid));

        // An animation to the invalid target is refused when it starts.
        UIRoot root = new();
        using MotionPropertyBinding<float> binding = new(
            root.Motion, cone, property, root.Motion.Graph.CreateValue(before));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => binding.AnimateTo(invalid, new TweenSpec<float>(TimeSpan.FromSeconds(1))));
        Assert.Equal(before, cone.GetValue(property));
    }

    [Theory]
    [InlineData("Radius", -1f)]
    [InlineData("Angle", 400f)]
    [InlineData("Direction", float.NaN)]
    public void MotionToAnInvalidTargetIsRefusedAndLeavesTheValueInPlace(string name, float invalid)
    {
        UiProperty<float> property = name switch
        {
            "Radius" => ConeCollider2D.RadiusProperty,
            "Angle" => ConeCollider2D.AngleProperty,
            _ => ConeCollider2D.DirectionProperty
        };
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene, new ManualMotionClock());
        Sprite2D bee = Bee();
        scene.Children.Add(bee);
        ConeCollider2D cone = bee.Vision!;
        float before = cone.GetValue(property);

        Assert.Throws<ArgumentOutOfRangeException>(() => cone.Motion()
            .Animate(property)
            .To(invalid)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100))));

        root.ProcessFrame();
        Assert.Equal(before, cone.GetValue(property));
    }

    // A bee at (100, 100) with a narrow cone that looks right from its centre.
    private static Sprite2D Bee() => new()
    {
        X = 100,
        Y = 100,
        Vision = new ConeCollider2D { Radius = 80, Angle = 40 } // Direction stays the default 0, so Motion can animate it
    };

    private static Sprite2D Fox(float x, float y) => new()
    {
        X = x,
        Y = y,
        Collider = new BoxCollider2D { Width = 10, Height = 10 }
    };
}
