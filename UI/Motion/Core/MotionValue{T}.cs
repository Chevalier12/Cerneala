using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Diagnostics;
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

    internal MotionGraph Graph => graph;

    public MotionHandle AnimateTo(T target, MotionSpec<T> spec, MotionStartOptions? options = null)
    {
        graph.VerifyAccess();
        ArgumentNullException.ThrowIfNull(spec);
        MotionStartOptions effectiveOptions = options ?? MotionStartOptions.Default;

        if (sampler is not null &&
            activeHandle?.IsActive == true &&
            effectiveOptions.RetargetMode == RetargetMode.PreserveProgress)
        {
            MotionHandle? expectedHandle = activeHandle;
            MotionSampler<T>? expectedSampler = sampler;
            TimeSpan preservedElapsed = animationElapsed;
            if (!TryDetachActiveMotion(expectedHandle, expectedSampler, out MotionHandle? finishingHandle))
            {
                return AnimateTo(
                    target,
                    spec,
                    new MotionStartOptions(
                        RetargetMode.Restart,
                        effectiveOptions.Priority,
                        effectiveOptions.DebugName));
            }

            finishingHandle?.FinishCanceled(MotionCancelBehavior.KeepCurrent, fireEvent: true);
            CancelCallbackCreatedMotion();
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
        CancelCallbackCreatedMotion();
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
        graph.Diagnostics?.Record(MotionTraceEventKind.MotionStarted);

        if (sampler.IsComplete)
        {
            FinishNaturalCompletion(handle, sampler);
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
        MotionSampler<T>? expectedSampler = sampler;
        if (expectedSampler is null)
        {
            return 0;
        }

        MotionHandle? expectedHandle = activeHandle;
        animationElapsed += frame.Delta;
        expectedSampler.Advance(frame.Delta);
        graph.Diagnostics?.Record(MotionTraceEventKind.MotionSampled);
        velocity = TryGetVelocity(expectedSampler);
        if (expectedSampler.IsComplete)
        {
            T completionTarget = target;
            int changed = mixer.EqualsWithinTolerance(current, completionTarget, 0) ? 0 : 1;
            completed = FinishNaturalCompletion(expectedHandle, expectedSampler);
            return changed;
        }

        int sampledChanged = ApplySample(expectedSampler.Current) ? 1 : 0;
        if (!ReferenceEquals(activeHandle, expectedHandle) ||
            !ReferenceEquals(sampler, expectedSampler))
        {
            return sampledChanged;
        }

        return sampledChanged;
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

        MotionHandle? expectedHandle = activeHandle;
        MotionSampler<T>? expectedSampler = sampler;
        T completionTarget = target;
        if (!TryDetachActiveMotion(expectedHandle, expectedSampler, out MotionHandle? finishingHandle))
        {
            return;
        }

        ApplySample(completionTarget);
        graph.Diagnostics?.Record(MotionTraceEventKind.MotionCompleted);
        finishingHandle?.FinishCompleted(fireEvent: true);
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

        MotionHandle? expectedHandle = activeHandle;
        MotionSampler<T>? expectedSampler = sampler;
        T valueToApply = current;
        bool applyValue = false;

        switch (behavior)
        {
            case MotionCancelBehavior.KeepCurrent:
                target = current;
                break;
            case MotionCancelBehavior.Revert:
                target = animationStart;
                valueToApply = animationStart;
                applyValue = true;
                break;
            case MotionCancelBehavior.Complete:
                valueToApply = target;
                applyValue = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unknown motion cancel behavior.");
        }

        if (!TryDetachActiveMotion(expectedHandle, expectedSampler, out MotionHandle? finishingHandle))
        {
            return;
        }

        if (applyValue)
        {
            ApplySample(valueToApply);
        }

        finishingHandle?.FinishCanceled(behavior, fireEvent);
    }

    private void CancelCallbackCreatedMotion()
    {
        if (activeHandle is null && sampler is null)
        {
            return;
        }

        CancelActiveHandle(MotionCancelBehavior.KeepCurrent, fireEvent: false);
    }

    private bool FinishNaturalCompletion(MotionHandle? expectedHandle, MotionSampler<T>? expectedSampler)
    {
        T completionTarget = target;
        if (!TryDetachActiveMotion(expectedHandle, expectedSampler, out MotionHandle? finishingHandle))
        {
            return false;
        }

        ApplySample(completionTarget);
        graph.Diagnostics?.Record(MotionTraceEventKind.MotionCompleted);
        finishingHandle?.FinishCompleted(fireEvent: true);
        return sampler is null && activeHandle is null;
    }

    private bool TryDetachActiveMotion(
        MotionHandle? expectedHandle,
        MotionSampler<T>? expectedSampler,
        out MotionHandle? finishingHandle)
    {
        finishingHandle = null;
        if (!ReferenceEquals(activeHandle, expectedHandle) ||
            !ReferenceEquals(sampler, expectedSampler))
        {
            return false;
        }

        sampler = null;
        animationElapsed = TimeSpan.Zero;
        velocity = null;
        graph.Unregister(node);

        finishingHandle = activeHandle;
        activeHandle = null;
        return true;
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
