namespace Cerneala.UI.Controls.Items;

public sealed class ItemsPanelTemplate
{
    private readonly Func<Layout.Panels.Panel> factory;

    public ItemsPanelTemplate(Func<Layout.Panels.Panel> factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Panel CreatePanel()
    {
        return CreateLayoutPanel() as Panel ??
            throw new InvalidOperationException("Items panel template factory did not return a controls-facing Panel.");
    }

    internal Layout.Panels.Panel CreateLayoutPanel()
    {
        return factory() ?? throw new InvalidOperationException("Items panel template factory returned null.");
    }
}
