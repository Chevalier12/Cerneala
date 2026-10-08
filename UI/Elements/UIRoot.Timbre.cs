using Cerneala.Timbre;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Elements;

public sealed partial class UIRoot
{
    private readonly List<ElementTimbreOwner> timbreOwners = [];
    private TimbreRuntime? timbreRuntime;

    // Shared, caller-owned runtime for every element scope under this root.
    public TimbreRuntime? TimbreRuntime => timbreRuntime;

    public void SetTimbreRuntime(TimbreRuntime? runtime)
    {
        Relay.VerifyAccess();
        if (ReferenceEquals(timbreRuntime, runtime))
        {
            return;
        }

        foreach (ElementTimbreOwner owner in timbreOwners.ToArray())
        {
            owner.Retire();
        }

        timbreOwners.Clear();
        timbreRuntime = runtime;
    }

    internal void RegisterTimbreOwner(ElementTimbreOwner owner) => timbreOwners.Add(owner);

    internal void UnregisterTimbreOwner(ElementTimbreOwner owner) => timbreOwners.Remove(owner);
}
