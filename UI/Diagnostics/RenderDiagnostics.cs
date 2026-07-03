using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Diagnostics;

public static class RenderDiagnostics
{
    public static RootRenderDiagnosticsSnapshot CaptureRoot(RetainedRenderCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        return new RootRenderDiagnosticsSnapshot(cache.IsRootValid, cache.Version, cache.RootCommands.Count);
    }

    public static ElementRenderDiagnosticsSnapshot CaptureElement(UIElement element, RetainedRenderCache cache)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(cache);

        ElementRenderCache elementCache = cache.GetElementCache(element);
        return new ElementRenderDiagnosticsSnapshot(
            element.ElementId?.ToString(),
            element.GetType().Name,
            element.RenderVersion,
            element.RenderDependencies,
            elementCache.IsValid,
            elementCache.RenderVersion,
            elementCache.Dependencies,
            elementCache.ContentBounds,
            elementCache.Commands.Count,
            elementCache.IsStale(element));
    }
}

public sealed record RootRenderDiagnosticsSnapshot(bool IsRootValid, int Version, int RootCommandCount)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"root valid={IsRootValid}, version={Version}, commands={RootCommandCount}");
    }
}

public sealed record ElementRenderDiagnosticsSnapshot(
    string? ElementId,
    string ElementType,
    int ElementRenderVersion,
    RenderDependency ElementDependencies,
    bool IsCacheValid,
    int CacheRenderVersion,
    RenderDependency CacheDependencies,
    Cerneala.UI.Layout.LayoutRect ContentBounds,
    int CommandCount,
    bool IsStale)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ElementType}#{ElementId ?? "unattached"} cacheValid={IsCacheValid}, stale={IsStale}, elementRenderVersion={ElementRenderVersion}, cacheRenderVersion={CacheRenderVersion}, commands={CommandCount}, bounds={ContentBounds}, elementDependencies={ElementDependencies}, cacheDependencies={CacheDependencies}");
    }
}
