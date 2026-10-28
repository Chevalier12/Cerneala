using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyBinding<T> : MotionPropertyBinding
{
    private readonly MotionSystem motion;
    private readonly MotionPropertyInvalidationCategory invalidationCategory;
    private readonly BindingNode node;
    private readonly IDisposable valueSubscription;
    private MotionHandle? activeHandle;
    private bool hasPendingSample;
    private T pendingSample = default!;
    private bool holdOnComplete;
    private bool completedNaturally;
    private bool holdsSetValue;
    // True while the property does not show the Animation value of this binding: it never wrote one, or its last write
    // cleared it (an animation completed without HoldOnComplete, or Clear restored the base value).
    private bool releasedAnimationValue = true;
    private bool disposed;

    public MotionPropertyBinding(MotionSystem motion, UiObject target, UiProperty<T> property, MotionValue<T> value)
    {
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        if (!ReferenceEquals(Value.Graph, motion.Graph))
        {
            throw new InvalidOperationException("MotionPropertyBinding requires a MotionValue created by the same MotionSystem as the binding.");
        }

        invalidationCategory = MotionPropertyInvalidationClassifier.Classify(property);
        node = new BindingNode(this);
        valueSubscription = Value.Subscribe(OnValueChanged);
        Value.TargetValidation += ValidateTarget;
    }

    internal override MotionSystem Motion => motion;

    internal override bool IsActive =>
        activeHandle?.IsActive == true || Value.IsAnimating || hasPendingSample;

    internal override bool HoldsSetValue => holdsSetValue;

    internal override MotionHandle? ActiveHandle => activeHandle?.IsActive == true ? activeHandle : null;

    public override UiObject Target { get; }

    public UiProperty<T> Property { get; }

    public override UiProperty PropertyUntyped => Property;

    public MotionValue<T> Value { get; }

    public MotionHandle AnimateTo(T to, MotionSpec<T> spec, MotionPropertyStartOptions? options = null)
    {
        ThrowIfDisposed();
        motion.VerifyAccess();
        ArgumentNullException.ThrowIfNull(spec);

        MotionPropertyStartOptions effectiveOptions = options ?? MotionPropertyStartOptions.Default;
        MotionHandle handle = Value.AnimateTo(to, spec, effectiveOptions.ToMotionStartOptions());
        if (handle.IsCanceled && Value.IsAnimating)
        {
            return handle;
        }

        holdsSetValue = false;
        holdOnComplete = effectiveOptions.HoldOnComplete;
        completedNaturally = false;
        activeHandle = handle;
        activeHandle.Completed += OnMotionCompleted;
        if (Target is UIElement element && !UIElementVisibility.IsEffectivelyVisible(element))
        {
            Clear();
            return handle;
        }

        if (handle.IsCompleted)
        {
            activeHandle = null;
            completedNaturally = true;
            if (holdOnComplete)
            {
                StageAnimationValue(Value.Current);
            }
            else
            {
                ReleaseAnimationValue();
            }

            return handle;
        }

        StageCurrent();
        motion.Graph.Register(node);
        return handle;
    }

    // Snaps the Motion value and holds it, like an animation that completed at
    // once with HoldOnComplete: the Animation source carries the value until a
    // later motion or Clear replaces it. The value is held also while the target
    // is hidden, so it is still there when the target is shown again.
    internal void JumpToAndHold(T value)
    {
        ThrowIfDisposed();
        motion.VerifyAccess();
        Value.JumpTo(value);
        activeHandle = null;
        holdsSetValue = true;
        holdOnComplete = true;
        completedNaturally = true;
        hasPendingSample = false;
        motion.Graph.Unregister(node);
        StageAnimationValue(Value.Current);
    }

    public override void Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase)
    {
        if (disposed)
        {
            return;
        }

        holdsSetValue = false;
        motion.VerifyAccess();
        activeHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        activeHandle = null;
        completedNaturally = false;

        if (behavior == MotionClearBehavior.HoldCurrent)
        {
            StageAnimationValue(Value.Current);
        }
        else
        {
            ReleaseAnimationValue();
        }

        motion.Graph.Unregister(node);
    }

    public override void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Clear();
        disposed = true;
        valueSubscription.Dispose();
        Value.TargetValidation -= ValidateTarget;
        motion.Properties.RemoveBinding(this);
    }

    private void OnValueChanged(MotionValueChanged<T> change)
    {
        pendingSample = change.NewValue;
        hasPendingSample = true;
    }

    private void ValidateTarget(T value)
    {
        if (Property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{Property.DiagnosticName}' is read-only.");
        }

        object? coerced = Property.CoerceUntyped(Target, value);
        Property.ValidateUntyped(coerced);
        Target.ValidatePropertyWrite(Property, coerced, UiPropertyValueSource.Animation);
    }

    private void OnMotionCompleted(object? sender, MotionCompletedEventArgs args)
    {
        if (args.State != MotionCompletionState.Completed)
        {
            return;
        }

        completedNaturally = true;
        activeHandle = null;
        if (!holdOnComplete)
        {
            ReleaseAnimationValue();
        }
    }

    private MotionNodeTickResult Tick()
    {
        if (disposed)
        {
            return new MotionNodeTickResult(Completed: true);
        }

        if (Target is UIElement element &&
            (!element.IsAttached || !UIElementVisibility.IsEffectivelyVisible(element)))
        {
            Clear();
            return new MotionNodeTickResult(Completed: true);
        }

        if (hasPendingSample)
        {
            StageAnimationValue(pendingSample);
            hasPendingSample = false;
        }

        if (completedNaturally && !holdOnComplete)
        {
            ReleaseAnimationValue();
        }

        bool finished = completedNaturally || activeHandle is null || !activeHandle.IsActive;
        return new MotionNodeTickResult(Completed: finished);
    }

    // A markup @animate without @from starts at the value the property shows. While this binding drives or holds the
    // Animation value, that is the Motion value. Once it released the Animation value, the property shows the value
    // below it again, while the Motion value can still be where the last animation ended.
    internal void JumpToShownValue()
    {
        ThrowIfDisposed();
        motion.VerifyAccess();
        if (!releasedAnimationValue || Value.IsAnimating)
        {
            return;
        }

        Value.JumpTo(Target.GetValueAfterClear(Property, UiPropertyValueSource.Animation));
        // The property already shows the value, so the jump stages no Animation write.
        hasPendingSample = false;
    }

    private void StageAnimationValue(T value)
    {
        releasedAnimationValue = false;
        motion.Properties.StageSet(Target, Property, value, invalidationCategory);
    }

    private void ReleaseAnimationValue()
    {
        releasedAnimationValue = true;
        motion.Properties.StageClear(Target, Property, invalidationCategory);
    }

    private void StageCurrent()
    {
        releasedAnimationValue = false;
        pendingSample = Value.Current;
        hasPendingSample = true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    private sealed class BindingNode(MotionPropertyBinding<T> owner) : MotionNode
    {
        protected internal override MotionNodeTickResult Tick(MotionFrame frame)
        {
            return owner.Tick();
        }
    }
}
