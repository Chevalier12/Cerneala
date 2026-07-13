using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectInvalidation : IDisposable
{
    private readonly AspectEngine engine;
    private readonly AspectCatalog catalog;
    private readonly AspectEnvironment environment;
    private readonly HashSet<UIElement> trackedElements = new(ReferenceEqualityComparer.Instance);
    private bool isApplying;
    private bool disposed;

    public AspectInvalidation(AspectEngine engine, AspectCatalog catalog, AspectEnvironment environment)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        environment.TokenChanged += OnEnvironmentTokenChanged;
    }

    public AspectApplicationResult Track(UIElement element)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(element);
        if (trackedElements.Add(element))
        {
            element.PropertyChanged += OnElementPropertyChanged;
        }

        return Apply(element);
    }

    public AspectApplicationResult Recompute(UIElement element)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(element);
        return Apply(element);
    }

    public bool Untrack(UIElement element)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(element);
        if (!trackedElements.Remove(element))
        {
            return false;
        }

        element.PropertyChanged -= OnElementPropertyChanged;
        engine.Clear(element);
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        environment.TokenChanged -= OnEnvironmentTokenChanged;
        foreach (UIElement element in trackedElements)
        {
            element.PropertyChanged -= OnElementPropertyChanged;
        }

        trackedElements.Clear();
    }

    private AspectApplicationResult Apply(UIElement element)
    {
        bool previous = isApplying;
        isApplying = true;
        try
        {
            return engine.Apply(
                element,
                catalog,
                environment,
                dataContext: new AspectDataContext(element.DataContext, owner: element));
        }
        finally
        {
            isApplying = previous;
        }
    }

    private void OnElementPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (!isApplying && sender is UIElement element && AffectsAspect(element, args.Property))
        {
            Apply(element);
        }
    }

    private void OnEnvironmentTokenChanged(AspectToken token)
    {
        if (isApplying)
        {
            return;
        }

        foreach (UIElement element in trackedElements.ToArray())
        {
            if (engine.GetDependencies(element).Tokens.Contains(token))
            {
                Apply(element);
            }
        }
    }

    private bool AffectsAspect(UIElement element, UiProperty property)
    {
        AspectDependencySet dependencies = engine.GetDependencies(element);
        return property.Options.HasFlag(UiPropertyOptions.AffectsAspect) ||
            dependencies.Properties.Contains(property);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
