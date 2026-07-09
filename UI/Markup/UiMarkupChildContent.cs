namespace Cerneala.UI.Markup;

public sealed class UiMarkupChildContent : UiMarkupContent
{
    public UiMarkupChildContent(UiMarkupNode node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public UiMarkupNode Node { get; }
}
