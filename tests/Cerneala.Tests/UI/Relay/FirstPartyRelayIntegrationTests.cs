using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Ink;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Relay;

public sealed class FirstPartyRelayIntegrationTests
{
    [Fact]
    public void CanExecuteChangedCoalescesAndDiscardsInactiveSources()
    {
        UIRoot root = new();
        bool canExecute = true;
        ActionCommand first = new(_ => { }, _ => canExecute);
        Button button = new() { Command = first };
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Exception? fault = RunWorker(() =>
        {
            canExecute = false;
            for (int index = 0; index < 10_000; index++)
            {
                first.RaiseCanExecuteChanged();
            }
        });

        Assert.Null(fault);
        Assert.True(button.IsEnabled);
        Assert.Equal(1, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.False(button.IsEnabled);

        ActionCommand replacement = new(_ => { }, _ => true);
        button.Command = replacement;
        root.ProcessFrame();
        Assert.Null(RunWorker(first.RaiseCanExecuteChanged));
        Assert.Equal(0, root.Relay.PendingCount);

        Assert.Null(RunWorker(replacement.RaiseCanExecuteChanged));
        root.VisualChildren.Remove(button);
        root.ProcessFrame();
        Assert.True(button.IsEnabled);

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.True(button.IsEnabled);
    }

    [Fact]
    public void ThemeChangesCoalesceAndProviderReplacementInvalidatesQueuedWork()
    {
        UIRoot root = new();
        ThemeProvider first = new(new Theme("initial"));
        root.SetThemeProvider(first);
        root.ProcessFrame();

        Assert.Null(RunWorker(() =>
        {
            for (int index = 0; index < 10_000; index++)
            {
                first.Theme = new Theme($"theme-{index}");
            }
        }));
        Assert.Equal(1, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Equal("theme-9999", root.ThemeProvider!.Theme.Name);

        Assert.Null(RunWorker(() => first.Theme = new Theme("stale")));
        ThemeProvider replacement = new(new Theme("replacement"));
        root.SetThemeProvider(replacement);
        root.ProcessFrame();
        Assert.Same(replacement, root.ThemeProvider);
    }

    [Fact]
    public void ResourceChangesKeepFifoAndIgnoreReplacedProviders()
    {
        UIRoot root = new();
        ObservableProvider first = new();
        root.SetResourceProvider(first);
        root.ProcessFrame();

        Assert.Null(RunWorker(() =>
        {
            first.Raise("ordered", 1);
            first.Raise("ordered", 2);
        }));
        Assert.Equal(2, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Equal(2, root.ResourceDependencyTracker.GetResourceVersion(new ResourceId<string>("ordered")));

        Assert.Null(RunWorker(() => first.Raise("stale", 7)));
        ObservableProvider replacement = new();
        root.SetResourceProvider(replacement);
        Assert.Null(RunWorker(() => replacement.Raise("current", 1)));
        Assert.Equal(2, root.Relay.PendingCount);
        root.ProcessFrame();

        Assert.Equal(0, root.ResourceDependencyTracker.GetResourceVersion(new ResourceId<string>("stale")));
        Assert.Equal(1, root.ResourceDependencyTracker.GetResourceVersion(new ResourceId<string>("current")));
    }

    [Fact]
    public void ElementResourceChangesMarshalEachDeltaThroughRelay()
    {
        UIRoot root = new();
        TextBlock target = new();
        root.VisualChildren.Add(target);
        root.ProcessFrame();

        Assert.Null(RunWorker(() =>
        {
            target.Resources.Add("first", "one");
            target.Resources.Add("second", "two");
        }));

        Assert.Equal(2, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Equal("one", target.FindResource<string>("first"));
        Assert.Equal("two", target.FindResource<string>("second"));

        int resourceInvalidations = root.Trace.Entries.Count(entry => entry.Reason == "Element resources changed");
        Assert.Null(RunWorker(() => target.Resources.Add("stale", "value")));
        root.VisualChildren.Remove(target);
        root.VisualChildren.Add(target);
        root.ProcessFrame();
        Assert.Equal(
            resourceInvalidations,
            root.Trace.Entries.Count(entry => entry.Reason == "Element resources changed"));
    }

    [Fact]
    public async Task ObservableListRequiresExplicitRelayAndSupportsCancellation()
    {
        UIRoot root = new();
        ObservableList<string> items = new();
        ItemsControl control = new() { ItemsSource = items };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        Exception failure = Assert.IsType<InvalidOperationException>(RunWorker(() => items.Add("wrong thread")));
        Assert.Contains("Relay.InvokeAsync", failure.Message, StringComparison.Ordinal);

        Task mutation = root.Relay.InvokeAsync(() => items.Add("safe"));
        root.ProcessFrame();
        await mutation;
        Assert.Equal(2, items.Count);

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        Task canceled = root.Relay.InvokeAsync(() => items.Add("canceled"), cancellation.Token);
        root.ProcessFrame();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await canceled);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void UiOwnedCollectionsRejectWorkerNotificationsBeforeRetainedWork()
    {
        UIRoot root = new();
        ItemsControl items = new();
        InkCanvas ink = new();
        root.VisualChildren.Add(items);
        root.VisualChildren.Add(ink);
        root.ProcessFrame();

        Exception itemFailure = Assert.IsType<InvalidOperationException>(
            RunWorker(() => items.Items.Add("wrong thread")));
        Exception strokeFailure = Assert.IsType<InvalidOperationException>(
            RunWorker(() => ink.Strokes.Add(new Stroke())));

        Assert.Contains("Relay.InvokeAsync", itemFailure.Message, StringComparison.Ordinal);
        Assert.Contains("Relay.InvokeAsync", strokeFailure.Message, StringComparison.Ordinal);
        Assert.Equal(0, root.Relay.PendingCount);
        Assert.False(root.Scheduler.HasWork);
    }

    private static Exception? RunWorker(Action action)
    {
        Exception? exception = null;
        Thread worker = new(() => exception = Record.Exception(action));
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Worker did not finish.");
        return exception;
    }

    private sealed class ObservableProvider : IObservableResourceProvider
    {
        public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

        public bool TryGetResource<T>(ResourceId<T> id, out T resource)
        {
            resource = default!;
            return false;
        }

        public void Raise(string key, long version)
        {
            ResourceChanged?.Invoke(
                this,
                new ResourceChangedEventArgs(typeof(string), key, null, version.ToString(), version));
        }
    }
}
