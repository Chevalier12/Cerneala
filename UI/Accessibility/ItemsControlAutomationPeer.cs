using Cerneala.UI.Controls;

namespace Cerneala.UI.Accessibility;

public sealed class ItemsControlAutomationPeer : AutomationPeer
{
    private readonly ItemsControl itemsControl;

    public ItemsControlAutomationPeer(ItemsControl itemsControl)
        : base(itemsControl)
    {
        this.itemsControl = itemsControl;
    }

    public override SemanticsRole Role => SemanticsRole.List;

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.ItemCount] = itemsControl.ItemCount
        };
        return properties;
    }
}
