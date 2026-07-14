namespace Cerneala.UI.Elements;

internal interface IElementLifecycleBehavior
{
    void ValidateRoot(UIRoot root)
    {
    }

    void Attach();

    void Detach();
}
