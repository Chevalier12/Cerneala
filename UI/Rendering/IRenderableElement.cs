namespace Cerneala.UI.Rendering;

public interface IRenderableElement
{
    int RenderVersion { get; }

    RenderDependency RenderDependencies { get; }

    void Render(RenderContext context);
}
