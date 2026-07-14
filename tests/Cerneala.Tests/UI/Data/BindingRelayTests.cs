using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Data;

public sealed class BindingRelayTests
{
    [Fact]
    public void AttachedMarkupBindingDefersWorkerReadAndCoalescesBurst()
    {
        UIRoot root = new();
        ThreadedTextSource source = new("initial");
        TextBlock target = AttachTextBinding(root, source);
        source.ResetReads();

        Exception? fault = RunWorker(() =>
        {
            for (int i = 0; i < 10_000; i++)
            {
                source.Set($"value-{i}");
            }
        });

        Assert.Null(fault);
        Assert.Equal("initial", target.Text);
        Assert.Equal(0, source.WorkerReads);
        Assert.Equal(1, root.Relay.PendingCount);

        root.ProcessFrame();

        Assert.Equal("value-9999", target.Text);
        Assert.Equal(0, source.WorkerReads);
        Assert.Equal(1, source.OwnerReads);
    }

    [Fact]
    public void MultipleProducersStillPublishLatestStateWithOnePendingRefresh()
    {
        UIRoot root = new();
        ThreadedTextSource source = new("initial");
        TextBlock target = AttachTextBinding(root, source);
        source.ResetReads();
        Thread[] producers = Enumerable.Range(0, 4)
            .Select(producer => new Thread(() =>
            {
                for (int index = 0; index < 2_500; index++)
                {
                    source.Set($"{producer}:{index}");
                }
            }))
            .ToArray();

        foreach (Thread producer in producers)
        {
            producer.Start();
        }

        foreach (Thread producer in producers)
        {
            Assert.True(producer.Join(TimeSpan.FromSeconds(10)));
        }

        Assert.Null(RunWorker(() => source.Set("final")));
        Assert.Equal(1, root.Relay.PendingCount);
        root.ProcessFrame();

        Assert.Equal("final", target.Text);
        Assert.Equal(0, source.WorkerReads);
        Assert.Equal(1, source.OwnerReads);
    }

    [Fact]
    public void NotificationDuringRefreshQueuesNextUpdateWithoutLosingLatestValue()
    {
        UIRoot root = new();
        RacingTextSource source = new("initial");
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Value", owner => ((RacingTextSource)owner!).Value));
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "racing path");
        root.VisualChildren.Add(target);

        Assert.Null(RunWorker(() => source.Set("stale")));
        source.ArmReadBarrier();
        Thread producer = new(() =>
        {
            source.WaitForRead();
            source.Set("final");
            source.ReleaseRead();
        });
        producer.Start();

        root.ProcessFrame();
        Assert.True(producer.Join(TimeSpan.FromSeconds(10)));
        Assert.Equal("stale", target.Text);
        Assert.Equal(1, root.Relay.PendingCount);

