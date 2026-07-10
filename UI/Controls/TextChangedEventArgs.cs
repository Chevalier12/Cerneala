using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public sealed class TextChangedEventArgs : RoutedEventArgs
{
    public TextChangedEventArgs(RoutedEvent routedEvent, object source, string oldText, string newText) : base(routedEvent, source)
    {
        OldText = oldText;
        NewText = newText;
    }

    public string OldText { get; }
    public string NewText { get; }
}
