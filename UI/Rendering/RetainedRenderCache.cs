using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Rendering;

public sealed class RetainedRenderCache
{
    private readonly ConditionalWeakTable<UIElement, ElementRenderCache> elementCaches = new();
    private readonly ConditionalWeakTable<UIElement, VisualContentStamp> visualContentStamps = new();
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

    internal long GetVisualContentVersion(UIElement element, long signature)
    {
        ArgumentNullException.ThrowIfNull(element);
        VisualContentStamp stamp = visualContentStamps.GetValue(element, _ => new VisualContentStamp());
        if (!stamp.Initialized || stamp.Signature != signature)
        {
            stamp.Signature = signature;
            stamp.Version = stamp.Version == long.MaxValue ? 1 : stamp.Version + 1;
            stamp.Initialized = true;
        }

        return stamp.Version;
    }

    private sealed class VisualContentStamp
    {
        public bool Initialized { get; set; }

        public long Signature { get; set; }

        public long Version { get; set; }
    }
}
