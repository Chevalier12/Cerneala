using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionValue<T> : MotionValue
{
    private readonly MotionGraph graph;
    private readonly ValueMixer<T> mixer;
    private readonly ValueNode node;
    private readonly List<Action<MotionValueChanged<T>>> listeners = [];
    private MotionSampler<T>? sampler;
    private MotionHandle? activeHandle;
    private T current;
    private T target;
    private T animationStart;
    private TimeSpan animationElapsed;
    private MotionVelocity<T>? velocity;

    internal MotionValue(MotionGraph graph, ValueMixer<T> mixer, T initial)
    {
        this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
        this.mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
        current = initial;
        target = initial;
        animationStart = initial;
        node = new ValueNode(this);
    }

    public T Current => current;

    public T Target => target;

    public bool IsAnimating => sampler is not null && activeHandle?.IsActive == true;

    public MotionVelocity<T>? Velocity => velocity;

    internal override Type ValueType => typeof(T);

    public MotionHandle AnimateTo(T target, MotionSpec<T> spec, MotionStartOptions? options = null)
    {
        graph.VerifyAccess();
        ArgumentNullException.ThrowIfNull(spec);
        MotionStartOptions effectiveOptions = options ?? MotionStartOptions.Default;

        if (sampler is not null &&
            activeHandle?.IsActive == true &&
            effectiveOptions.RetargetMode == RetargetMode.PreserveProgress)
        {
            TimeSpan preservedElapsed = animationElapsed;
            activeHandle.FinishCanceled(MotionCancelBehavior.KeepCurrent, fireEvent: true);
            activeHandle = null;
            this.target = target;
            animationStart = current;
            sampler = spec.CreateSampler(
                current,
                target,
                mixer,
                graph.CreateSpecContext(effectiveOptions.DebugName));
            if (preservedElapsed > TimeSpan.Zero)
            {
                sampler.Advance(preservedElapsed);
            }

            animationElapsed = preservedElapsed;

            MotionHandle retargetedHandle = CreateHandle();
            activeHandle = retargetedHandle;

            graph.Register(node);
            return retargetedHandle;
        }

        CancelActiveHandle(MotionCancelBehavior.KeepCurrent, fireEvent: true);
        this.target = target;
        animationStart = current;
        animationElapsed = TimeSpan.Zero;
        sampler = spec.CreateSampler(
            current,
            target,
            mixer,
            graph.CreateSpecContext(effectiveOptions.DebugName));

        MotionHandle handle = CreateHandle();
        activeHandle = handle;

        if (sampler.IsComplete)
        {
            ApplySample(sampler.Current);
            FinishNaturalCompletion();
            return handle;
        }

        graph.Register(node);
        return handle;
    }

    public void JumpTo(T value)
    {
        graph.VerifyAccess();
        CancelActiveHandle(MotionCancelBehavior.KeepCurrent, fireEvent: true);
        target = value;
        animationStart = value;
        animationElapsed = TimeSpan.Zero;
        ApplySample(value);
    }

    public IDisposable Subscribe(Action<MotionValueChanged<T>> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        listeners.Add(listener);
        return new Subscription(listeners, listener);
    }

    internal int Advance(MotionFrame frame, out bool completed)
    {
        completed = false;
        if (sampler is null)
        {
            return 0;
        }

        animationElapsed += frame.Delta;
        sampler.Advance(frame.Delta);
        velocity = TryGetVelocity(sampler);
        int changed = ApplySample(sampler.Current) ? 1 : 0;
        if (sampler.IsComplete)
        {
            completed = true;
            FinishNaturalCompletion();
        }

        return changed;
    }

    private void CancelHandle(MotionHandle? handle, MotionCancelBehavior behavior, bool fireEvent)
    {
        if (handle is not null && !ReferenceEquals(handle, activeHandle))
        {
            return;
        }

        CancelActiveHandle(behavior, fireEvent);
    }

    private MotionHandle CreateHandle()
    {
        MotionHandle? handle = null;
        handle = new MotionHandle(
            behavior => CancelHandle(handle, behavior, fireEvent: true),
            () => CompleteHandle(handle),
            () => DisposeHandle(handle));
        return handle;
    }

    private void CompleteHandle(MotionHandle? handle)
    {
        if (handle is not null && !ReferenceEquals(handle, activeHandle))
        {
            return;
        }

        if (activeHandle is null)
        {
            return;
        }

        ApplySample(target);
        FinishHandle(MotionCompletionState.Completed, MotionCancelBehavior.Complete, fireEvent: true);
    }

    private void DisposeHandle(MotionHandle? handle)
    {
        if (handle is not null && !ReferenceEquals(handle, activeHandle))
        {
            return;
        }

        CancelActiveHandle(MotionCancelBehavior.KeepCurrent, fireEvent: false);
    }

    private void CancelActiveHandle(MotionCancelBehavior behavior, bool fireEvent)
    {
        if (activeHandle is null && sampler is null)
        {
            return;
        }

        switch (behavior)
        {
            case MotionCancelBehavior.KeepCurrent:
                target = current;
                break;
            case MotionCancelBehavior.Revert:
                target = animationStart;
                ApplySample(animationStart);
                break;
            case MotionCancelBehavior.Complete:
                ApplySample(target);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unknown motion cancel behavior.");
        }

        FinishHandle(MotionCompletionState.Canceled, behavior, fireEvent);
    }

    private void FinishNaturalCompletion()
    {
        ApplySample(target);
        FinishHandle(MotionCompletionState.Completed, MotionCancelBehavior.Complete, fireEvent: true);
    }

    private void FinishHandle(MotionCompletionState state, MotionCancelBehavior behavior, bool fireEvent)
    {
        sampler = null;
        animationElapsed = TimeSpan.Zero;
        velocity = null;
        graph.Unregister(node);

        MotionHandle? handle = activeHandle;
        activeHandle = null;
        if (handle is null)
        {
            return;
        }

        if (state == MotionCompletionState.Completed)
        {
            handle.FinishCompleted(fireEvent);
        }
        else
        {
            handle.FinishCanceled(behavior, fireEvent);
        }
    }

    private bool ApplySample(T value)
    {
        if (mixer.EqualsWithinTolerance(current, value, 0))
        {
            return false;
        }

        T oldValue = current;
        current = value;
        MotionValueChanged<T> change = new(oldValue, current, target, IsAnimating);
        foreach (Action<MotionValueChanged<T>> listener in listeners.ToArray())
        {
            listener(change);
        }

        return true;
    }

    private static MotionVelocity<T>? TryGetVelocity(MotionSampler<T> sampler)
    {
        try
        {
            return sampler.Velocity;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private sealed class ValueNode(MotionValue<T> owner) : MotionNode
    {
        protected internal override MotionNodeTickResult Tick(MotionFrame frame)
        {
            int valuesChanged = owner.Advance(frame, out bool completed);
            return new MotionNodeTickResult(valuesChanged, Completed: completed);
        }
    }

    private sealed class Subscription(
        List<Action<MotionValueChanged<T>>> listeners,
        Action<MotionValueChanged<T>> listener) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            listeners.Remove(listener);
        }
    }
}

public readonly record struct MotionValueChanged<T>(
    T OldValue,
    T NewValue,
    T Target,
    bool IsAnimating);
