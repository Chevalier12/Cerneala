using Cerneala.Timbre;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Timbre;

// Owns the TimbreScope behind UIElement.Timbre for one attachment lifecycle.
// Detach disposes the scope; reattaching creates a fresh scope on next use.
// Renderability changes are deliberately ignored: hiding a control keeps its audio.
internal sealed class ElementTimbreOwner(UIElement element) : IElementLifecycleBehavior
{
    private TimbreScope? scope;
    private UIRoot? registeredRoot;

    internal TimbreScope GetScope()
    {
        UIRoot root = element.Root ??
            throw new InvalidOperationException("A detached element cannot play sounds; attach it to a root first.");
        root.Relay.VerifyAccess();
        TimbreRuntime runtime = root.TimbreRuntime ??
            throw new InvalidOperationException(
                "The element's root has no TimbreRuntime. Use Application.TimbreRuntime, UiHostOptions.TimbreRuntime, or UIRoot.SetTimbreRuntime.");
        if (scope is null || scope.IsDisposed)
        {
            scope = runtime.CreateScope();
            TimbrePlaybackMotion.RegisterScope(scope, root);
            if (!ReferenceEquals(registeredRoot, root))
            {
                registeredRoot?.UnregisterTimbreOwner(this);
                root.RegisterTimbreOwner(this);
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
        registeredRoot?.UnregisterTimbreOwner(this);
        registeredRoot = null;
    }
}
