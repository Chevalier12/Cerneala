using System.Runtime.CompilerServices;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

internal static class ObjectMotionRuntime
{
    [ThreadStatic]
    private static ObjectMotionRuntimeState? current;

    public static ObjectMotionRuntimeState Current =>
        current ??= new ObjectMotionRuntimeState(new SystemMotionClock());

    public static void TickCurrent()
    {
        current?.Tick();
    }

    internal static void ResetForTests(IMotionClock? clock = null)
    {
        current = clock is null
            ? null
            : new ObjectMotionRuntimeState(clock);
    }
}

internal sealed class ObjectMotionRuntimeState
{
    private readonly IMotionClock clock;
    private TimeSpan? previousTimestamp;
    private int frameIndex;

    public ObjectMotionRuntimeState(IMotionClock clock)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Mixers = new ValueMixerRegistry();
        Mixers.RegisterBuiltIns();
        Graph = new MotionGraph(
            Mixers,
            ReducedMotionPolicy.Default);
        Bindings = new ObjectMotionBindingStore();
    }

    public MotionGraph Graph { get; }

    public ValueMixerRegistry Mixers { get; }

    public ObjectMotionBindingStore Bindings { get; }

    public void PrepareForAnimation()
    {
        Graph.VerifyAccess();
        previousTimestamp ??= clock.Now;
    }

    public void Tick()
    {
        Graph.VerifyAccess();
        if (!Graph.HasActiveMotion)
        {
            previousTimestamp = null;
            return;
        }

        TimeSpan now = clock.Now;
        TimeSpan delta = previousTimestamp is null
            ? TimeSpan.Zero
            : now - previousTimestamp.Value;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        previousTimestamp = now;
        frameIndex++;
        Graph.Tick(new MotionFrame(
            now,
            delta,
            frameIndex,
            MotionFrameReason.Scheduled,
            MotionFramePhase.BeforeRender));
        if (!Graph.HasActiveMotion)
        {
            previousTimestamp = null;
        }
    }
}

internal sealed class ObjectMotionBindingStore
{
    private readonly Dictionary<BindingKey, ObjectMotionBinding> bindings = [];

    public ObjectMotionBinding<TTarget, TValue> GetOrCreateBinding<TTarget, TValue>(
        ObjectMotionRuntimeState runtime,
        TTarget target,
        MotionProperty<TTarget, TValue> property)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        BindingKey key = new(target, property);
        if (bindings.TryGetValue(key, out ObjectMotionBinding? existing))
        {
            if (existing is ObjectMotionBinding<TTarget, TValue> typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Existing object Motion binding for '{property.Name}' has an incompatible type.");
        }

        ValueMixer<TValue> mixer = ResolveMixer(runtime, property);
        ObjectMotionBinding<TTarget, TValue> binding = new(
            this,
            runtime,
            target,
            property,
            mixer);
        bindings.Add(key, binding);
        return binding;
    }

    public void Remove(ObjectMotionBinding binding)
    {
        BindingKey key = new(binding.Target, binding.Property);
        if (bindings.TryGetValue(key, out ObjectMotionBinding? current) &&
            ReferenceEquals(current, binding))
        {
            bindings.Remove(key);
        }
    }

    private static ValueMixer<TValue> ResolveMixer<TTarget, TValue>(
        ObjectMotionRuntimeState runtime,
        MotionProperty<TTarget, TValue> property)
        where TTarget : class
    {
        if (property.Mixer is not null)
        {
            return property.Mixer;
        }

        if (property.IsDiscrete)
        {
            return ObjectDiscreteMixer<TValue>.Instance;
        }

        return runtime.Mixers.Resolve<TValue>(property.Name);
    }

    private readonly struct BindingKey : IEquatable<BindingKey>
    {
        public BindingKey(object target, object property)
        {
            Target = target;
            Property = property;
        }

        public object Target { get; }

        public object Property { get; }

        public bool Equals(BindingKey other) =>
            ReferenceEquals(Target, other.Target) &&
            ReferenceEquals(Property, other.Property);

        public override bool Equals(object? obj) =>
            obj is BindingKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            RuntimeHelpers.GetHashCode(Target),
            RuntimeHelpers.GetHashCode(Property));
    }
}

internal abstract class ObjectMotionBinding
{
    public abstract object Target { get; }

    public abstract object Property { get; }
}

