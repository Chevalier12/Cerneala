using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleSelector
{
    private readonly Func<UIElement, bool> predicate;

    private StyleSelector(string description, Func<UIElement, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Selector description cannot be empty.", nameof(description));
        }

        Description = description;
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public string Description { get; }

    public static StyleSelector Any { get; } = new("Any", _ => true);

    public static StyleSelector ForType<TElement>()
        where TElement : UIElement
    {
        return ForType(typeof(TElement));
    }

    public static StyleSelector ForType(Type elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        if (!typeof(UIElement).IsAssignableFrom(elementType))
        {
            throw new ArgumentException("Selector type must derive from UIElement.", nameof(elementType));
        }

        return new StyleSelector(elementType.FullName ?? elementType.Name, elementType.IsInstanceOfType);
    }

    public static StyleSelector Where(string description, Func<UIElement, bool> predicate)
    {
        return new StyleSelector(description, predicate);
    }

    public bool Matches(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return predicate(element);
    }

    public override string ToString()
    {
        return Description;
    }
}
