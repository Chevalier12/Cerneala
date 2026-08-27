using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectProcessor
{
    private readonly UIRoot root;
    private readonly AspectEngine engine;
    private readonly ConditionalWeakTable<UIElement, CatalogState> catalogStates = new();
    private readonly ConditionalWeakTable<UIElement, EnvironmentState> environmentStates = new();
    private readonly ConditionalWeakTable<UIElement, BehaviorState> behaviorStates = new();
    private int nextCompositeVersion = 1_000_000;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        engine = new AspectEngine(root.Relay);
    }

    public AspectEngine Engine => engine;

    internal AspectEnvironment Environment => GetEnvironment(root);

    public void Process(UIElement element)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        AspectCatalog catalog = GetCatalog(element);
        SynchronizeBehaviors(element, catalog);
        AspectEnvironment environment = GetEnvironment(element, catalog);
        if (element is ContentPresenter presenter)
        {
            presenter.RefreshAspectContentTemplates(catalog.Version);
        }

        if (TemplateAspectContext.TryGet(element, out TemplateAspectContext.Registration registration))
        {
            engine.ApplyTemplateSlot(
                element,
                registration.Owner,
                catalog,
                environment,
                root.ThemeProvider,
                registration.Owner.AspectVariants,
                new AspectDataContext(element.DataContext, owner: registration.Owner),
                registration.SlotPath);
        }
        else
        {
            AspectVariantSet variants = element is Control control
                ? control.AspectVariants
                : AspectVariantSet.Empty;
            engine.Apply(
                element,
                catalog,
                environment,
                root.ThemeProvider,
                variants,
                new AspectDataContext(element.DataContext, owner: element));
        }

        if (element is Control templatedControl)
        {
            templatedControl.ApplyTemplate();
        }
    }

    public void Clear(UIElement element)
    {
        root.Relay.VerifyAccess();
        if (behaviorStates.TryGetValue(element, out BehaviorState? behaviorState))
        {
            behaviorState.Dispose();
            behaviorStates.Remove(element);
        }

        engine.Clear(element);
        catalogStates.Remove(element);
        environmentStates.Remove(element);
    }

    internal ComponentTemplate? ResolveComponentTemplate(Control owner, string? key)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(owner);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        ComponentTemplateDefinition? best = null;
        int bestDistance = int.MaxValue;
        int bestIndex = -1;
        IReadOnlyList<ComponentTemplateDefinition> definitions = GetCatalog(owner).ComponentTemplates;
        for (int index = 0; index < definitions.Count; index++)
        {
            ComponentTemplateDefinition candidate = definitions[index];
            if (!string.Equals(candidate.Name, key, StringComparison.Ordinal) ||
                !candidate.OwnerType.IsAssignableFrom(owner.GetType()) ||
                candidate.Template is null)
            {
                continue;
            }

            int distance = InheritanceDistance(owner.GetType(), candidate.OwnerType);
            if (distance < bestDistance || distance == bestDistance && index > bestIndex)
            {
                best = candidate;
                bestDistance = distance;
                bestIndex = index;
            }
        }

        if (best is null)
        {
            return null;
        }

        if (best.Template is not ComponentTemplate template)
        {
            throw new InvalidOperationException(
                $"Aspect component template '{best.Name}' must contain a ComponentTemplate value.");
        }

        if (!template.OwnerType.IsInstanceOfType(owner))
        {
            throw new InvalidOperationException(
                $"Aspect component template '{best.Name}' cannot be applied to '{owner.GetType().FullName}'.");
        }

        return template;
    }

    internal ContentTemplateRegistry GetContentTemplates(UIElement element)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        CatalogState state = GetCatalogState(element);
        if (state.ContentTemplates is not null)
        {
            return state.ContentTemplates;
        }

        ContentTemplateRegistry next = new();
        foreach (ContentTemplateDefinition definition in state.Catalog.ContentTemplates)
        {
            if (definition.Template is null)
            {
                continue;
            }

            if (definition.Template is not ContentTemplate template)
            {
                throw new InvalidOperationException(
                    $"Aspect content template '{definition.Name}' must contain a ContentTemplate value.");
            }

            next.Register(template);
        }

        state.ContentTemplates = next;
        return next;
    }

    private static int InheritanceDistance(Type actualType, Type targetType)
    {
        if (actualType == targetType)
        {
            return 0;
        }

        int distance = 1;
        for (Type? current = actualType.BaseType; current is not null; current = current.BaseType)
        {
            if (current == targetType)
            {
                return distance;
            }

            distance++;
        }

        return targetType.IsInterface ? 10_000 : int.MaxValue;
    }

    internal AspectEnvironment GetEnvironment(UIElement element)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        return GetEnvironment(element, GetCatalog(element));
    }

    private AspectEnvironment GetEnvironment(UIElement element, AspectCatalog catalog)
    {
        EnvironmentState state = environmentStates.GetValue(
            element,
            _ => new EnvironmentState(new AspectEnvironment(root.Relay, "runtime.element")));
        Theme? theme = root.ThemeProvider?.Theme;
        if (state.CatalogVersion == catalog.Version && ReferenceEquals(state.Theme, theme))
        {
            return state.Environment;
        }

        AspectEnvironment next = new(root.Relay, "runtime.element.next");
        foreach ((AspectToken token, AspectValue defaultValue) in catalog.TokenDefaults)
        {
            object? resolved = defaultValue.Resolve(new AspectResolutionContext(
                root,
                next,
                AspectStateSet.Empty,
                AspectVariantSet.Empty,
                root.ThemeProvider));
            next.Set(token, resolved);
        }

        if (theme is not null)
        {
            ThemeTokenBridge.Apply(theme, next);
        }

        state.Environment.ReplaceWith(next);
        state.CatalogVersion = catalog.Version;
        state.Theme = theme;
        return state.Environment;
    }

    private AspectCatalog GetCatalog(UIElement element)
    {
        return GetCatalogState(element).Catalog;
    }

    private void SynchronizeBehaviors(UIElement element, AspectCatalog catalog)
    {
        BehaviorState state = behaviorStates.GetValue(element, _ => new BehaviorState());
        AspectBehavior[] desired = catalog.Behaviors.Where(behavior => behavior.Matches(element)).ToArray();
        state.Synchronize(element, desired);
    }

    private CatalogState GetCatalogState(UIElement element)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        AspectCatalog rootCatalog = root.AspectRegistry.BuildCatalog();
        ResourceDictionary? applicationResources = root.ResourceProvider as ResourceDictionary;
        long applicationVersion = applicationResources?.Version ?? 0;
        List<ScopeStamp> scopes = [];
        for (UIElement? current = element;
             current is not null && !ReferenceEquals(current, root);
             current = current.LogicalParent ?? current.VisualParent)
        {
            scopes.Add(new ScopeStamp(current.Resources, current.Resources.Version));
        }

        scopes.Reverse();
        ElementAspect? localAspect = element.Aspect;
        int localAspectVersion = localAspect?.Version ?? 0;
        CatalogState state = catalogStates.GetValue(element, _ => new CatalogState());
        if (state.Matches(
            rootCatalog,
            applicationResources,
            applicationVersion,
            scopes,
            localAspect,
            localAspectVersion))
        {
            return state;
        }

        List<AspectPackageSource> sources = [];
        int sourceOrder = 1;
        AddPackages(applicationResources, sourceOrder++, "application", sources);
        for (int index = 0; index < scopes.Count; index++)
        {
            AddPackages(scopes[index].Resources, sourceOrder++, $"scope[{index}]", sources);
        }

        if (localAspect is not null)
        {
            sources.Add(new AspectPackageSource(localAspect.Package, sourceOrder, "element"));
        }

        AspectCatalog catalog = sources.Count == 0
            ? rootCatalog
            : AspectCatalog.Compose(rootCatalog, sources, NextCompositeVersion());
        state.Update(
            catalog,
            rootCatalog,
            applicationResources,
            applicationVersion,
            scopes,
            localAspect,
            localAspectVersion);
        return state;
    }

    private static void AddPackages(
        ResourceDictionary? resources,
        int sourceOrder,
        string scope,
        List<AspectPackageSource> sources)
    {
        if (resources is null)
        {
            return;
        }

        foreach (AspectPackage package in resources.Values.OfType<AspectPackage>())
        {
            sources.Add(new AspectPackageSource(package, sourceOrder, scope));
        }
    }

    private int NextCompositeVersion()
    {
        if (nextCompositeVersion == int.MaxValue)
        {
            throw new InvalidOperationException("Aspect composite catalog version space was exhausted.");
        }

        return nextCompositeVersion++;
    }

    private readonly record struct ScopeStamp(ResourceDictionary Resources, long Version);

    private sealed class CatalogState
    {
        private AspectCatalog? rootCatalog;
        private ResourceDictionary? applicationResources;
        private long applicationVersion;
        private ScopeStamp[] scopes = [];
        private ElementAspect? localAspect;
        private int localAspectVersion;

        public AspectCatalog Catalog { get; private set; } = null!;

        public ContentTemplateRegistry? ContentTemplates { get; set; }

        public bool Matches(
            AspectCatalog nextRootCatalog,
            ResourceDictionary? nextApplicationResources,
            long nextApplicationVersion,
            IReadOnlyList<ScopeStamp> nextScopes,
            ElementAspect? nextLocalAspect,
            int nextLocalAspectVersion)
        {
            if (Catalog is null ||
                !ReferenceEquals(rootCatalog, nextRootCatalog) ||
                !ReferenceEquals(applicationResources, nextApplicationResources) ||
                applicationVersion != nextApplicationVersion ||
                !ReferenceEquals(localAspect, nextLocalAspect) ||
                localAspectVersion != nextLocalAspectVersion ||
                scopes.Length != nextScopes.Count)
            {
                return false;
            }

            for (int index = 0; index < scopes.Length; index++)
            {
                if (!ReferenceEquals(scopes[index].Resources, nextScopes[index].Resources) ||
                    scopes[index].Version != nextScopes[index].Version)
                {
                    return false;
                }
            }

            return true;
        }

        public void Update(
            AspectCatalog catalog,
            AspectCatalog nextRootCatalog,
            ResourceDictionary? nextApplicationResources,
            long nextApplicationVersion,
            IReadOnlyList<ScopeStamp> nextScopes,
            ElementAspect? nextLocalAspect,
            int nextLocalAspectVersion)
        {
            Catalog = catalog;
            rootCatalog = nextRootCatalog;
            applicationResources = nextApplicationResources;
            applicationVersion = nextApplicationVersion;
            scopes = nextScopes.ToArray();
            localAspect = nextLocalAspect;
            localAspectVersion = nextLocalAspectVersion;
            ContentTemplates = null;
        }
    }

    private sealed class EnvironmentState(AspectEnvironment environment)
    {
        public AspectEnvironment Environment { get; } = environment;

        public int CatalogVersion { get; set; } = -1;

        public Theme? Theme { get; set; }
    }

    private sealed class BehaviorState : IDisposable
    {
        private List<AppliedBehavior> applied = [];

        public void Synchronize(UIElement element, IReadOnlyList<AspectBehavior> desired)
        {
            bool[] reused = new bool[applied.Count];
            List<AppliedBehavior> next = new(desired.Count);
            List<AppliedBehavior> attached = [];
            try
            {
                foreach (AspectBehavior behavior in desired)
                {
                    int existingIndex = FindReusable(behavior, reused);
                    if (existingIndex >= 0)
                    {
                        reused[existingIndex] = true;
                        next.Add(applied[existingIndex]);
                        continue;
                    }

                    AppliedBehavior added = new(behavior, behavior.Attach(element));
                    attached.Add(added);
                    next.Add(added);
                }
            }
            catch
            {
                DisposeReverse(attached);
                throw;
            }

            for (int index = applied.Count - 1; index >= 0; index--)
            {
                if (!reused[index])
                {
                    applied[index].Lifetime?.Dispose();
                }
            }

            applied = next;
        }

        public void Dispose()
        {
            DisposeReverse(applied);
            applied = [];
        }

        private int FindReusable(AspectBehavior behavior, IReadOnlyList<bool> reused)
        {
            for (int index = 0; index < applied.Count; index++)
            {
                if (!reused[index] && ReferenceEquals(applied[index].Behavior, behavior))
                {
                    return index;
                }
            }

            return -1;
        }

        private static void DisposeReverse(IReadOnlyList<AppliedBehavior> values)
        {
            for (int index = values.Count - 1; index >= 0; index--)
            {
                values[index].Lifetime?.Dispose();
            }
        }

        private readonly record struct AppliedBehavior(AspectBehavior Behavior, IDisposable? Lifetime);
    }
}
