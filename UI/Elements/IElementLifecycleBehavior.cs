namespace Cerneala.UI.Elements;

internal interface IElementLifecycleBehavior
{
    void ValidateRoot(UIRoot root)
    {
    }

    void Attach();

    void Detach();

    void OnRenderabilityChanged(bool isRenderable)
    {
    }
}

// Data observation can be owned without loading a visual element. Rendering,
// resource decoding, Aspect and Motion behaviors deliberately do not opt in.
internal interface IElementDataLifecycleBehavior : IElementLifecycleBehavior
{
    void ValidateRelay(Cerneala.UI.Relay.UiRelay relay) { }
}
