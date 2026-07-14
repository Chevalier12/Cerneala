using System.ComponentModel;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiRelayStressAndAllocationTests
{
    [Fact]
    public void ConcurrentInvokeStressAccountsForOneHundredThousandItemsExactlyOnce()
    {
        const int itemCount = 100_000;
        const int producerCount = 8;
        UIRoot root = new();
        Task[] completions = new Task[itemCount];
        CancellationTokenSource?[] cancellations = new CancellationTokenSource?[itemCount];
        int executed = 0;
        Thread[] producers = Enumerable.Range(0, producerCount)
            .Select(producer => new Thread(() =>
            {
                for (int index = producer; index < itemCount; index += producerCount)
                {
                    if (index % 10 == 0)
                    {
                        CancellationTokenSource cancellation = new();
                        cancellations[index] = cancellation;
                        completions[index] = root.Relay.InvokeAsync(
                            () => Interlocked.Increment(ref executed),
                            cancellation.Token);
                    }
                    else if (index % 10 == 1)
                    {
                        completions[index] = root.Relay.InvokeAsync(() =>
                        {
                            Interlocked.Increment(ref executed);
                            throw new ExpectedRelayException();
                        });
                    }
                    else
                    {
                        completions[index] = root.Relay.InvokeAsync(
                            () => Interlocked.Increment(ref executed));
                    }
                }
            }))
            .ToArray();

        foreach (Thread producer in producers)
        {
            producer.Start();
        }

        foreach (Thread producer in producers)
        {
            Assert.True(producer.Join(TimeSpan.FromSeconds(30)), "Relay producer did not finish.");
        }

        int accepted = root.Relay.PendingCount;
        foreach (CancellationTokenSource cancellation in cancellations.Where(item => item is not null).Cast<CancellationTokenSource>())
        {
            cancellation.Cancel();
        }

        while (root.Relay.HasPendingWork)
        {
            root.ProcessFrame();
        }

        int canceled = completions.Count(task => task.IsCanceled);
        int faulted = completions.Count(task => task.IsFaulted);
        int completed = completions.Count(task => task.Status == TaskStatus.RanToCompletion);

        Assert.Equal(itemCount, accepted);
        Assert.Equal(itemCount, Volatile.Read(ref executed) + canceled);
        Assert.Equal(10_000, canceled);
        Assert.Equal(10_000, faulted);
        Assert.Equal(80_000, completed);
        Assert.Equal(0, root.Relay.PendingCount);
        foreach (CancellationTokenSource cancellation in cancellations.Where(item => item is not null).Cast<CancellationTokenSource>())
        {
            cancellation.Dispose();
        }
    }

    [Fact]
    public void BindingBurstStressStaysBoundedAcrossDetachAndRootReplacement()
    {
        const int cycleCount = 100;
        const int notificationsPerCycle = 1_000;
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        ThreadedSource source = new();
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Value", owner => ((ThreadedSource)owner!).Value));
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "stress binding");
        firstRoot.VisualChildren.Add(target);
        UIRoot activeRoot = firstRoot;
        UIRoot nextRoot = secondRoot;

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            Thread[] producers = Enumerable.Range(0, 4)
                .Select(producer => new Thread(() =>
                {
                    for (int index = producer; index < notificationsPerCycle; index += 4)
                    {
                        source.Set($"{cycle}:{index}");
                    }
                }))
                .ToArray();
            foreach (Thread producer in producers)
            {
                producer.Start();
            }

            foreach (Thread producer in producers)
            {
                Assert.True(producer.Join(TimeSpan.FromSeconds(10)), "Binding producer did not finish.");
            }

            Assert.Equal(1, activeRoot.Relay.PendingCount);
            activeRoot.VisualChildren.Remove(target);
            nextRoot.VisualChildren.Add(target);
            string expected = source.Value;
            Assert.Equal(expected, target.Text);

            activeRoot.ProcessFrame();
            Assert.Equal(expected, target.Text);
            Assert.Equal(0, activeRoot.Relay.PendingCount);
            (activeRoot, nextRoot) = (nextRoot, activeRoot);
        }

        Assert.Equal(0, source.WorkerReads);
    }

    [Fact]
    public void RelayIdleAndCounterReadsAllocateNothingAfterWarmup()
    {
        UIRoot root = new();
        SynchronizationContext? originalContext = SynchronizationContext.Current;
        for (int index = 0; index < 100; index++)
        {
            _ = root.Relay.HasPendingWork;
            _ = root.Relay.PendingCount;
            _ = root.Relay.Drain();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = 0;
        for (int index = 0; index < 10_000; index++)
        {
            if (root.Relay.HasPendingWork)
            {
                count++;
            }

            count += root.Relay.PendingCount;
        }

        for (int index = 0; index < 1_000; index++)
        {
            _ = root.Relay.Drain();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, count);
        Assert.Equal(0, allocated);
        Assert.Same(originalContext, SynchronizationContext.Current);
    }

    private sealed class ThreadedSource : INotifyPropertyChanged
    {
        private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
        private string value = "initial";
        private int workerReads;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get
            {
                if (Environment.CurrentManagedThreadId != ownerThreadId)
                {
                    Interlocked.Increment(ref workerReads);
                }

                return Volatile.Read(ref value);
            }
        }

        public int WorkerReads => Volatile.Read(ref workerReads);

        public void Set(string next)
        {
            Volatile.Write(ref value, next);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    private sealed class ExpectedRelayException : Exception
    {
    }
}
