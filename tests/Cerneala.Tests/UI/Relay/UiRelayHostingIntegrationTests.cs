using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Relay;
using Cerneala.UI.Rendering;
using Cerneala.Tests.UI.Hosting;
using System.Reflection;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiRelayHostingIntegrationTests
{
    [Fact]
    public void StandaloneFrameDrainsOneSnapshotAndCountsDispatch()
    {
        UIRoot root = new();
        List<int> order = [];
        root.Relay.Post(() =>
        {
            order.Add(1);
            root.Relay.Post(() => order.Add(2));
        });

        FrameStats first = root.ProcessFrame();

        Assert.Equal([1], order);
        Assert.Equal(1, first.RelaySnapshotCallbacks);
        Assert.Equal(1, first.RelayDequeuedCallbacks);
        Assert.Equal(1, first.RelayExecutedCallbacks);
        Assert.Equal(1, first.RelayDeferredCallbacks);
        Assert.Equal(1, first.RelayBacklog);
        Assert.Equal(0, first.NoWorkFrames);
        Assert.True(first.HasWork);

        FrameStats second = root.ProcessFrame();
        Assert.Equal([1, 2], order);
        Assert.Equal(1, second.RelayExecutedCallbacks);
    }

    [Fact]
    public void StandaloneFrameCountsFireAndForgetFaultBeforeRethrowing()
    {
        UIRoot root = new();
        FrameStats stats = new();
        root.Relay.Post(() => throw new InvalidOperationException("expected"));

        Assert.Throws<AggregateException>(() => root.ProcessFrame(stats: stats));

        Assert.Equal(1, stats.RelayDequeuedCallbacks);
        Assert.Equal(1, stats.RelayFaultedCallbacks);
        Assert.True(stats.HasWork);
    }

    [Fact]
    public void HostProcessesRelayInvalidationInTheSameUpdate()
    {
        UIRoot root = new();
        RenderCountingElement child = new();
        root.VisualChildren.Add(child);
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int rendersBefore = child.RenderCount;
        root.Relay.Post(() => child.Invalidate(InvalidationFlags.Render, "Relay update"));

        UiFrame frame = host.Update(
            FakeInputSource.CreateFrame(),
            new UiViewport(100, 100),
            TimeSpan.Zero);

        Assert.Equal(1, frame.Stats.RelayExecutedCallbacks);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.Equal(rendersBefore + 1, child.RenderCount);
    }

    [Fact]
    public void HostDrainsOnlyOnceWhenCallbackReposts()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        int executions = 0;
        root.Relay.Post(() =>
        {
            executions++;
            root.Relay.Post(() => executions++);
        });

        UiFrame first = host.Update(FakeInputSource.CreateFrame(), elapsedTime: TimeSpan.Zero);

        Assert.Equal(1, executions);
        Assert.Equal(1, first.Stats.RelayExecutedCallbacks);
        Assert.Equal(1, first.Stats.RelayBacklog);

        UiFrame second = host.Update(FakeInputSource.CreateFrame(), elapsedTime: TimeSpan.Zero);
        Assert.Equal(2, executions);
        Assert.Equal(1, second.Stats.RelayExecutedCallbacks);
    }

    [Fact]
    public void WorkPostedByInputWaitsForTheNextUpdate()
    {
        UIRoot root = new(100, 100);
        RepeatButton button = new();
        root.VisualChildren.Add(button);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(100, 100)
        });
        host.Update(FakeInputSource.CreateFrame(), elapsedTime: TimeSpan.Zero);
        int executions = 0;
        button.Click += (_, _) => root.Relay.Post(() => executions++);
        float x = button.ArrangedBounds.X + (button.ArrangedBounds.Width / 2);
        float y = button.ArrangedBounds.Y + (button.ArrangedBounds.Height / 2);

        UiFrame inputFrame = host.Update(PointerFrame(x, y, false, true), elapsedTime: TimeSpan.Zero);

        Assert.Equal(0, executions);
        Assert.Equal(0, inputFrame.Stats.RelayExecutedCallbacks);
        Assert.True(root.Relay.HasPendingWork);

        host.Update(PointerFrame(x, y, true, true), elapsedTime: TimeSpan.Zero);
        Assert.Equal(1, executions);
    }

    [Fact]
    public void RootReplacementPumpsOnlyTheCurrentRootAndKeepsOldRelayUsable()
    {
        UIRoot oldRoot = new();
        UIRoot newRoot = new();
        UiHost host = new(new UiHostOptions { Root = oldRoot });
        int oldExecutions = 0;
        int newExecutions = 0;
        oldRoot.Relay.Post(() => oldExecutions++);
        newRoot.Relay.Post(() => newExecutions++);

        host.SetRoot(newRoot);
        UiFrame frame = host.Update(
            FakeInputSource.CreateFrame(),
            new UiViewport(100, 100),
            TimeSpan.Zero);

        Assert.Same(newRoot.Relay, host.Relay);
        Assert.Equal(0, oldExecutions);
        Assert.Equal(1, newExecutions);
        Assert.True(oldRoot.Relay.HasPendingWork);
        Assert.Equal(1, frame.Stats.RelayExecutedCallbacks);

        oldRoot.ProcessFrame();
        Assert.Equal(1, oldExecutions);
    }

    [Fact]
    public void RootReplacementRejectsRootOwnedByAnotherThreadWithoutChangingHost()
    {
        UIRoot oldRoot = new();
        UiHost host = new(new UiHostOptions { Root = oldRoot });
        UIRoot? workerRoot = null;
        Thread worker = new(() => workerRoot = new UIRoot());
        worker.Start();
        worker.Join();

        Assert.Throws<InvalidOperationException>(() => host.SetRoot(workerRoot!));
        Assert.Same(oldRoot, host.Root);
    }

    [Fact]
    public void HostOperationsRejectOffThreadBeforeUpdateDrawOrRootReplacementWork()
    {
        UIRoot root = new();
        UIRoot nextRoot = new();
        UiHost host = new(new UiHostOptions { Root = root });
        FakeDrawingBackend backend = new();
        Exception? updateException = null;
        Exception? drawException = null;
        Exception? setRootException = null;
        Thread worker = new(() =>
        {
            updateException = Record.Exception(() =>
            {
                _ = host.Update(
                    FakeInputSource.CreateFrame(),
                    new UiViewport(100, 100),
                    TimeSpan.Zero);
            });
            drawException = Record.Exception(() => host.Draw(backend));
            setRootException = Record.Exception(() => host.SetRoot(nextRoot));
        });

        worker.Start();
        worker.Join();

        Assert.IsType<InvalidOperationException>(updateException);
        Assert.IsType<InvalidOperationException>(drawException);
        Assert.IsType<InvalidOperationException>(setRootException);
        Assert.Same(root, host.Root);
        Assert.Null(host.LastFrame);
        Assert.Equal(0, backend.RenderCalls);
    }

    [Fact]
    public void HostsExposeTheRootOwnedRelayWithoutDuplicatingOwnership()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        UiHost detachedHost = new();
        NullabilityInfoContext nullability = new();

        Assert.Same(root.Relay, host.Relay);
        Assert.Null(detachedHost.Relay);
        Assert.Equal(
            typeof(UiRelay),
            typeof(MonoGameUiHost).GetProperty(nameof(MonoGameUiHost.Relay))!.PropertyType);
        Assert.Equal(
            NullabilityState.Nullable,
            nullability.Create(typeof(UiHost).GetProperty(nameof(UiHost.Relay))!).ReadState);
        Assert.Equal(
            NullabilityState.Nullable,
            nullability.Create(typeof(MonoGameUiHost).GetProperty(nameof(MonoGameUiHost.Relay))!).ReadState);
    }

    [Fact]
    public void IdleUpdateKeepsRelayCountersAtZeroAndReportsNoWork()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame idle = host.Update(FakeInputSource.CreateFrame(), elapsedTime: TimeSpan.Zero);

        Assert.Equal(0, idle.Stats.RelaySnapshotCallbacks);
        Assert.Equal(0, idle.Stats.RelayDequeuedCallbacks);
        Assert.Equal(0, idle.Stats.RelayBacklog);
        Assert.Equal(1, idle.Stats.NoWorkFrames);
    }

    [Fact]
    public void IdlePendingCheckDoesNotAllocateOrStartTheScheduler()
    {
        UIRoot root = new();
        _ = root.Relay.HasPendingWork;
        long before = GC.GetAllocatedBytesForCurrentThread();
        bool pending = false;

        for (int i = 0; i < 10_000; i++)
        {
            pending |= root.Relay.HasPendingWork;
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.False(pending);
        Assert.Equal(0, allocated);
        Assert.False(root.Scheduler.HasWork);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown, bool currentDown)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(
                new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1),
                Color.White);
        }
    }
}
