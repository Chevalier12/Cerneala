using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Markup;

public sealed class GeneratedMarkupMotionTests
{
    [Fact]
    public void TriggerSubscriptionIsUniqueAcrossAttachDetachCycles()
    {
        UIRoot root = new();
        EventElement element = new();
        int invocations = 0;
        EventHandler handler = (_, _) => invocations++;
        using IDisposable lifetime = GeneratedMarkup.AttachMotionTriggers(
            element,
            () => element.Fired += handler,
            () => element.Fired -= handler);

        element.RaiseFired();
        Assert.Equal(0, invocations);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            ElementLifecycle.AttachSubtree(root, element);
            element.RaiseFired();
            Assert.Equal(cycle + 1, invocations);
            ElementLifecycle.DetachSubtree(root, element);
            element.RaiseFired();
            Assert.Equal(cycle + 1, invocations);
        }
    }

    [Fact]
    public void ConditionActivationRunsOnlyOnBranchTransitionsAndStopsWhileDetached()
    {
        UIRoot root = new();
        UIElement element = new() { IsEnabled = false };
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(element, UIElement.IsEnabledProperty);
        int activations = 0;
        using IDisposable lifetime = GeneratedMarkup.AttachConditions(
            element,
            new[] { observation },
            new[]
            {
                new MarkupConditionRule(
                    0,
                    () => (bool)observation.Value!,
                    null,
                    null,
                    () => activations++)
            });

        ElementLifecycle.AttachSubtree(root, element);
        Assert.Equal(0, activations);
        element.IsEnabled = true;
        Assert.Equal(1, activations);
        element.IsEnabled = true;
        Assert.Equal(1, activations);

        ElementLifecycle.DetachSubtree(root, element);
        element.IsEnabled = false;
        element.IsEnabled = true;
        Assert.Equal(1, activations);

        ElementLifecycle.AttachSubtree(root, element);
        Assert.Equal(2, activations);
    }

    [Fact]
    public void DetachCancelsOnlyTheOwningMotionSession()
    {
        UIRoot root = new();
        UIElement first = new();
        UIElement second = new();
        using IDisposable firstSession = GeneratedMarkup.AttachMotionSession(first);
        using IDisposable secondSession = GeneratedMarkup.AttachMotionSession(second);
        ElementLifecycle.AttachSubtree(root, first);
        ElementLifecycle.AttachSubtree(root, second);

        MotionGroupHandle firstGroup = StartOpacity(firstSession, first, 0.25f);
        MotionGroupHandle secondGroup = StartOpacity(secondSession, second, 0.75f);

        ElementLifecycle.DetachSubtree(root, first);
        root.ProcessFrame();

        Assert.True(firstGroup.IsCanceled);
        Assert.False(secondGroup.IsCanceled);
        Assert.True(root.Motion.HasActiveMotion);

        ElementLifecycle.DetachSubtree(root, second);
        root.ProcessFrame();
        Assert.True(secondGroup.IsCanceled);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void MotionSessionLeavesNoActiveMotionAcrossOneHundredAttachDetachCycles()
    {
        UIRoot root = new();
        UIElement element = new();
        using IDisposable session = GeneratedMarkup.AttachMotionSession(element);

        for (int cycle = 0; cycle < 100; cycle++)
        {
            ElementLifecycle.AttachSubtree(root, element);
            MotionGroupHandle group = StartOpacity(session, element, cycle % 2 == 0 ? 0.2f : 0.8f);
            Assert.True(root.Motion.HasActiveMotion);

            ElementLifecycle.DetachSubtree(root, element);
            root.ProcessFrame();

            Assert.True(group.IsCanceled);
            Assert.False(root.Motion.HasActiveMotion);
        }
    }

    private static MotionGroupHandle StartOpacity(IDisposable session, UIElement element, float destination)
    {
        return GeneratedMarkup.StartMotion(
            session,
            () =>
            [
                GeneratedMarkup.StartMotionProperty(
                    session,
                    element,
                    UIElement.OpacityProperty,
                    false,
                    default,
                    false,
                    destination,
                    MotionFactory.Tween<float>(TimeSpan.FromSeconds(10)),
                    new MotionPropertyStartOptions())
            ]);
    }

    private sealed class EventElement : UIElement
    {
        public event EventHandler? Fired;

        public void RaiseFired()
        {
            Fired?.Invoke(this, EventArgs.Empty);
        }
    }
}
