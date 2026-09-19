using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectInvalidationGraph
{
    private readonly ConditionalWeakTable<UIElement, AspectDependencySet> dependencies = new();

    public void Track(UIElement element, AspectDependencySet dependencySet)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(dependencySet);
        dependencies.Remove(element);
        dependencies.Add(element, dependencySet);
    }

    public bool TryGetDependencies(UIElement element, out AspectDependencySet dependencySet)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (dependencies.TryGetValue(element, out AspectDependencySet? trackedDependencies))
        {
            dependencySet = trackedDependencies;
            return true;
        }

        dependencySet = new AspectDependencySet();
        return false;
    }

    public void Untrack(UIElement element)
    {
        dependencies.Remove(element);
    }
}
