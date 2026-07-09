using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class DataTemplate<T> : DataTemplate
{
    private readonly Func<T, UIElement?> factory;

    public DataTemplate(Func<T, UIElement?> factory)
        : base(typeof(T))
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    private protected override UIElement? CreateElementCore(object? data)
    {
        if (data is null)
        {
            return null;
        }

        return factory((T)data);
    }
}
