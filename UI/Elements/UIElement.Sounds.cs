using Cerneala.Timbre;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Elements;

public partial class UIElement
{
    private ElementSoundOwner? soundOwner;

    public SoundScope Sounds
    {
        get
        {
            UIRoot root = Root ??
                throw new InvalidOperationException("A detached element cannot play sounds; attach it to a root first.");
            root.Relay.VerifyAccess();
            if (soundOwner is null)
            {
                soundOwner = new ElementSoundOwner(this);
                AddLifecycleBehavior(soundOwner);
            }

            return soundOwner.GetScope();
        }
    }
}
