using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Rendering;

public sealed class ElementRenderCache : IDisposable
{
    private readonly DrawCommandList commands = new();
    private readonly ImageResourceLeaseSet imageLeases = new();
    private UIElement? cachedElement;
    private UIElement? transformElement;
    private long transformPropertyVersion;
    private int transformScopeVersion;
    private LayoutRect transformBounds;
    private Matrix3x2 elementTransform;
    private bool disposed;

    public DrawCommandList Commands => commands;

    public bool IsValid { get; private set; }

    public int RenderVersion { get; private set; } = -1;

    public RenderDependency Dependencies { get; private set; }

    public LayoutRect ContentBounds { get; private set; }

    internal Matrix3x2 GetElementTransform(UIElement element)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(element);
        // Property writes precede PropertyChanged callbacks, whereas scope
        // invalidation follows them. Include the store version so composition
        // inside a callback also observes the new effective values.
        if (!ReferenceEquals(transformElement, element) ||
            transformPropertyVersion != element.PropertyValueVersion ||
            transformScopeVersion != element.RenderScopeVersion ||
            transformBounds != element.ArrangedBounds)
        {
            elementTransform = ElementVisualTransform.GetElementTransform(element);
            transformElement = element;
            transformPropertyVersion = element.PropertyValueVersion;
            transformScopeVersion = element.RenderScopeVersion;
            transformBounds = element.ArrangedBounds;
        }
        return elementTransform;
    }

    public bool IsStale(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return !IsValid ||
            !ReferenceEquals(cachedElement, element) ||
            RenderVersion != element.RenderVersion ||
            Dependencies != element.RenderDependencies;
    }

    public DrawCommandList GetValidCommands(UIElement element)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(element);
        if (IsStale(element))
        {
            throw new InvalidOperationException(
                $"Element '{element.GetType().Name}' does not have a valid local render cache. " +
                "Local render caches must be rebuilt by RenderQueueProcessor before root command composition.");
        }

        return commands;
    }

    public bool Ensure(UIElement element, RenderCounters counters, bool forceRebuild = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(counters);

        if (!forceRebuild && !IsStale(element))
        {
            counters.CountCacheHit();
            return false;
        }

        counters.CountCacheMiss();
        counters.CountLocalRebuild();
        transformElement = null;
        IsValid = false;
        cachedElement = null;
        commands.Clear();

        try
        {
            if (element.Visibility == Visibility.Visible && element.IsVisible)
            {
                DrawingContext drawingContext = new(commands);
                RenderContext context = new(element, drawingContext, element.ArrangedBounds, RenderLayer.Default, counters);
                element.Render(context);
            }
            imageLeases.RetainCommands(commands, element.Root?.ImageResourceCache);
        }
        catch
        {
            commands.Clear();
            imageLeases.Clear();
            throw;
        }

        RenderVersion = element.RenderVersion;
        Dependencies = element.RenderDependencies;
        ContentBounds = element.ArrangedBounds;
        cachedElement = element;
        IsValid = true;
        return true;
    }

    public void Invalidate()
    {
        IsValid = false;
        cachedElement = null;
        transformElement = null;
    }

    public void Dispose()
    {
        if (disposed) { return; }
        disposed = true;
        Invalidate();
        commands.Clear();
        imageLeases.Clear();
    }
}
