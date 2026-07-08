using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectMatchContext
{
    public AspectMatchContext(
        UIElement element,
        UIElement? ownerComponent = null,
        AspectSlotPath? slotPath = null,
        AspectStateSet? states = null,
        AspectVariantSet? variants = null,
        int environmentVersion = 0,
        AspectDataContext? dataContext = null)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        OwnerComponent = ownerComponent;
        SlotPath = slotPath;
        States = states ?? AspectStateSet.Empty;
        Variants = variants ?? AspectVariantSet.Empty;
        EnvironmentVersion = environmentVersion;
        DataContext = dataContext ?? AspectDataContext.Empty;
    }

    public UIElement Element { get; }

    public UIElement? OwnerComponent { get; }

    public AspectSlotPath? SlotPath { get; }

    public AspectStateSet States { get; }

    public AspectVariantSet Variants { get; }

    public int EnvironmentVersion { get; }

    public AspectDataContext DataContext { get; }

    public object? Data => DataContext.Data;

    public Type? DataType => DataContext.DataType;

    public int? ItemIndex => DataContext.Index;
}
