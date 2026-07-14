using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Presence;

public sealed class PresenceCoordinator
{
    private readonly MotionSystem motion;
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

        PresenceHandle handle = new(owner, element, CompleteExit);
        exits[element] = handle;
        if (!exitingByOwner.TryGetValue(owner, out List<UIElement>? exitingChildren))
        {
            exitingChildren = [];
            exitingByOwner[owner] = exitingChildren;
        }

        exitingChildren.Add(element);
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
        _ = opacity.Subscribe(change => element.SetPresenceVisual(change.NewValue, element.PresenceScale));
        _ = scale.Subscribe(change => element.SetPresenceVisual(element.PresenceOpacity, change.NewValue));
        opacity.AnimateTo(1, options.Enter);
        scale.AnimateTo(1, options.Enter);
    }

    internal void MarkDetached(UIElement element)
    {
        if (exits.Remove(element, out PresenceHandle? handle))
        {
            RemoveFromOwner(handle.Owner, element);
        }

        element.SetPresenceExiting(false);
        states[element] = PresenceState.Detached;
    }

    private void CompleteExit(PresenceHandle handle)
    {
        UIElement element = handle.Element;
        if (!exits.Remove(element))
        {
            return;
        }

        RemoveFromOwner(handle.Owner, element);
        element.SetPresenceExiting(false);
        element.SetPresenceVisual(1, 1);
        handle.RemoveElement(motion.Root);
        states[element] = PresenceState.Detached;
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
}
