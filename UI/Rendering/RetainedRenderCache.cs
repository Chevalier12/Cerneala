using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Rendering;

public sealed class RetainedRenderCache
{
    private readonly ConditionalWeakTable<UIElement, ElementRenderCache> elementCaches = new();
    private readonly DrawCommandList rootCommands = new();

    public DrawCommandList RootCommands => rootCommands;

    public int Version { get; private set; }

    public bool IsRootValid { get; private set; }

    public ElementRenderCache GetElementCache(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return elementCaches.GetValue(element, _ => new ElementRenderCache());
    }

    public void InvalidateRoot()
    {
        IsRootValid = false;
    }

    public void MarkRootBuilt()
    {
        IsRootValid = true;
        Version++;
    }
}
