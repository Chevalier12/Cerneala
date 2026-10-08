using Cerneala.Timbre;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Elements;

public partial class UIElement
{
    private ElementTimbreOwner? timbreOwner;

    public TimbreScope Timbre
    {
        get
        {
            UIRoot root = Root ??
                throw new InvalidOperationException("A detached element cannot play sounds; attach it to a root first.");
            root.Relay.VerifyAccess();
            if (timbreOwner is null)
            {
                timbreOwner = new ElementTimbreOwner(this);
                AddLifecycleBehavior(timbreOwner);
            }

            return timbreOwner.GetScope();
        }
    }
}