        root.ProcessFrame();
        Assert.Equal("final", target.Text);
    }

    [Fact]
    public void NestedPathReconnectsOnUiThreadAndIgnoresOldBranch()
    {
        UIRoot root = new();
        ThreadedChild oldChild = new("old");
        ThreadedRoot source = new(oldChild);
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Child", owner => ((ThreadedRoot)owner!).Child),
            new MarkupDataPathSegment("Name", owner => ((ThreadedChild)owner!).Name));
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "nested path");
        root.VisualChildren.Add(target);
        ThreadedChild replacement = new("replacement");

        Assert.Null(RunWorker(() => source.Child = replacement));
        Assert.Equal("old", target.Text);
        root.ProcessFrame();
        Assert.Equal("replacement", target.Text);

        Assert.Null(RunWorker(() => oldChild.Name = "stale"));
        Assert.Equal(0, root.Relay.PendingCount);
        Assert.Null(RunWorker(() => replacement.Name = "current"));
        root.ProcessFrame();
        Assert.Equal("current", target.Text);
    }

    [Fact]
    public void InterpolationAndConditionsCoalescePerCompositeController()
    {
        UIRoot root = new();
        ThreadedTextSource first = new("A");
        ThreadedTextSource second = new("B");
        TextBlock target = new();
        MarkupObservation firstObservation = GeneratedMarkup.ObserveObject(() => first);
        MarkupObservation secondObservation = GeneratedMarkup.ObserveObject(() => second);
        int composeCount = 0;
        using Binding interpolation = GeneratedMarkup.AttachInterpolatedStringBinding(
            target,
            target,
            TextBlock.TextProperty,
            [firstObservation, secondObservation],
            () =>
            {
                composeCount++;
                return $"{first.Value}:{second.Value}";
            },
            "composite");

        FlagSource a = new(false);
        FlagSource b = new(false);
        FlagSource c = new(false);
        MarkupObservation aObservation = GeneratedMarkup.ObserveObject(() => a);
        MarkupObservation bObservation = GeneratedMarkup.ObserveObject(() => b);
        MarkupObservation cObservation = GeneratedMarkup.ObserveObject(() => c);
        MarkupConditionalValue conditional = new(
            target,
            TextBlock.TextProperty,
            "condition",
            UiPropertyValueSource.MarkupConditional);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(
            target,
            [aObservation, bObservation, cObservation],
            [new MarkupConditionRule(0, () => (a.Value && b.Value) || c.Value, [conditional])]);
        root.VisualChildren.Add(target);
        composeCount = 0;

        Assert.Null(RunWorker(() =>
        {
            first.Set("X");
            second.Set("Y");
            a.Value = true;
            b.Value = true;
            c.Value = true;
        }));

        Assert.Equal(2, root.Relay.PendingCount);
        root.ProcessFrame();
        Assert.Equal(1, composeCount);
        Assert.Equal("condition", target.Text);
    }

    [Fact]
    public void DetachDisposeAndReattachDiscardStaleCallbacks()
    {
        UIRoot root = new();
        ThreadedTextSource source = new("initial");
        TextBlock target = new();
        MarkupObservation observation = GeneratedMarkup.ObserveObject(() => source);
        Binding binding = GeneratedMarkup.AttachInterpolatedStringBinding(
            target,
            target,
            TextBlock.TextProperty,
            [observation],
            () => source.Value,
            "lifecycle");
        root.VisualChildren.Add(target);

        Assert.Null(RunWorker(() => source.Set("queued")));
        root.VisualChildren.Remove(target);
        root.ProcessFrame();
        Assert.Equal("initial", target.Text);

        root.VisualChildren.Add(target);
        Assert.Equal("queued", target.Text);
        Assert.Null(RunWorker(() => source.Set("disposed")));
        binding.Dispose();
        root.ProcessFrame();
        Assert.Equal(string.Empty, target.Text);
    }

    [Fact]
    public void ObservableValueUsesAutomaticOrExplicitRelayAndTwoWayStaysImmediate()
    {
        UIRoot root = new();
        TextBox attached = new();
        root.VisualChildren.Add(attached);
        ObservableValue<string> attachedSource = new("one");
        using UiPropertyBinding<string> attachedBinding = BindingOperations.BindTwoWay(
            attached,
            TextBox.TextProperty,
            attachedSource);

        attached.Text = "local";
        Assert.Equal("local", attachedSource.Value);
        Assert.Null(RunWorker(() => attachedSource.Value = "worker"));
        Assert.Equal("local", attached.Text);
        root.ProcessFrame();
        Assert.Equal("worker", attached.Text);

        UiObject generic = new();
        ObservableValue<int> genericSource = new(1);
        UiProperty<int> valueProperty = UiProperty<int>.Register(
            "Value",
            typeof(BindingRelayTests),
            new UiPropertyMetadata<int>(0));
        using UiPropertyBinding<int> genericBinding = BindingOperations.BindOneWay(
            generic,
            valueProperty,
            genericSource,
            root.Relay);
        Assert.Null(RunWorker(() => genericSource.Value = 2));
        root.ProcessFrame();
        Assert.Equal(2, generic.GetValue(valueProperty));
    }

    [Fact]
    public void ProgrammaticBindingFailsWithoutRelayAndRejectsAttachMismatchBeforeTreeChange()
    {
        UiObject generic = new();
        ObservableValue<int> source = new(1);
        UiProperty<int> valueProperty = UiProperty<int>.Register(
            "DetachedValue",
            typeof(BindingRelayTests),
            new UiPropertyMetadata<int>(0));
        using UiPropertyBinding<int> binding = BindingOperations.BindOneWay(generic, valueProperty, source);

        Exception failure = Assert.IsType<InvalidOperationException>(RunWorker(() => source.Value = 2));
        Assert.Contains("Relay.Post", failure.Message, StringComparison.Ordinal);
        Assert.Equal(1, generic.GetValue(valueProperty));

        UIRoot first = new();
        UIRoot second = new();
        UIElement target = new();
        ObservableValue<float> opacity = new(0.5f);
        using UiPropertyBinding<float> explicitBinding = BindingOperations.BindOneWay(
            target,
            UIElement.OpacityProperty,
            opacity,
            first.Relay);

        Assert.Throws<InvalidOperationException>(() => second.VisualChildren.Add(target));
        Assert.Null(target.Root);
        Assert.Empty(second.VisualChildren);
    }

    [Fact]
    public void DisposedBindingIsCollectibleAfterQueuedRefreshDrains()
    {
        UIRoot root = new();
        WeakReference binding = CreateDisposedQueuedBinding(root);

        root.ProcessFrame();
        ForceCollection();

        Assert.False(binding.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedQueuedBinding(UIRoot root)
    {
        ThreadedTextSource source = new("initial");
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Value", owner => ((ThreadedTextSource)owner!).Value));
        Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "collectible binding");
        root.VisualChildren.Add(target);
        Assert.Null(RunWorker(() => source.Set("queued")));
        Assert.Equal(1, root.Relay.PendingCount);

        WeakReference reference = new(binding);
        binding.Dispose();
        return reference;
    }

    private static void ForceCollection()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static TextBlock AttachTextBinding(UIRoot root, ThreadedTextSource source)
    {
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Value", owner => ((ThreadedTextSource)owner!).Value));
        _ = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "threaded text");
        root.VisualChildren.Add(target);
        return target;
    }

    private static Exception? RunWorker(Action action)
    {
        Exception? exception = null;
        Thread worker = new(() => exception = Record.Exception(action));
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Worker did not finish.");
        return exception;
    }

    private sealed class ThreadedTextSource : INotifyPropertyChanged
    {
        private string value;
        private int workerReads;
        private int ownerReads;
        private readonly int ownerThreadId = Environment.CurrentManagedThreadId;

        public ThreadedTextSource(string value)
        {
            this.value = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get
            {
                if (Environment.CurrentManagedThreadId == ownerThreadId)
                {
                    Interlocked.Increment(ref ownerReads);
                }
                else
                {
                    Interlocked.Increment(ref workerReads);
                }

                return Volatile.Read(ref value);
            }
        }

        public int WorkerReads => Volatile.Read(ref workerReads);

        public int OwnerReads => Volatile.Read(ref ownerReads);

        public void Set(string next)
        {
            Volatile.Write(ref value, next);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        public void ResetReads()
        {
            workerReads = 0;
            ownerReads = 0;
        }
    }

    private sealed class ThreadedRoot : INotifyPropertyChanged
    {
        private ThreadedChild child;

        public ThreadedRoot(ThreadedChild child)
        {
            this.child = child;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ThreadedChild Child
        {
            get => child;
            set
            {
                child = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
            }
        }
    }

    private sealed class RacingTextSource : INotifyPropertyChanged
    {
        private readonly ManualResetEventSlim readStarted = new(false);
        private readonly ManualResetEventSlim releaseRead = new(false);
        private string value;
        private int armed;

        public RacingTextSource(string value)
        {
            this.value = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get
            {
                string snapshot = Volatile.Read(ref value);
                if (Interlocked.Exchange(ref armed, 0) != 0)
                {
                    readStarted.Set();
                    Assert.True(releaseRead.Wait(TimeSpan.FromSeconds(10)));
                }

                return snapshot;
            }
        }

        public void Set(string next)
        {
            Volatile.Write(ref value, next);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        public void ArmReadBarrier()
        {
            Interlocked.Exchange(ref armed, 1);
        }

        public void WaitForRead()
        {
            Assert.True(readStarted.Wait(TimeSpan.FromSeconds(10)));
        }

        public void ReleaseRead()
        {
            releaseRead.Set();
        }
    }

    private sealed class ThreadedChild : INotifyPropertyChanged
    {
        private string name;

        public ThreadedChild(string name)
        {
            this.name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    private sealed class FlagSource : INotifyPropertyChanged
    {
        private bool value;

        public FlagSource(bool value)
        {
            this.value = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
