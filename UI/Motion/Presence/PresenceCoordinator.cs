using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Presence;

public sealed class PresenceCoordinator
{
    private readonly MotionSystem motion;
    private readonly Dictionary<UIElement, EnterAnimation> enters = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UIElement, PresenceHandle> exits = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UIElement, PresenceState> states = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<UIElement, List<UIElement>> exitingByOwner = new(ReferenceEqualityComparer.Instance);

    public PresenceCoordinator(MotionSystem motion)
    {
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
    }

    public PresenceState GetState(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (states.TryGetValue(element, out PresenceState state))
        {
            return state;
        }

        return element.IsAttached ? PresenceState.Present : PresenceState.Detached;
    }

    public int ActiveExitCount => exits.Count;

    public IReadOnlyList<UIElement> GetExitingVisualChildren(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return exitingByOwner.TryGetValue(owner, out List<UIElement>? children) ? children : [];
    }

    internal bool TryBeginExit(UIElement owner, UIElement element)
    {
        motion.VerifyAccess();
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(element);

        PresenceOptions? options = element.Presence;
        if (options is null || !element.IsAttached)
        {
            return false;
        }

        if (exits.ContainsKey(element))
        {
            return true;
        }

        CancelEnter(element);
        PresenceHandle handle = new(owner, element, CompleteExit);
        exits[element] = handle;
        if (!exitingByOwner.TryGetValue(owner, out List<UIElement>? exitingChildren))
        {
            exitingChildren = [];
            exitingByOwner[owner] = exitingChildren;
        }

        exitingChildren.Add(element);
        element.SetRetainedVisualParent(owner);
        states[element] = PresenceState.Exiting;
        element.SetPresenceExiting(options.ExcludeInputWhileExiting);

        MotionValue<float> opacity = motion.Graph.CreateValue(element.PresenceOpacity);
        MotionValue<float> scale = motion.Graph.CreateValue(element.PresenceScale);
        handle.AddSubscription(opacity.Subscribe(change => element.SetPresenceVisual(change.NewValue, element.PresenceScale)));
        handle.AddSubscription(scale.Subscribe(change => element.SetPresenceVisual(element.PresenceOpacity, change.NewValue)));
        handle.OpacityHandle = opacity.AnimateTo(0, options.Exit);
        handle.ScaleHandle = scale.AnimateTo(0.95f, options.Exit);
        handle.OpacityHandle.Completed += (_, args) =>
        {
            if (!args.IsCanceled)
            {
                handle.CompleteRemoval();
            }
        };
        if (handle.OpacityHandle.IsCompleted)
        {
            handle.CompleteRemoval();
        }

        return true;
    }

    internal bool TryCancelExitForAdd(UIElement newOwner, UIElement element)
    {
        motion.VerifyAccess();
        ArgumentNullException.ThrowIfNull(newOwner);
        ArgumentNullException.ThrowIfNull(element);
        if (!exits.Remove(element, out PresenceHandle? handle))
        {
            return false;
        }

        RemoveFromOwner(handle.Owner, element);
        element.SetRetainedVisualParent(null);
        handle.Owner.IncrementPrismDescendantVisualVersion();
        handle.Cancel();
        element.SetPresenceExiting(false);
        element.SetPresenceVisual(1, 1);
        PresenceOptions? options = element.Presence;

        states[element] = PresenceState.Present;
        return true;
    }

    internal void MarkAttached(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        CancelEnter(element);
        PresenceOptions? options = element.Presence;
        if (options is null)
        {
            return;
        }

        if (states.TryGetValue(element, out PresenceState state) && state == PresenceState.Present)
        {
            return;
        }

        states[element] = PresenceState.Present;
        element.SetPresenceVisual(0, 0.95f);

        MotionValue<float> opacity = motion.Graph.CreateValue(element.PresenceOpacity);
        MotionValue<float> scale = motion.Graph.CreateValue(element.PresenceScale);
        EnterAnimation animation = new();
        animation.AddSubscription(opacity.Subscribe(change => element.SetPresenceVisual(change.NewValue, element.PresenceScale)));
        animation.AddSubscription(scale.Subscribe(change => element.SetPresenceVisual(element.PresenceOpacity, change.NewValue)));
        enters[element] = animation;
        animation.OpacityHandle = opacity.AnimateTo(1, options.Enter);
        animation.ScaleHandle = scale.AnimateTo(1, options.Enter);
        animation.OpacityHandle.Completed += (_, _) => CompleteEnter(element, animation);
        animation.ScaleHandle.Completed += (_, _) => CompleteEnter(element, animation);
        CompleteEnter(element, animation);
    }

    internal void MarkDetached(UIElement element)
    {
        CancelEnter(element);
        if (exits.Remove(element, out PresenceHandle? handle))
        {
            RemoveFromOwner(handle.Owner, element);
            element.SetRetainedVisualParent(null);
            handle.Owner.IncrementPrismDescendantVisualVersion();
            handle.Cancel();
        }

        element.SetPresenceExiting(false);
        states.Remove(element);
    }

    private void CompleteExit(PresenceHandle handle)
    {
        UIElement element = handle.Element;
        if (!exits.Remove(element))
        {
            return;
        }

        RemoveFromOwner(handle.Owner, element);
        element.SetRetainedVisualParent(null);
        handle.Owner.IncrementPrismDescendantVisualVersion();
        element.SetPresenceExiting(false);
        element.SetPresenceVisual(1, 1);
        handle.RemoveElement(motion.Root);
        handle.Cancel();
        states.Remove(element);
    }

    private void CompleteEnter(UIElement element, EnterAnimation animation)
    {
        if (animation.OpacityHandle?.IsActive == true || animation.ScaleHandle?.IsActive == true)
        {
            return;
        }

        if (enters.TryGetValue(element, out EnterAnimation? current) && ReferenceEquals(current, animation))
        {
            enters.Remove(element);
            animation.Dispose();
        }
    }

    private void CancelEnter(UIElement element)
    {
        if (enters.Remove(element, out EnterAnimation? animation))
        {
            animation.Dispose();
        }
    }

    private void RemoveFromOwner(UIElement owner, UIElement element)
    {
        if (!exitingByOwner.TryGetValue(owner, out List<UIElement>? children))
        {
            return;
        }

        children.RemoveAll(candidate => ReferenceEquals(candidate, element));
        if (children.Count == 0)
        {
            exitingByOwner.Remove(owner);
        }
    }

    private sealed class EnterAnimation : IDisposable
    {
        private readonly List<IDisposable> subscriptions = [];

        public MotionHandle? OpacityHandle { get; set; }

        public MotionHandle? ScaleHandle { get; set; }

        public void AddSubscription(IDisposable subscription)
        {
            subscriptions.Add(subscription);
        }

        public void Dispose()
        {
            OpacityHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
            ScaleHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
            foreach (IDisposable subscription in subscriptions)
            {
                subscription.Dispose();
            }

            subscriptions.Clear();
        }
    }
}
