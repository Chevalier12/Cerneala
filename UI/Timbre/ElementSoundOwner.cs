using Cerneala.Timbre;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Timbre;

// Owns the SoundScope behind UIElement.Sounds for one attachment lifecycle.
// Detach disposes the scope; reattaching creates a fresh scope on next use.
// Renderability changes are deliberately ignored: hiding a control keeps its audio.
internal sealed class ElementSoundOwner(UIElement element) : IElementLifecycleBehavior
{
    private SoundScope? scope;
    private UIRoot? registeredRoot;

    internal SoundScope GetScope()
    {
        UIRoot root = element.Root ??
            throw new InvalidOperationException("A detached element cannot play sounds; attach it to a root first.");
        root.Relay.VerifyAccess();
        SoundRuntime runtime = root.SoundRuntime ??
            throw new InvalidOperationException(
                "The element's root has no SoundRuntime. Use Application.SoundRuntime, UiHostOptions.SoundRuntime, or UIRoot.SetSoundRuntime.");
        if (scope is null || scope.IsDisposed)
        {
            scope = runtime.CreateScope();
            if (!ReferenceEquals(registeredRoot, root))
            {
                registeredRoot?.UnregisterSoundOwner(this);
                root.RegisterSoundOwner(this);
                registeredRoot = root;
            }
        }

        return scope;
    }

    public void Attach()
    {
    }

    public void Detach() => Retire();

    internal void Retire()
    {
        scope?.Dispose();
        scope = null;
        registeredRoot?.UnregisterSoundOwner(this);
        registeredRoot = null;
    }
}