internal sealed class ObjectMotionBinding<TTarget, TValue> :
    ObjectMotionBinding
    where TTarget : class
{
    private readonly ObjectMotionBindingStore store;
    private readonly ObjectMotionRuntimeState runtime;
    private readonly TTarget target;
    private readonly MotionProperty<TTarget, TValue> property;
    private readonly TValue baseValue;
    private readonly MotionValue<TValue> value;
    private readonly BindingNode node;
    private readonly IDisposable valueSubscription;
    private MotionHandle? activeHandle;
    private TValue pendingSample = default!;
    private bool hasPendingSample;
    private bool holdOnComplete;
    private bool completedNaturally;
    private bool finished;
    private bool disposed;

    public ObjectMotionBinding(
        ObjectMotionBindingStore store,
        ObjectMotionRuntimeState runtime,
        TTarget target,
        MotionProperty<TTarget, TValue> property,
        ValueMixer<TValue> mixer)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.property = property ?? throw new ArgumentNullException(nameof(property));
        ArgumentNullException.ThrowIfNull(mixer);
        baseValue = property.GetValue(target);
        value = runtime.Graph.CreateValue(baseValue, mixer);
        node = new BindingNode(this);
        valueSubscription = value.Subscribe(OnValueChanged);
    }

    public override object Target => target;

    public override object Property => property;

    public void JumpTo(TValue next)
    {
        ThrowIfDisposed();
        value.JumpTo(next);
        ApplyPendingSample();
    }

    public MotionHandle AnimateTo(
        TValue destination,
        MotionSpec<TValue> spec,
        MotionPropertyStartOptions options)
    {
        ThrowIfDisposed();
        runtime.Graph.VerifyAccess();
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(options);

        runtime.PrepareForAnimation();
        if (activeHandle is not null)
        {
            activeHandle.Completed -= OnMotionCompleted;
        }

        holdOnComplete = options.HoldOnComplete;
        completedNaturally = false;
        finished = false;
        MotionHandle handle = value.AnimateTo(
            destination,
            spec,
            options.ToMotionStartOptions());
        activeHandle = handle;
        if (handle.IsCompleted)
        {
            completedNaturally = true;
            finished = true;
            StageCurrent();
            Finish();
            return handle;
        }

        handle.Completed += OnMotionCompleted;
        StageCurrent();
        runtime.Graph.Unregister(node);
        runtime.Graph.Register(node);
        return handle;
    }

    private MotionNodeTickResult Tick()
    {
        if (disposed)
        {
            return new MotionNodeTickResult(Completed: true);
        }

        int writes = ApplyPendingSample();
        if (finished || activeHandle is null || !activeHandle.IsActive)
        {
            if (completedNaturally && !holdOnComplete)
            {
                writes += WriteIfChanged(baseValue);
            }

            DisposeBinding();
            return new MotionNodeTickResult(
                PropertyWrites: writes,
                Completed: true);
        }

        return new MotionNodeTickResult(PropertyWrites: writes);
    }

    private void Finish()
    {
        _ = ApplyPendingSample();
        if (completedNaturally && !holdOnComplete)
        {
            WriteIfChanged(baseValue);
        }

        runtime.Graph.Unregister(node);
        DisposeBinding();
    }

    private int ApplyPendingSample()
    {
        if (!hasPendingSample)
        {
            return 0;
        }

        TValue next = pendingSample;
        hasPendingSample = false;
        return WriteIfChanged(next);
    }

    private int WriteIfChanged(TValue next)
    {
        TValue current = property.GetValue(target);
        if (EqualityComparer<TValue>.Default.Equals(current, next))
        {
            return 0;
        }

        property.SetValue(target, next);
        return 1;
    }

    private void OnValueChanged(MotionValueChanged<TValue> change)
    {
        pendingSample = change.NewValue;
        hasPendingSample = true;
    }

    private void OnMotionCompleted(object? sender, MotionCompletedEventArgs args)
    {
        if (!ReferenceEquals(sender, activeHandle))
        {
            return;
        }

        activeHandle = null;
        completedNaturally = args.State == MotionCompletionState.Completed;
        finished = true;
    }

    private void StageCurrent()
    {
        pendingSample = value.Current;
        hasPendingSample = true;
    }

    private void DisposeBinding()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (activeHandle is not null)
        {
            activeHandle.Completed -= OnMotionCompleted;
        }
        valueSubscription.Dispose();
        store.Remove(this);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    private sealed class BindingNode(ObjectMotionBinding<TTarget, TValue> owner) :
        MotionNode
    {
        protected internal override MotionNodeTickResult Tick(MotionFrame frame) =>
            owner.Tick();
    }
}

internal sealed class ObjectDiscreteMixer<T> : ValueMixer<T>
{
    public static ObjectDiscreteMixer<T> Instance { get; } = new();

    public override T Mix(T from, T to, float progress) =>
        progress < 1 ? from : to;
}
