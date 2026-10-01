using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ApplicationResourceIntegrationTests
{
    [Fact]
    public void GlobalResourceUpdatesAllRealConsumersAndReturnsToIdle()
    {
        Application application = new();
        SolidColorBrush initial = new(new Color(10, 20, 30));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), initial);

        UIRoot firstRoot = CreateRoot(application);
        UIRoot secondRoot = CreateRoot(application);
        Border first = CreateConsumer(firstRoot);
        Border second = CreateConsumer(secondRoot);
        Border unaffected = new();
        firstRoot.VisualChildren.Add(unaffected);
        firstRoot.ProcessFrame();
        secondRoot.ProcessFrame();

        SolidColorBrush updated = new(new Color(40, 50, 60));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), updated);

        Assert.Same(updated, first.Background);
        Assert.Same(updated, second.Background);
        Assert.False(unaffected.DirtyState.IsDirty);
        Assert.Contains(first, firstRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));
        Assert.Contains(second, secondRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));

        FrameStats firstUpdate = firstRoot.ProcessFrame();
        FrameStats secondUpdate = secondRoot.ProcessFrame();
        Assert.True(firstUpdate.RenderedElements > 0);
        Assert.True(secondUpdate.RenderedElements > 0);

        FrameStats firstIdle = firstRoot.ProcessFrame();
        FrameStats secondIdle = secondRoot.ProcessFrame();
        Assert.False(firstIdle.HasWork);
        Assert.False(secondIdle.HasWork);
    }

    [Fact]
    public void LocalResourceShadowsApplicationAndLaterRootsSeeLatestValue()
    {
        Application application = new();
        application.Resources.SetResource(
            new ResourceId<Brush>("Accent"),
            new SolidColorBrush(new Color(1, 2, 3)));
        UIRoot firstRoot = CreateRoot(application);
        Border shadowed = new();
        SolidColorBrush local = new(new Color(9, 8, 7));
        shadowed.Resources["Accent"] = local;
        GeneratedMarkup.AttachResource(
            shadowed,
            shadowed,
            Control.BackgroundProperty,
            "Accent",
            UiPropertyValueSource.MarkupBase);
        firstRoot.VisualChildren.Add(shadowed);

        SolidColorBrush latest = new(new Color(7, 8, 9));
        application.Resources.SetResource(new ResourceId<Brush>("Accent"), latest);

        Assert.Same(local, shadowed.Background);
        Assert.DoesNotContain(
            shadowed,
            firstRoot.ResourceDependencyTracker.GetDependents(new ResourceId<Brush>("Accent")));

        UIRoot laterRoot = CreateRoot(application);
        Border later = CreateConsumer(laterRoot);
        Assert.Same(latest, later.Background);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DisposedResourceAttachmentIgnoresPendingAndFutureProviderChanges(
        bool disposeBeforeNotification,
        bool changeAgainAfterDispose)
    {
        ResourceStore store = new();
        ResourceId<Brush?> id = new("DisposedAccent");
        SolidColorBrush initial = new(new Color(1, 2, 3));
        SolidColorBrush workerValue = new(new Color(4, 5, 6));
        SolidColorBrush laterValue = new(new Color(7, 8, 9));
        store.SetResource(id, initial);
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        Border border = new();
        using IDisposable attachment = GeneratedMarkup.AttachResource(
            border, border, Control.BackgroundProperty, id.Key, UiPropertyValueSource.MarkupBase);
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        Assert.Same(initial, border.Background);
        Assert.Equal(0, root.Relay.PendingCount);

        if (disposeBeforeNotification)
        {
            attachment.Dispose();
        }

        PublishOnWorker(() => store.SetResource(id, workerValue));

        // The root invalidation consumer always queues; the attachment queues only while subscribed.
        int expectedCallbacks = disposeBeforeNotification ? 1 : 2;
        Assert.Equal(expectedCallbacks, root.Relay.PendingCount);
        Assert.Same(initial, border.Background);
        attachment.Dispose();
        attachment.Dispose();
        Assert.Same(initial, border.Background);
        Assert.Equal(UiPropertyValueSource.MarkupBase, border.GetValueSource(Control.BackgroundProperty));
        if (changeAgainAfterDispose)
        {
            store.SetResource(id, laterValue);
        }

        Assert.Same(initial, border.Background);
        FrameStats frame = root.ProcessFrame();

        Assert.Equal(expectedCallbacks, frame.RelayExecutedCallbacks);
        Assert.Equal(0, root.Relay.PendingCount);
        Assert.Same(initial, border.Background);
        Assert.Equal(UiPropertyValueSource.MarkupBase, border.GetValueSource(Control.BackgroundProperty));
        PublishOnWorker(() => store.SetResource(id, new SolidColorBrush(new Color(10, 11, 12))));
        Assert.Equal(1, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Same(initial, border.Background);
    }

    [Fact]
    public void ActiveResourceAttachmentAppliesWorkerNotificationOnTheNextFrame()
    {
        ResourceStore store = new();
        ResourceId<Brush?> id = new("ActiveAccent");
        SolidColorBrush initial = new(new Color(1, 2, 3));
        SolidColorBrush updated = new(new Color(4, 5, 6));
        store.SetResource(id, initial);
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        Border border = new();
        using IDisposable attachment = GeneratedMarkup.AttachResource(
            border, border, Control.BackgroundProperty, id.Key, UiPropertyValueSource.MarkupBase);
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        PublishOnWorker(() => store.SetResource(id, updated));

        Assert.Same(initial, border.Background);
        Assert.Equal(2, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Same(updated, border.Background);
        Assert.Equal(0, root.Relay.PendingCount);
    }

    [Fact]
    public void ApplicationAspectAppliesToCodeCreatedDerivedControlAtCorrectPrecedence()
    {
        Application application = new();
        SolidColorBrush applicationBrush = new(new Color(12, 34, 56));
        application.Resources[typeof(Button)] = AspectPackage.Create("Application.Button")
            .Components(components => components.AddRule(new AspectRuleSet(
                "application.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(
                    Control.BackgroundProperty,
                    AspectValue<Brush?>.Literal(applicationBrush))],
                declarationOrder: 0)))
            .Build();
        UIRoot root = CreateRoot(application);
        DerivedButton button = new();

        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Same(applicationBrush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        SolidColorBrush localBrush = new(new Color(65, 43, 21));
        button.Background = localBrush;
        Assert.Same(localBrush, button.Background);
        Assert.Equal(UiPropertyValueSource.Local, button.GetValueSource(Control.BackgroundProperty));
    }

    private static UIRoot CreateRoot(Application application)
    {
        UIRoot root = new(100, 100);
        root.SetResourceProvider(application.Resources);
        return root;
    }

    private static void PublishOnWorker(Action publish)
    {
        Exception? failure = null;
        Thread worker = new(() =>
        {
            try
            {
                publish();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        { IsBackground = true };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
    }

    private static Border CreateConsumer(UIRoot root)
    {
        Border border = new();
        GeneratedMarkup.AttachResource(
            border,
            border,
            Control.BackgroundProperty,
            "Accent",
            UiPropertyValueSource.MarkupBase);
        root.VisualChildren.Add(border);
        return border;
    }

    private sealed class DerivedButton : Button
    {
    }
}
