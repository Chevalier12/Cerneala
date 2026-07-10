using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public enum ScrollEventType
{
    SmallDecrement,
    SmallIncrement,
    LargeDecrement,
    LargeIncrement,
    ThumbTrack,
    ThumbPosition,
    First,
    Last,
    EndScroll
}

public sealed class ScrollEventArgs : RoutedEventArgs
{
    public ScrollEventArgs(RoutedEvent routedEvent, object source, ScrollEventType scrollEventType, float newValue)
        : base(routedEvent, source)
    {
        ScrollEventType = scrollEventType;
        NewValue = newValue;
    }

    public ScrollEventType ScrollEventType { get; }
    public float NewValue { get; }
}
