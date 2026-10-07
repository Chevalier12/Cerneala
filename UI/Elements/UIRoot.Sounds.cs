using Cerneala.Timbre;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Elements;

public sealed partial class UIRoot
{
    private readonly List<ElementSoundOwner> soundOwners = [];
    private SoundRuntime? soundRuntime;

    // Shared, caller-owned runtime for every element scope under this root.
    public SoundRuntime? SoundRuntime => soundRuntime;

    public void SetSoundRuntime(SoundRuntime? runtime)
    {
        Relay.VerifyAccess();
        if (ReferenceEquals(soundRuntime, runtime))
        {
            return;
        }

        foreach (ElementSoundOwner owner in soundOwners.ToArray())
        {
            owner.Retire();
        }

        soundOwners.Clear();
        soundRuntime = runtime;
    }

    internal void RegisterSoundOwner(ElementSoundOwner owner) => soundOwners.Add(owner);

    internal void UnregisterSoundOwner(ElementSoundOwner owner) => soundOwners.Remove(owner);
}
