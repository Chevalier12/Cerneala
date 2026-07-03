using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Rendering;

public sealed class RetainedRenderer
{
    private readonly RetainedRenderCache renderCache;
    private readonly DrawCommandListBuilder builder;
    private readonly RenderCounters counters;

    public RetainedRenderer(RetainedRenderCache renderCache, DrawCommandListBuilder builder, RenderCounters counters)
    {
        this.renderCache = renderCache ?? throw new ArgumentNullException(nameof(renderCache));
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
    }

    public DrawCommandList Render(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!renderCache.IsRootValid)
        {
            builder.Build(root, renderCache, counters);
        }

        return renderCache.RootCommands;
    }

    public void Submit(UIRoot root, IDrawingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        backend.Render(CopyCommands(Render(root)));
    }

    private static DrawCommandList CopyCommands(DrawCommandList commands)
    {
        DrawCommandList copy = new();
        foreach (DrawCommand command in commands)
        {
            copy.Add(command);
        }

        return copy;
    }
}
