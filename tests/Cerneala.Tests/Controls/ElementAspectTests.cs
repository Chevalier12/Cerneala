using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ElementAspectTests
{
    [Fact]
    public void AspectAppliesReplacesAndClearsLocalAspectValues()
    {
        Button button = new();
        UIRoot root = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Brush? defaultForeground = button.Foreground;
        ElementAspect first = new(
            [new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.White))]);
        ElementAspect second = new(
            [new ElementAspectValue(Control.ForegroundProperty, new SolidColorBrush(Color.Transparent))]);

        button.Aspect = first;
        root.ProcessFrame();

        Assert.Same(first, button.Aspect);
        Assert.Equal(new Cerneala.UI.Media.SolidColorBrush(Color.White), button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = second;
        root.ProcessFrame();

        Assert.Equal(new SolidColorBrush(Color.White), button.Background);
        Assert.Equal(new SolidColorBrush(Color.Transparent), button.Foreground);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.ForegroundProperty));

        button.Aspect = null;
        root.ProcessFrame();

        Assert.Null(button.Aspect);
        Assert.Equal(defaultForeground, button.Foreground);
    }

    [Fact]
    public void AspectRejectsDuplicateDefaultProperties()
    {
        Assert.Throws<ArgumentException>(() => new ElementAspect(
        [
            new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.Black)),
            new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.White))
        ]));
    }

    [Fact]
    public void DefaultViewStaysLiveWhileConditionalSnapshotsSurvivePackageRebuilds()
    {
        List<ElementAspectValue> defaults = [new(UIElement.OpacityProperty, 0.4f)];
        List<ElementAspectValue> conditionalValues = [new(UIElement.OpacityProperty, 0.8f)];
        AspectConditionKey key = new("active");
        ElementAspectCondition condition = new(key, conditionalValues, 0);
        List<ElementAspectCondition> conditions = [condition];
        ElementAspect aspect = new("editable", typeof(Button), defaults, conditions);
        IReadOnlyList<ElementAspectValue> defaultView = aspect.DefaultValues;
        IReadOnlyList<ElementAspectCondition> conditionSnapshot = aspect.Conditions;
        defaults.Clear();
        conditionalValues.Clear();
        conditions.Clear();
        Assert.Single(defaultView);
        Assert.Same(condition, Assert.Single(conditionSnapshot));
        Assert.Equal(0.8f, Assert.Single(condition.Values).Value);
        Assert.Same(key, Assert.Single(aspect.ConditionKeys));
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<ElementAspectValue>>(defaultView).Clear());
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<ElementAspectCondition>>(conditionSnapshot).Clear());

        UIRoot root = new();
        Button button = new() { Aspect = aspect };
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Equal(0.4f, button.Opacity);

        Assert.True(aspect.SetValue(UIElement.OpacityProperty, 0.6f));
        Assert.True(aspect.SetValue(UIElement.WidthProperty, 120f));
        root.ProcessFrame();
        Assert.Same(defaultView, aspect.DefaultValues);
        Assert.Equal(2, defaultView.Count);
        Assert.Equal(0.6f, defaultView[0].Value);
        Assert.Equal(120f, button.Width);
        Assert.Equal(0.6f, button.Opacity);

        Assert.True(key.SetActive(button, true));
        root.ProcessFrame();
        Assert.Equal(0.8f, button.Opacity);

        Assert.True(aspect.SetValue(UIElement.OpacityProperty, 0.7f));
        root.ProcessFrame();
        Assert.Same(conditionSnapshot, aspect.Conditions);
        Assert.Same(condition, Assert.Single(aspect.Conditions));
        Assert.Equal(0.8f, button.Opacity);

        Assert.True(key.SetActive(button, false));
        root.ProcessFrame();
        Assert.Equal(0.7f, button.Opacity);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void AspectRejectsAnElementOutsideItsDeclaredTargetType()
    {
        ElementAspect aspect = new(
            "text-only",
            typeof(TextBlock),
            [new ElementAspectValue(TextBlock.TextProperty, "text")]);

        Assert.Throws<InvalidOperationException>(() => new Button().Aspect = aspect);
    }

    [Fact]
    public void ReplacingAspectOnlyInvalidatesPropertiesWhoseValuesChanged()
    {
        Button button = new();
        UIRoot root = new();
        root.VisualChildren.Add(button);
        ElementAspect first = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f),
            new ElementAspectValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White))
        ]);
        ElementAspect second = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f),
            new ElementAspectValue(Control.BorderBrushProperty, new SolidColorBrush(Color.Black))
        ]);

        button.Aspect = first;
        root.ProcessFrame();

        button.Aspect = second;
        FrameStats update = root.ProcessFrame();

        Assert.Equal(0, update.MeasureCalls);
        Assert.Equal(0, update.ArrangeCalls);
        Assert.True(update.RenderedElements > 0);
        Assert.Equal(120f, button.Width);
        Assert.Equal(new SolidColorBrush(Color.Black), button.BorderBrush);
    }

    [Fact]
    public void UpdatingAnAspectValuePreservesTheAspectAndInvalidatesOnlyThatProperty()
    {
        Button button = new();
        UIRoot root = new();
        root.VisualChildren.Add(button);
        ElementAspect aspect = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f)
        ]);
        button.Aspect = aspect;
        root.ProcessFrame();

        Assert.True(aspect.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White)));

        Assert.Same(aspect, button.Aspect);
        Assert.Equal(new SolidColorBrush(Color.White), aspect.DefaultValues[1].Value);
        Assert.True(button.DirtyState.Has(InvalidationFlags.Aspect));
        Assert.False(button.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(button.DirtyState.Has(InvalidationFlags.Arrange));
        FrameStats update = root.ProcessFrame();
        Assert.Equal(new SolidColorBrush(Color.White), button.BorderBrush);
        Assert.True(update.RenderedElements > 0);

        Assert.False(aspect.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White)));
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void ReplacingAnAspectWhoseLifetimeThrowsOnDisposeAppliesTheNewAspectAndThenThrows()
    {
        UIRoot root = new();
        Border border = new();
        int created = 0;
        ElementAspect oldAspect = new(
            "Old",
            typeof(UIElement),
            [],
            [],
            _ => new CallbackLifetime(() => throw new InvalidOperationException("Dispose failed")));
        ElementAspect newAspect = new(
            "New",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            _ =>
            {
                created++;
                return new CallbackLifetime(() => { });
            });
        border.Aspect = oldAspect;
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => border.Aspect = newAspect);
        root.ProcessFrame();

        Assert.Equal("Dispose failed", failure.Message);
        Assert.Same(newAspect, border.Aspect);
        Assert.Equal(1, created);
        Assert.Equal(0.5f, border.Opacity);

        Assert.True(oldAspect.SetValue(UIElement.OpacityProperty, 0.9f));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Aspect));

        Assert.True(newAspect.SetValue(UIElement.OpacityProperty, 0.25f));
        root.ProcessFrame();
        Assert.Equal(0.25f, border.Opacity);
    }

    [Fact]
    public void ClearingAnAspectWhoseLifetimeThrowsOnDisposeRemovesItsValuesAndThenThrows()
    {
        UIRoot root = new();
        Border border = new();
        ElementAspect aspect = new(
            "Old",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            _ => new CallbackLifetime(() => throw new InvalidOperationException("Dispose failed")));
        border.Aspect = aspect;
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        Assert.Equal(0.5f, border.Opacity);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => border.Aspect = null);
        root.ProcessFrame();

        Assert.Equal("Dispose failed", failure.Message);
        Assert.Null(border.Aspect);
        Assert.Equal(1f, border.Opacity);

        Assert.True(aspect.SetValue(UIElement.OpacityProperty, 0.25f));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Aspect));
    }

    [Fact]
    public void ReplacingAnAspectWhosePropertyChangedHandlerThrowsFollowsTheNewAspectAndThenThrows()
    {
        UIRoot root = new();
        Border border = new();
        int oldAlive = 0;
        int newCreated = 0;
        ElementAspect oldAspect = new(
            "Old",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.9f)],
            [],
            _ =>
            {
                oldAlive++;
                return new CallbackLifetime(() => oldAlive--);
            });
        ElementAspect newAspect = new(
            "New",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            _ =>
            {
                newCreated++;
                return new CallbackLifetime(() => { });
            });
        border.Aspect = oldAspect;
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        bool armed = true;
        border.PropertyChanged += (_, args) =>
        {
            if (armed && ReferenceEquals(args.Property, UIElement.AspectProperty))
            {
                throw new InvalidOperationException("Handler failed");
            }
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => border.Aspect = newAspect);
        armed = false;
        root.ProcessFrame();

        Assert.Equal("Handler failed", failure.Message);
        Assert.Same(newAspect, border.Aspect);
        Assert.Equal(0, oldAlive);
        Assert.Equal(1, newCreated);
        Assert.Equal(0.5f, border.Opacity);

        Assert.True(newAspect.SetValue(UIElement.OpacityProperty, 0.25f));
        root.ProcessFrame();
        Assert.Equal(0.25f, border.Opacity);

        Assert.True(oldAspect.SetValue(UIElement.OpacityProperty, 0.8f));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Aspect));
    }

    [Fact]
    public void AspectWhoseBehaviorFactoryThrowsIsStillFollowedAndCreatesItsLifetimeOnReattach()
    {
        UIRoot root = new();
        Border border = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        bool throwInFactory = true;
        int created = 0;
        ElementAspect aspect = new(
            "Throwing",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            _ =>
            {
                if (throwInFactory)
                {
                    throw new InvalidOperationException("Factory failed");
                }

                created++;
                return new CallbackLifetime(() => { });
            });

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => border.Aspect = aspect);
        root.ProcessFrame();

        Assert.Equal("Factory failed", failure.Message);
        Assert.Same(aspect, border.Aspect);
        Assert.Equal(0.5f, border.Opacity);

        Assert.True(aspect.SetValue(UIElement.OpacityProperty, 0.4f));
        root.ProcessFrame();
        Assert.Equal(0.4f, border.Opacity);

        throwInFactory = false;
        root.VisualChildren.Remove(border);
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        Assert.Equal(1, created);
        Assert.True(aspect.SetValue(UIElement.OpacityProperty, 0.25f));
        root.ProcessFrame();
        Assert.Equal(0.25f, border.Opacity);
    }

    [Fact]
    public void OldLifetimeThatAssignsAnotherAspectOnDisposeLeavesTheElementFollowingOnlyThatAspect()
    {
        UIRoot root = new();
        Border border = new();
        List<string> events = [];
        ElementAspect x = new(
            "X",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.7f)],
            [],
            _ =>
            {
                events.Add("create X");
                return new CallbackLifetime(() => events.Add("dispose X"));
            });
        ElementAspect newAspect = new(
            "New",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            _ =>
            {
                events.Add("create New");
                return new CallbackLifetime(() => events.Add("dispose New"));
            });
        bool reassign = true;
        ElementAspect oldAspect = new(
            "Old",
            typeof(UIElement),
            [],
            [],
            _ => new CallbackLifetime(() =>
            {
                events.Add("dispose Old");
                if (reassign)
                {
                    reassign = false;
                    border.Aspect = x;
                }
            }));
        border.Aspect = oldAspect;
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        border.Aspect = newAspect;
        root.ProcessFrame();

        Assert.Same(x, border.Aspect);
        Assert.Equal(0.7f, border.Opacity);
        Assert.Equal(["dispose Old", "create X"], events);
        Assert.False(border.DirtyState.Has(InvalidationFlags.Aspect));

        Assert.True(newAspect.SetValue(UIElement.OpacityProperty, 0.3f));
        Assert.False(border.DirtyState.Has(InvalidationFlags.Aspect));

        Assert.True(x.SetValue(UIElement.OpacityProperty, 0.6f));
        root.ProcessFrame();
        Assert.Equal(0.6f, border.Opacity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BehaviorThatReplacesItsAspectWhileAttachingDisposesEachLifetimeWithItsAspect(bool assignBeforeAttach)
    {
        UIRoot root = new();
        Border border = new();
        List<string> disposed = [];
        ElementAspect replacement = new(
            "Replacement",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.25f)],
            [],
            _ => new CallbackLifetime(() => disposed.Add("replacement")));
        ElementAspect original = new(
            "Original",
            typeof(UIElement),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)],
            [],
            element =>
            {
                element.Aspect = replacement;
                return new CallbackLifetime(() => disposed.Add("original"));
            });

        if (assignBeforeAttach)
        {
            border.Aspect = original;
            root.VisualChildren.Add(border);
        }
        else
        {
            root.VisualChildren.Add(border);
            border.Aspect = original;
        }

        root.ProcessFrame();

        Assert.Same(replacement, border.Aspect);
        Assert.Equal(0.25f, border.Opacity);
        Assert.Equal(["original"], disposed);

        border.Aspect = null;

        Assert.Equal(["original", "replacement"], disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BehaviorThatDetachesItsElementWhileAttachingIsDisposedAndRecreatedOnReattach(bool assignBeforeAttach)
    {
        UIRoot root = new();
        Border border = new();
        List<string> events = [];
        int attaches = 0;
        ElementAspect aspect = new(
            "Detacher",
            typeof(UIElement),
            [],
            [],
            element =>
            {
                int attach = ++attaches;
                events.Add($"create {attach}");
                if (attach == 1)
                {
                    root.VisualChildren.Remove(element);
                }

                return new CallbackLifetime(() => events.Add($"dispose {attach}"));
            });

        if (assignBeforeAttach)
        {
            border.Aspect = aspect;
            root.VisualChildren.Add(border);
        }
        else
        {
            root.VisualChildren.Add(border);
            border.Aspect = aspect;
        }

        Assert.False(border.IsAttached);
        Assert.Equal(["create 1", "dispose 1"], events);

        root.VisualChildren.Add(border);
        root.ProcessFrame();

        Assert.Equal(["create 1", "dispose 1", "create 2"], events);

        border.Aspect = null;

        Assert.Equal(["create 1", "dispose 1", "create 2", "dispose 2"], events);
    }

    [Fact]
    public void BehaviorWhoseDisposeThrowsOnDetachIsReplacedOnReattachAndNotDisposedAgain()
    {
        UIRoot root = new();
        Border border = new();
        List<string> events = [];
        int attaches = 0;
        ElementAspect aspect = new(
            "ThrowingLifetime",
            typeof(UIElement),
            [],
            [],
            _ =>
            {
                int attach = ++attaches;
                events.Add($"create {attach}");
                // Records every Dispose call, so a second dispose of a lifetime shows up.
                return new EveryCallLifetime(() =>
                {
                    events.Add($"dispose {attach}");
                    if (attach == 1)
                    {
                        throw new InvalidOperationException("Dispose failed");
                    }
                });
            });
        border.Aspect = aspect;
        root.VisualChildren.Add(border);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(border));

        Assert.Equal("Dispose failed", failure.Message);
        Assert.Null(border.Root);
        Assert.Equal(["create 1", "dispose 1"], events);

        root.VisualChildren.Add(border);

        Assert.Equal(["create 1", "dispose 1", "create 2"], events);

        root.VisualChildren.Remove(border);

        Assert.Equal(["create 1", "dispose 1", "create 2", "dispose 2"], events);
    }

    [Fact]
    public void AspectAssignedByAnUnloadedHandlerGetsNoLifetimeUntilTheElementIsAttachedAgain()
    {
        UIRoot root = new();
        Border border = new();
        List<string> events = [];
        int creates = 0;
        ElementAspect other = new(
            "Other",
            typeof(UIElement),
            [],
            [],
            _ =>
            {
                int create = ++creates;
                events.Add($"create other {create}");
                return new EveryCallLifetime(() => events.Add($"dispose other {create}"));
            });
        bool assign = true;
        border.Unloaded += (_, _) =>
        {
            if (assign)
            {
                assign = false;
                border.Aspect = other;
            }
        };
        root.VisualChildren.Add(border);

        root.VisualChildren.Remove(border);

        Assert.Null(border.Root);
        Assert.Same(other, border.Aspect);
        Assert.Empty(events);

        root.VisualChildren.Add(border);

        Assert.Equal(["create other 1"], events);

        root.VisualChildren.Remove(border);

        Assert.Equal(["create other 1", "dispose other 1"], events);
    }

    // #849: an entry created from an AspectValue is an assignment like any other; SetValue compares with its value.
    [Theory]
    [InlineData("plainLiteral")]
    [InlineData("opacityAspectValue")]
    [InlineData("backgroundAspectValueToNull")]
    public void SetValueReplacesAnAssignmentThatWasCreatedFromAnAspectValue(string scenario)
    {
        SolidColorBrush initialBrush = new(Color.White);
        ElementAspectValue initial = scenario switch
        {
            "backgroundAspectValueToNull" => new ElementAspectValue(Control.BackgroundProperty, AspectValue<Brush?>.Literal(initialBrush)),
            "opacityAspectValue" => new ElementAspectValue(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f)),
            _ => new ElementAspectValue(UIElement.OpacityProperty, 0.5f)
        };
        Button button = new();
        UIRoot root = new();
        root.VisualChildren.Add(button);
        ElementAspect aspect = new([initial]);
        button.Aspect = aspect;
        root.ProcessFrame();

        bool changed = false;
        Exception? failure = Record.Exception(() => changed = scenario == "backgroundAspectValueToNull"
            ? aspect.SetValue(Control.BackgroundProperty, null)
            : aspect.SetValue(UIElement.OpacityProperty, 0.6f));

        Assert.Null(failure);
        Assert.True(changed);
        root.ProcessFrame();
        if (scenario == "backgroundAspectValueToNull")
        {
            Assert.Null(button.Background);
        }
        else
        {
            Assert.Equal(0.6f, button.Opacity);
        }
    }

    // #849: a value equal to the literal of an AspectValue entry is unchanged.
    [Fact]
    public void SetValueWithTheLiteralOfAnAspectValueEntryIsANoOp()
    {
        SolidColorBrush brush = new(Color.White);
        ElementAspect aspect = new(
        [
            new ElementAspectValue(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f)),
            new ElementAspectValue(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))
        ]);

        Assert.False(aspect.SetValue(UIElement.OpacityProperty, 0.5f));
        Assert.False(aspect.SetValue(Control.BackgroundProperty, brush));
        Assert.Equal(0, aspect.Version);
    }

    [Fact]
    public void AspectAssignedByALifetimeDisposedOnDetachGetsNoLifetimeUntilTheElementIsAttachedAgain()
    {
        UIRoot root = new();
        Border border = new();
        List<string> events = [];
        int otherCreates = 0;
        ElementAspect other = new(
            "Other",
            typeof(UIElement),
            [],
            [],
            _ =>
            {
                int create = ++otherCreates;
                events.Add($"create other {create}");
                return new EveryCallLifetime(() => events.Add($"dispose other {create}"));
            });
        int firstDisposes = 0;
        ElementAspect first = new(
            "First",
            typeof(UIElement),
            [],
            [],
            _ =>
            {
                events.Add("create first");
                // Records every Dispose call, so a second dispose of the lifetime shows up.
                return new EveryCallLifetime(() =>
                {
                    events.Add($"dispose first {++firstDisposes}");
                    if (firstDisposes == 1)
                    {
                        border.Aspect = other;
                    }
                });
            });
        border.Aspect = first;
        root.VisualChildren.Add(border);

        root.VisualChildren.Remove(border);

        Assert.Null(border.Root);
        Assert.Same(other, border.Aspect);
        Assert.Equal(["create first", "dispose first 1"], events);

        root.VisualChildren.Add(border);

        Assert.Equal(["create first", "dispose first 1", "create other 1"], events);

        root.VisualChildren.Remove(border);

        Assert.Equal(["create first", "dispose first 1", "create other 1", "dispose other 1"], events);
    }

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }

    private sealed class EveryCallLifetime(Action dispose) : IDisposable
    {
        public void Dispose()
        {
            dispose();
        }
    }
}
