using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Rendering;

public sealed class RetainedRenderCache : IDisposable
{
    // These entries own image acquisitions. A weak-key table can collect an
    // entry before its owner gets the opportunity to release those acquisitions.
    private readonly Dictionary<UIElement, ElementRenderCache> elementCaches = new(ReferenceEqualityComparer.Instance);
    private readonly DrawCommandList rootCommands = new();
    private readonly ImageResourceLeaseSet rootImages = new();
    private bool disposed;

    public DrawCommandList RootCommands => rootCommands;

    public int Version { get; private set; }

    public bool IsRootValid { get; private set; }

    public ElementRenderCache GetElementCache(UIElement element)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(element);
        if (!elementCaches.TryGetValue(element, out ElementRenderCache? cache))
        {
            cache = new ElementRenderCache();
            elementCaches.Add(element, cache);
        }
        return cache;
    }

    public void InvalidateRoot()
    {
        IsRootValid = false;
    }

    public void MarkRootBuilt()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        IsRootValid = true;
        Version++;
    }

    internal void RetainRootResources(ImageResourceCache? cache)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        rootImages.RetainCommands(rootCommands, cache);
    }

    internal void ReleaseRootResources()
    {
        InvalidateRoot();
        rootCommands.Clear();
        rootImages.Clear();
    }

    internal void ReleaseElement(UIElement element)
    {
        InvalidateRoot();
        if (elementCaches.Remove(element, out ElementRenderCache? cache))
        {
            cache.Dispose();
        }
    }

    public void Dispose()
    {
        if (disposed) { return; }
        disposed = true;
        List<Exception>? errors = null;
        foreach (ElementRenderCache cache in elementCaches.Values)
        {
            try { cache.Dispose(); }
            catch (Exception error) { (errors ??= new()).Add(error); }
        }
        elementCaches.Clear();
        try { ReleaseRootResources(); }
        catch (Exception error) { (errors ??= new()).Add(error); }
        if (errors is not null) { throw new AggregateException(errors); }
    }
}
