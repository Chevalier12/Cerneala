using System.Runtime.CompilerServices;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.States;
using Cerneala.UI.Relay;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectEngine
{
    private readonly ConditionalWeakTable<UIElement, AspectEngineElementState> states = new();
    private readonly AspectInvalidationGraph invalidationGraph = new();
    private readonly AspectEngineCounters counters = new();
    private readonly IUiThreadAccess threadAccess;

    public AspectEngine()
        : this(new CapturedUiThreadAccess())
    {
    }

    internal AspectEngine(IUiThreadAccess threadAccess)
    {
        this.threadAccess = threadAccess ?? throw new ArgumentNullException(nameof(threadAccess));
    }

    public AspectEngineCounters Counters => counters;

    public AspectApplicationResult Apply(
        UIElement element,
        AspectCatalog catalog,
        AspectEnvironment environment,
        ThemeProvider? themeProvider = null,
        AspectVariantSet? variants = null,
        AspectDataContext? dataContext = null,
        AspectSlotPath? slotPath = null)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        ResolvedAspect resolved = Resolve(element, catalog, environment, themeProvider, variants, dataContext, slotPath);
        AspectEngineElementState state = states.GetOrCreateValue(element);
        bool changed = ApplyResolved(element, state.LastResolved, resolved, themeProvider);
        state.LastResolved = resolved;
        state.LastThemeProvider = themeProvider;
        state.Diagnostics = BuildDiagnostics(resolved, environment, counters.Snapshot());
        invalidationGraph.Track(element, resolved.Dependencies);
        return new AspectApplicationResult(changed, resolved);
    }

    public ResolvedAspect Resolve(
        UIElement element,
        AspectCatalog catalog,
        AspectEnvironment environment,
        ThemeProvider? themeProvider = null,
        AspectVariantSet? variants = null,
        AspectDataContext? dataContext = null,
        AspectSlotPath? slotPath = null)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(environment);

        AspectStateSet states = AspectStateSet.FromElement(element);
        AspectMatchContext matchContext = new(
            element,
            ownerComponent: element,
            slotPath: slotPath,
            states: states,
            variants: variants ?? AspectVariantSet.Empty,
            environmentVersion: environment.Version,
            dataContext: dataContext ?? AspectDataContext.Empty);
        AspectResolutionContext resolutionContext = new(element, environment, states, variants, themeProvider);

        Dictionary<UiProperty, ResolvedAspectValue> winners = new(ReferenceEqualityComparer.Instance);
        List<RejectedAspectDeclaration> rejected = [];
        List<AspectRuleSet> matchedRules = [];
        List<AspectConditionDependency> conditionDependencies = [];
        List<AspectToken> tokenDependencies = [];

        foreach (AspectRuleSet rule in catalog.Rules)
        {
            counters.RulesConsidered++;
            IReadOnlyList<AspectConditionResult> conditionResults = rule.Target.Conditions.Select(condition => condition.Evaluate(matchContext)).ToArray();
            conditionDependencies.AddRange(conditionResults.SelectMany(result => result.Dependencies));
            if (!rule.Target.Matches(matchContext))
            {
                continue;
            }

            counters.RulesMatched++;
            matchedRules.Add(rule);
            AspectCascadeKey cascadeKey = new(rule.Layer.Order, rule.Target.Specificity, rule.DeclarationOrder);
            AspectMotionSource motionSource = GetMotionSource(conditionResults);
            foreach (AspectDeclaration declaration in rule.Declarations)
            {
                counters.DeclarationsResolved++;
                counters.TokenLookups += declaration.Value.Dependencies.Count;
                object? value = declaration.Value.Resolve(resolutionContext);
                tokenDependencies.AddRange(declaration.Value.Dependencies);
                ResolvedAspectValue resolvedValue = new(
                    declaration.Property,
                    value,
                    declaration,
                    cascadeKey,
                    declaration.Motion,
                    motionSource);
                if (!winners.TryGetValue(declaration.Property, out ResolvedAspectValue? current))
                {
                    winners[declaration.Property] = resolvedValue;
                    continue;
                }

                if (cascadeKey.CompareTo(current.CascadeKey) > 0)
                {
                    rejected.Add(new RejectedAspectDeclaration(current.SourceDeclaration, declaration, "Higher cascade key won."));
                    winners[declaration.Property] = resolvedValue;
                }
                else
                {
                    rejected.Add(new RejectedAspectDeclaration(declaration, current.SourceDeclaration, "Existing cascade key won."));
                }
            }
        }

        AspectDependencySet dependencies = new(
            tokenDependencies.Distinct().ToArray(),
            conditionDependencies.Where(dependency => dependency.State is not null).Select(dependency => dependency.State!).Distinct().ToArray(),
            conditionDependencies.Where(dependency => dependency.Variant is not null).Select(dependency => dependency.Variant!).Distinct().ToArray(),
            conditionDependencies.Where(dependency => dependency.Property is not null).Select(dependency => dependency.Property!).Distinct().ToArray(),
            conditionDependencies.Where(dependency => dependency.Data is not null).Select(dependency => dependency.Data!).Distinct().ToArray(),
            matchContext.SlotPath?.Slot,
            catalog.Version,
            environment.Version);

        return new ResolvedAspect(winners, matchedRules, rejected, dependencies);
    }

    private static AspectDiagnostics.Snapshot BuildDiagnostics(
        ResolvedAspect resolved,
        AspectEnvironment environment,
        AspectEngineCounters counters)
    {
        List<AspectResolutionStep> steps = [];
        foreach (AspectRuleSet rule in resolved.MatchedRules)
        {
            steps.Add(new AspectResolutionStep(
                rule.PackageName ?? string.Empty,
                rule.Name,
                rule.Target.ToString(),
                rule.Layer,
                rule.Target.Specificity,
                rule.DeclarationOrder,
                "matched"));
        }

        foreach (RejectedAspectDeclaration rejected in resolved.RejectedDeclarations)
        {
            steps.Add(new AspectResolutionStep(
                string.Empty,
                rejected.Rejected.DiagnosticName ?? rejected.Rejected.Property.Name,
                rejected.Rejected.Property.DiagnosticName,
                AspectLayer.Reset,
                new AspectSpecificity(),
                0,
                $"rejected: {rejected.Reason}"));
        }

        List<AspectTokenTrace> tokenTraces = [];
        foreach (ResolvedAspectValue value in resolved.Values.Values)
        {
            foreach (AspectToken token in value.SourceDeclaration.Value.Dependencies)
            {
                tokenTraces.Add(new AspectTokenTrace(token, environment.Name, value.Value, value.Value));
            }
        }

        return new AspectDiagnostics.Snapshot(resolved, steps, tokenTraces, counters);
    }

    public AspectDiagnostics.Snapshot GetDiagnostics(UIElement element)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        return states.TryGetValue(element, out AspectEngineElementState? state)
            ? state.Diagnostics
            : new AspectDiagnostics.Snapshot();
    }

    public AspectDependencySet GetDependencies(UIElement element)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        return invalidationGraph.TryGetDependencies(element, out AspectDependencySet dependencySet)
            ? dependencySet
            : new AspectDependencySet();
    }

    public void Clear(UIElement element)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(element);
        if (!states.TryGetValue(element, out AspectEngineElementState? state) || state.LastResolved is null)
        {
            return;
        }

        foreach ((UiProperty property, ResolvedAspectValue resolvedValue) in state.LastResolved.Values)
        {
            ApplyMutation(
                element,
                resolvedValue,
                state.LastThemeProvider,
                () => element.ClearValueUntyped(property, UiPropertyValueSource.AspectBase));
        }

        state.LastResolved = null;
        state.LastThemeProvider = null;
        state.Diagnostics = new AspectDiagnostics.Snapshot();
        invalidationGraph.Untrack(element);
    }

    internal void VerifyAccess() => threadAccess.VerifyAccess();

    private static bool ApplyResolved(
        UIElement element,
        ResolvedAspect? previous,
        ResolvedAspect next,
        ThemeProvider? themeProvider)
    {
        bool changed = false;
        foreach (UiProperty property in previous?.Values.Keys ?? [])
        {
            if (!next.Values.ContainsKey(property))
            {
                ResolvedAspectValue previousValue = previous!.Values[property];
                ApplyMutation(
                    element,
                    previousValue,
                    themeProvider,
                    () => element.ClearValueUntyped(property, UiPropertyValueSource.AspectBase));
                changed = true;
            }
        }

        foreach ((UiProperty property, ResolvedAspectValue resolvedValue) in next.Values)
        {
            object? oldSourceValue = element.GetSourceValue(property, UiPropertyValueSource.AspectBase);
            if (element.GetValueSource(property) != UiPropertyValueSource.AspectBase ||
                !property.AreEqualUntyped(oldSourceValue, resolvedValue.Value))
            {
                ResolvedAspectValue? previousValue = null;
                previous?.Values.TryGetValue(property, out previousValue);
                ResolvedAspectValue? motionValue = GetMotionValue(resolvedValue, previousValue);
                ApplyMutation(
                    element,
                    motionValue,
                    themeProvider,
                    () => element.SetValueUntyped(property, resolvedValue.Value, UiPropertyValueSource.AspectBase));
                changed = true;
            }
        }

        return changed;
    }

    private static AspectMotionSource GetMotionSource(IReadOnlyList<AspectConditionResult> conditionResults)
    {
        AspectMotionSource source = AspectMotionSource.None;
        foreach (AspectConditionDependency dependency in conditionResults.SelectMany(result => result.Dependencies))
        {
            source |= dependency.Kind switch
            {
                AspectConditionDependencyKind.State => AspectMotionSource.State,
                AspectConditionDependencyKind.UiProperty => AspectMotionSource.State,
                AspectConditionDependencyKind.Predicate => AspectMotionSource.State,
                AspectConditionDependencyKind.Variant => AspectMotionSource.Variant,
                AspectConditionDependencyKind.DataContext => AspectMotionSource.Data,
                _ => AspectMotionSource.None
            };
        }

        return source == AspectMotionSource.None ? AspectMotionSource.Base : source;
    }

    private static ResolvedAspectValue? GetMotionValue(
        ResolvedAspectValue next,
        ResolvedAspectValue? previous)
    {
        if (HasApplicableMotion(next))
        {
            return next;
        }

        return previous is not null && HasApplicableMotion(previous) ? previous : null;
    }

    private static bool HasApplicableMotion(ResolvedAspectValue value)
    {
        return value.Motion is not null &&
            ReferenceEquals(value.Motion.Property, value.Property) &&
            (value.Motion.Source & value.MotionSource) != AspectMotionSource.None;
    }

    private static void ApplyMutation(
        UIElement element,
        ResolvedAspectValue? motionValue,
        ThemeProvider? themeProvider,
        Action mutation)
    {
        if (motionValue?.Motion is not { } motion ||
            element.Root is null ||
            themeProvider is null ||
            !HasApplicableMotion(motionValue))
        {
            mutation();
            return;
        }

        using (element.Root.Motion.BeginTransaction(ThemeMotionTokens.Resolve(themeProvider, motion.TokenName)))
        {
            mutation();
        }
    }
}

public sealed record AspectApplicationResult(bool Applied, ResolvedAspect ResolvedAspect);
