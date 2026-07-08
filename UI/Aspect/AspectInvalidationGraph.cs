using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectInvalidationGraph
{
    private readonly ConditionalWeakTable<UIElement, DependencyHolder> dependencies = new();

    public void Track(UIElement element, AspectDependencySet dependencySet)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(dependencySet);
        dependencies.Remove(element);
        dependencies.Add(element, new DependencyHolder(dependencySet));
    }

    public bool TryGetDependencies(UIElement element, out AspectDependencySet dependencySet)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (dependencies.TryGetValue(element, out DependencyHolder? holder))
        {
            dependencySet = holder.Dependencies;
            return true;
        }

        dependencySet = new AspectDependencySet();
        return false;
    }

    public void Untrack(UIElement element)
    {
        dependencies.Remove(element);
    }

    private sealed class DependencyHolder
    {
        public DependencyHolder(AspectDependencySet dependencies)
        {
            Dependencies = dependencies;
        }

        public AspectDependencySet Dependencies { get; }
    }
}
