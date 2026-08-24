using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.Tests.UI.Motion;

public sealed class ObjectMotionTests
{
    [Fact]
    public void WritablePropertyExpressionNeedsNoDeclaredMotionProperty()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            PlainAnimatedGauge gauge = new() { Value = 2 };

            MotionHandle handle = gauge.Motion()
                .Animate(current => current.Value)
                .From(2)
                .To(10)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            Assert.InRange(gauge.Value, 5.999f, 6.001f);
            Assert.True(handle.IsActive);

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            Assert.Equal(10, gauge.Value);
            Assert.True(handle.IsCompleted);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    [Fact]
    public void RepeatedPropertyExpressionsShareTheSameMotionBinding()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            PlainAnimatedGauge gauge = new();
            MotionHandle first = gauge.Motion()
                .Animate(current => current.Value)
                .To(10)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            MotionHandle second = gauge.Motion()
                .Animate(current => current.Value)
                .To(20)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));

            Assert.True(first.IsCanceled);
            Assert.True(second.IsActive);

            clock.Advance(TimeSpan.FromMilliseconds(100));
            ObjectMotionRuntime.TickCurrent();

            Assert.Equal(20, gauge.Value);
            Assert.True(second.IsCompleted);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    [Fact]
    public void PropertyExpressionMustSelectADirectWritableMember()
    {
        PlainAnimatedGauge gauge = new();

        ArgumentException nested = Assert.Throws<ArgumentException>(() =>
            gauge.Motion().Animate(current => current.Child.Value));
        ArgumentException method = Assert.Throws<ArgumentException>(() =>
            gauge.Motion().Animate(current => current.GetValue()));
        ArgumentException readOnly = Assert.Throws<ArgumentException>(() =>
            gauge.Motion().Animate(current => current.ReadOnly));

        Assert.Contains("directly select a writable instance member", nested.Message);
        Assert.Contains("directly select a writable instance member", method.Message);
        Assert.Contains("must be writable", readOnly.Message);
    }

    [Fact]
    public void UiElementMotionStillUsesTheElementFacade()
    {
        UIElement element = new();

        MotionElementFacade facade = element.Motion();

        Assert.NotNull(facade);
    }

    [Fact]
    public void ArbitraryObjectPropertyUsesTheExistingMotionEngine()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            AnimatedGauge gauge = new() { Value = 2 };

            MotionHandle handle = gauge.Motion()
                .Animate(AnimatedGauge.ValueProperty)
                .From(2)
                .To(10)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            Assert.InRange(gauge.Value, 5.999f, 6.001f);
            Assert.True(handle.IsActive);

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            Assert.Equal(10, gauge.Value);
            Assert.True(handle.IsCompleted);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    [Fact]
    public void UiFrameLoopAdvancesAnObjectThatIsNotDrawnOrAttached()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            AnimatedGauge gauge = new();
            gauge.Motion()
                .Animate(AnimatedGauge.ValueProperty)
                .To(20)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));
            UiHost host = new(new UiHostOptions
            {
                Root = new UIRoot(),
                Viewport = new UiViewport(100, 100)
            });

            clock.Advance(TimeSpan.FromMilliseconds(40));
            host.Update(
                EmptyInputFrame(),
                elapsedTime: TimeSpan.FromMilliseconds(40));

            Assert.InRange(gauge.Value, 7.999f, 8.001f);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    [Fact]
    public void HoldOnCompleteFalseRestoresTheOriginalClrValue()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            AnimatedGauge gauge = new() { Value = 4 };
            MotionHandle handle = gauge.Motion()
                .Animate(AnimatedGauge.ValueProperty)
                .To(12)
                .Start(
                    new TweenSpec<float>(
                        TimeSpan.FromMilliseconds(100),
                        Easings.Linear),
                    new MotionPropertyStartOptions
                    {
                        HoldOnComplete = false
                    });

            clock.Advance(TimeSpan.FromMilliseconds(100));
            ObjectMotionRuntime.TickCurrent();

            Assert.Equal(4, gauge.Value);
            Assert.True(handle.IsCompleted);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    [Fact]
    public void GeneratedPrismPropertiesUseTheGenericObjectMotionContract()
    {
        Type descriptorType = typeof(MotionProperty<,>);
        IEnumerable<PrismCatalogOperationInfo> operations =
            PrismCatalog.Filters.Concat(PrismCatalog.Styles);

        foreach (PrismCatalogOperationInfo operation in operations)
        {
            string suffix = operation.Kind == PrismCatalogOperationKind.Filter
                ? "Filter"
                : "Style";
            Type generated = typeof(PrismImage).Assembly.GetType(
                $"Cerneala.Drawing.Prism.{operation.Symbol}{suffix}")!;
            string[] expected = operation.Parameters
                .Select(parameter => parameter.Name + "Property")
                .Append("VisibleProperty")
                .Concat(operation.Kind == PrismCatalogOperationKind.Filter
                    ? ["OpacityProperty", "BlendModeProperty"]
                    : [])
                .ToArray();

            foreach (string propertyName in expected)
            {
                System.Reflection.FieldInfo? property = generated.GetField(
                    propertyName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static);
                Assert.NotNull(property);
                Assert.True(property.IsInitOnly);
                Assert.True(property.FieldType.IsGenericType);
                Assert.Equal(
                    descriptorType,
                    property.FieldType.GetGenericTypeDefinition());
                Assert.Equal(generated, property.FieldType.GenericTypeArguments[0]);
            }
        }
    }

    [Fact]
    public void PrismOperationIsOnlyOnePossibleObjectMotionTarget()
    {
        ManualMotionClock clock = new();
        ObjectMotionRuntime.ResetForTests(clock);
        try
        {
            OuterGlowStyle glow = new() { Size = 3 };
            MotionHandle handle = glow.Motion()
                .Animate(OuterGlowStyle.SizeProperty)
                .From(3)
                .To(18)
                .Start(new TweenSpec<float>(
                    TimeSpan.FromMilliseconds(100),
                    Easings.Linear));

            clock.Advance(TimeSpan.FromMilliseconds(50));
            ObjectMotionRuntime.TickCurrent();

            Assert.InRange(glow.Size, 10.499f, 10.501f);
            Assert.True(handle.IsActive);
        }
        finally
        {
            ObjectMotionRuntime.ResetForTests();
        }
    }

    private static InputFrame EmptyInputFrame() => new(
        PointerSnapshot.Empty,
        PointerSnapshot.Empty,
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.Empty,
        []);

    private sealed class AnimatedGauge
    {
        public static MotionProperty<AnimatedGauge, float> ValueProperty { get; } =
            MotionProperty.Create<AnimatedGauge, float>(
                nameof(Value),
                static gauge => gauge.Value,
                static (gauge, value) => gauge.Value = value);

        public float Value { get; set; }
    }

    private sealed class PlainAnimatedGauge
    {
        public float Value { get; set; }

        public PlainAnimatedGauge Child { get; } = null!;

        public float ReadOnly => Value;

        public float GetValue() => Value;
    }
}
