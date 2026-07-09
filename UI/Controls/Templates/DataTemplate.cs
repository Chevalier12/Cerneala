using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public abstract class DataTemplate
{
    private protected DataTemplate(Type dataType)
    {
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
    }

    public Type DataType { get; }

    public bool CanApply(object? data)
    {
        return data is null || DataType.IsInstanceOfType(data);
    }

    public UIElement? CreateElement(object? data)
    {
        if (!CanApply(data))
        {
            throw new InvalidOperationException(
                $"Data template for '{DataType.FullName}' cannot be applied to '{data?.GetType().FullName}'.");
        }

        return CreateElementCore(data);
    }

    private protected abstract UIElement? CreateElementCore(object? data);
}
