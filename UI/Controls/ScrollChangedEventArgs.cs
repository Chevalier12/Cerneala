using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public sealed class ScrollChangedEventArgs : RoutedEventArgs
{
    public ScrollChangedEventArgs(RoutedEvent routedEvent, object source, float oldHorizontalOffset, float oldVerticalOffset, float horizontalOffset, float verticalOffset)
        : base(routedEvent, source)
    {
        OldHorizontalOffset = oldHorizontalOffset;
        OldVerticalOffset = oldVerticalOffset;
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    public float OldHorizontalOffset { get; }
    public float OldVerticalOffset { get; }
    public float HorizontalOffset { get; }
    public float VerticalOffset { get; }
    public float HorizontalChange => HorizontalOffset - OldHorizontalOffset;
    public float VerticalChange => VerticalOffset - OldVerticalOffset;
}
