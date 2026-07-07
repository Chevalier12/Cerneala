using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Presence;

public sealed class PresenceHandle
{
    private readonly UIElement element;
    private readonly Action<PresenceHandle> complete;
    private readonly List<IDisposable> subscriptions = [];
    private bool completed;

    internal PresenceHandle(UIElement owner, UIElement element, Action<PresenceHandle> complete)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.element = element ?? throw new ArgumentNullException(nameof(element));
        this.complete = complete ?? throw new ArgumentNullException(nameof(complete));
    }

    public UIElement Owner { get; }

    public UIElement Element => element;

    public PresenceState State { get; internal set; } = PresenceState.Exiting;

    internal MotionHandle? OpacityHandle { get; set; }

    internal MotionHandle? ScaleHandle { get; set; }

    internal void Cancel()
    {
        OpacityHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        ScaleHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        foreach (IDisposable subscription in subscriptions)
        {
            subscription.Dispose();
        }

        subscriptions.Clear();
    }

    internal void AddSubscription(IDisposable subscription)
    {
        subscriptions.Add(subscription);
    }

    internal void CompleteRemoval()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        complete(this);
    }

    internal bool RemoveElement(UIRoot root)
    {
        ElementLifecycle.DetachSubtree(root, element);
        root.IncrementTreeVersion();
        return true;
    }
}
