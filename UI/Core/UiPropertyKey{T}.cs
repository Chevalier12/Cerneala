namespace Cerneala.UI.Core;

public sealed class UiPropertyKey<T>
{
    internal UiPropertyKey(UiProperty<T> property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public UiProperty<T> Property { get; }
}
