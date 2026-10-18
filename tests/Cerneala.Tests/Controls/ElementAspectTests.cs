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

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }
}
