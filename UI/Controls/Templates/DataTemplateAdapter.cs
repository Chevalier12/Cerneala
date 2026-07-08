using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class DataTemplateAdapter : ContentTemplate
{
    private readonly DataTemplate template;

    public DataTemplateAdapter(DataTemplate template)
        : base($"legacy.{template?.DataType.Name}", template?.DataType, key: null, priority: 0, ThrowBeforeInitialized)
    {
        this.template = template ?? throw new ArgumentNullException(nameof(template));
    }

    private static UIElement? ThrowBeforeInitialized(ContentTemplateContext context)
    {
        throw new InvalidOperationException("DataTemplateAdapter must be constructed before use.");
    }

    public override UIElement? Create(ContentTemplateContext context)
    {
        return template.CreateElement(context.Data);
    }
}
