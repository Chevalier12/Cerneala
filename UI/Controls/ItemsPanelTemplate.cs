namespace Cerneala.UI.Controls;

public sealed class ItemsPanelTemplate
{
    private readonly Func<Panel> factory;

    public ItemsPanelTemplate(Func<Panel> factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Panel CreatePanel()
    {
        return factory() ?? throw new InvalidOperationException("Items panel template factory returned null.");
    }
}
