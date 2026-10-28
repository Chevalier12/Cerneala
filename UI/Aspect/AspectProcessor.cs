using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
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
    private readonly ConditionalWeakTable<Control, TemplateOwnerState> templateOwnerStates = new();
    private readonly ConditionalWeakTable<UIElement, DataSubscription> dataSubscriptions = new();
    private bool isApplying;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        engine = new AspectEngine(root.Relay);
    }

    public AspectEngine Engine => engine;

    public void Process(UIElement element)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        AspectCatalog catalog = GetCatalog(element);
        // A behavior factory that detached the element ended this attachment: the detach's Clear removed its Aspect
        // state, so nothing more is applied to it. A reattach queues the element's Aspect work again.
        BehaviorState attachment = SynchronizeBehaviors(element, catalog);
        if (attachment.IsDisposed)
        {
            return;
        }

        AspectEnvironment environment = GetEnvironment(element, catalog);
        if (element is ContentPresenter presenter)
        {
            presenter.RefreshAspectContentTemplates(catalog);
        }

        if (TemplateAspectContext.TryGet(element, out TemplateAspectContext.Registration registration))
        {
            ApplyEngine(() => engine.ApplyTemplateSlot(
                element,
                registration.Owner,
                catalog,
                environment,
                root.ThemeProvider,
                registration.Owner.AspectVariants,
                new AspectDataContext(element.DataContext, owner: registration.Owner),
                registration.SlotPath));
        }
        else
        {
            AspectVariantSet variants = element is Control control
                ? control.AspectVariants
                : AspectVariantSet.Empty;
            ApplyEngine(() => engine.Apply(
                element,
                catalog,
                environment,
                root.ThemeProvider,
                variants,
                new AspectDataContext(element.DataContext, owner: element)));
        }

        // A property-change callback detached the element during the apply: the same as a detaching behavior factory.
        if (attachment.IsDisposed)
        {
            return;
        }

        SynchronizeDataSubscription(element);
        if (element is Control templatedControl)
        {
            templatedControl.ApplyTemplateOutsideLayout();
            InvalidateTemplateSlotStates(templatedControl);
        }
    }

    private void InvalidateTemplateSlotStates(Control owner)
    {
        ComponentTemplateInstance? instance = owner.ComponentTemplateInstance;
        if (instance is null || !instance.Slots.Entries.Any())
        {
            return;
        }

        TemplateOwnerState state = templateOwnerStates.GetOrCreateValue(owner);
        AspectStateSet next = AspectStateSet.FromElement(owner);
        AspectStateSet? previous = state.States;
        AspectVariantSet nextVariants = owner.AspectVariants;
        AspectVariantSet? previousVariants = state.Variants;
        if (next.Equals(previous) && nextVariants.Equals(previousVariants))
        {
            return;
        }

        state.States = next;
        state.Variants = nextVariants;
        // Slot state and variant dependencies belong to the part, but read the template owner.
        // An unchanged template does not reattach or otherwise queue those parts.
        foreach ((AspectSlot _, UIElement part) in instance.Slots.Entries)
        {
            if (ReferenceEquals(part, owner) || !ReferenceEquals(part.Root, root) ||
                !TemplateAspectContext.TryGet(part, out TemplateAspectContext.Registration registration) ||
                !ReferenceEquals(registration.Owner, owner))
            {
                continue;
            }

            AspectDependencySet dependencies = engine.GetDependencies(part);
            if (dependencies.States.Any(dependency =>
                    previous is null || previous.Contains(dependency) != next.Contains(dependency)) ||
                dependencies.Variants.Any(dependency =>
                    previousVariants is null || VariantChanged(previousVariants, nextVariants, dependency)))
            {
                part.Invalidate(InvalidationFlags.Aspect, "Template owner Aspect state changed");
            }
        }
    }

    // A data condition reads the element's DataContext, so a PropertyChanged of that object for a declared property
    // dependency queues the element's Aspect work, as a DataContext change does. The subscription follows the
    // DataContext and the dependencies of the last apply, and the detach's Clear removes it.
    private void SynchronizeDataSubscription(UIElement element)
    {
        IReadOnlyList<AspectDataDependency> dependencies = engine.GetDependencies(element).Data;
        object? data = element.DataContext;
        string[] propertyNames = data is INotifyPropertyChanged
            ? dependencies
                .Where(dependency => dependency.PropertyName is not null &&
                    (dependency.OwnerType is null || dependency.OwnerType.IsInstanceOfType(data)))
                .Select(dependency => dependency.PropertyName!)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
            : [];
        if (propertyNames.Length == 0)
        {
            if (dataSubscriptions.TryGetValue(element, out DataSubscription? existing))
            {
                existing.Dispose();
                dataSubscriptions.Remove(element);
            }

            return;
        }

        dataSubscriptions
            .GetValue(element, key => new DataSubscription(root, key))
            .Update((INotifyPropertyChanged)data!, propertyNames);
    }

    private static bool VariantChanged(AspectVariantSet previous, AspectVariantSet next, AspectVariantKey key)
    {
        bool hadValue = previous.TryGetUntyped(key, out object? previousValue);
        bool hasValue = next.TryGetUntyped(key, out object? nextValue);
        return hadValue != hasValue || !Equals(previousValue, nextValue);
    }

    // Package behaviors (the program of default Aspects) attach with the
    // element, before Loaded, like an ElementAspect behavior; the next Aspect
    // pass reuses them.
    internal void AttachBehaviors(UIElement element)
    {
        root.Relay.VerifyAccess();
        SynchronizeBehaviors(element, GetCatalog(element));
    }

    // A throwing behavior lifetime does not skip the rest of the cleanup: the aspect-base values and the caches
    // are still cleared, then the first failure propagates.
    public void Clear(UIElement element)
    {
        root.Relay.VerifyAccess();
        ExceptionDispatchInfo? failure = null;
        if (behaviorStates.TryGetValue(element, out BehaviorState? behaviorState))
        {
            behaviorStates.Remove(element);
            try { behaviorState.Dispose(); }
            catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
        }

        try { ApplyEngine(() => engine.Clear(element)); }
        catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }

        if (dataSubscriptions.TryGetValue(element, out DataSubscription? dataSubscription))
        {
            dataSubscription.Dispose();
            dataSubscriptions.Remove(element);
        }

        catalogStates.Remove(element);
        environmentStates.Remove(element);
        if (element is Control control)
        {
            templateOwnerStates.Remove(control);
        }

        failure?.Throw();
    }

    internal void OnPropertyMutated(UiPropertyMutation mutation)
    {
        if (isApplying ||
            mutation.Target is not UIElement element ||
            !ReferenceEquals(element.Root, root) ||
            mutation.Property.AreEqualUntyped(mutation.OldEffectiveValue, mutation.NewEffectiveValue) ||
            !engine.GetDependencies(element).Properties.Contains(mutation.Property, ReferenceEqualityComparer.Instance))
        {
            return;
        }

        element.Invalidate(InvalidationFlags.Aspect, "Aspect property dependency changed");
    }

    private void ApplyEngine(Action action)
    {
        bool wasApplying = isApplying;
        isApplying = true;
        try
        {
            action();
        }
        finally
        {
            isApplying = wasApplying;
        }
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
        if (ReferenceEquals(state.Catalog, catalog) && ReferenceEquals(state.Theme, theme))
        {
            return state.Environment;
        }

        AspectEnvironment next = new(root.Relay, "runtime.element.next");
        ResolveTokenDefaults(catalog.FrameworkTokenDefaults, next);
        if (theme is not null)
        {
            ThemeTokenBridge.Apply(theme, next);
        }

        ResolveTokenDefaults(catalog.ExplicitTokenDefaults, next);

        state.Environment.ReplaceWith(next);
        state.Catalog = catalog;
        state.Theme = theme;
        return state.Environment;
    }

    private void ResolveTokenDefaults(
        IReadOnlyDictionary<AspectToken, AspectValue> defaults,
        AspectEnvironment environment)
    {
        foreach ((AspectToken token, AspectValue defaultValue) in defaults)
        {
            object? resolved = defaultValue.Resolve(new AspectResolutionContext(
                root,
                environment,
                AspectStateSet.Empty,
                AspectVariantSet.Empty,
                root.ThemeProvider));
            environment.Set(token, resolved);
        }
    }

    private AspectCatalog GetCatalog(UIElement element)
    {
        return GetCatalogState(element).Catalog;
    }

    // The returned state belongs to the element's current attachment: the detach's Clear disposes it, also when a
    // behavior factory detached the element here.
    private BehaviorState SynchronizeBehaviors(UIElement element, AspectCatalog catalog)
    {
        BehaviorState state = behaviorStates.GetValue(element, _ => new BehaviorState());
        AspectBehavior[] desired = catalog.Behaviors.Where(behavior => behavior.Matches(element)).ToArray();
        state.Synchronize(element, desired);
        return state;
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
            : AspectCatalog.Compose(rootCatalog, sources);
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

    private readonly record struct ScopeStamp(ResourceDictionary Resources, long Version);

    private sealed class TemplateOwnerState
    {
        public AspectStateSet? States { get; set; }

        public AspectVariantSet? Variants { get; set; }
    }

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

    // A PropertyChanged whose PropertyName is null or empty reports that every property changed. An event raised on
    // another thread is posted to the root's thread, and only an element still in the root with this source is queued.
    private sealed class DataSubscription(UIRoot root, UIElement element) : IDisposable
    {
        private INotifyPropertyChanged? source;
        private string[] propertyNames = [];

        public void Update(INotifyPropertyChanged nextSource, string[] nextPropertyNames)
        {
            propertyNames = nextPropertyNames;
            if (ReferenceEquals(source, nextSource))
            {
                return;
            }

            Unsubscribe();
            source = nextSource;
            source.PropertyChanged += OnPropertyChanged;
        }

        public void Dispose()
        {
            Unsubscribe();
            propertyNames = [];
        }

        private void Unsubscribe()
        {
            if (source is not null)
            {
                source.PropertyChanged -= OnPropertyChanged;
                source = null;
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.PropertyName) && Array.IndexOf(propertyNames, args.PropertyName) < 0)
            {
                return;
            }

            if (root.Relay.CheckAccess())
            {
                Invalidate(source);
                return;
            }

            INotifyPropertyChanged? observed = source;
            root.Relay.Post(() => Invalidate(observed));
        }

        private void Invalidate(INotifyPropertyChanged? observed)
        {
            if (observed is not null && ReferenceEquals(source, observed) && ReferenceEquals(element.Root, root))
            {
                element.Invalidate(InvalidationFlags.Aspect, "Aspect data dependency changed");
            }
        }
    }

    private sealed class EnvironmentState(AspectEnvironment environment)
    {
        public AspectEnvironment Environment { get; } = environment;

        public AspectCatalog? Catalog { get; set; }

        public Theme? Theme { get; set; }
    }

    private sealed class BehaviorState : IDisposable
    {
        private List<AppliedBehavior> applied = [];
        private bool isDisposed;

        public bool IsDisposed => isDisposed;

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
                    if (isDisposed)
                    {
                        break;
                    }
                }
            }
            catch
            {
                // The attach failure came first; a rolled-back lifetime that also throws does not replace it.
                ExceptionDispatchInfo? rollbackFailure = null;
                DisposeReverse(attached, ref rollbackFailure);
                throw;
            }

            // The factory detached the element: the detach disposed this state's lifetimes and dropped the state, so
            // the lifetimes attached here belong to an attachment that is already gone. The next attach creates new ones.
            if (isDisposed)
            {
                ExceptionDispatchInfo? detachFailure = null;
                DisposeReverse(attached, ref detachFailure);
                detachFailure?.Throw();
                return;
            }

            // A lifetime is finished once its disposal starts, also when Dispose throws: the new state is stored
            // first, every removed lifetime is disposed once, then the first failure propagates.
            List<AppliedBehavior> previous = applied;
            applied = next;
            ExceptionDispatchInfo? failure = null;
            for (int index = previous.Count - 1; index >= 0; index--)
            {
                if (!reused[index])
                {
                    DisposeLifetime(previous[index], ref failure);
                }
            }

            failure?.Throw();
        }

        public void Dispose()
        {
            isDisposed = true;
            List<AppliedBehavior> previous = applied;
            applied = [];
            ExceptionDispatchInfo? failure = null;
            DisposeReverse(previous, ref failure);
            failure?.Throw();
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

        private static void DisposeReverse(IReadOnlyList<AppliedBehavior> values, ref ExceptionDispatchInfo? failure)
        {
            for (int index = values.Count - 1; index >= 0; index--)
            {
                DisposeLifetime(values[index], ref failure);
            }
        }

        private static void DisposeLifetime(AppliedBehavior value, ref ExceptionDispatchInfo? failure)
        {
            try { value.Lifetime?.Dispose(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
        }

        private readonly record struct AppliedBehavior(AspectBehavior Behavior, IDisposable? Lifetime);
    }
}
