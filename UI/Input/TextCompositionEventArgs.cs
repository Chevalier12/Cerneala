namespace Cerneala.UI.Input;

public sealed class TextCompositionEventArgs : RoutedEventArgs
{
    public TextCompositionEventArgs(RoutedEvent routedEvent, object originalSource, string text)
        : base(routedEvent, originalSource)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string Text { get; }
}
