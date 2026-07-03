using System.Runtime.CompilerServices;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleApplicator
{
    private readonly ConditionalWeakTable<UIElement, AppliedStyleState> states = new();

    public StyleApplicationResult Apply(UIElement element, StyleSheet styleSheet, ThemeProvider? themeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(styleSheet);

        AppliedStyleState state = states.GetOrCreateValue(element);
        Dictionary<UiProperty, AppliedSetter> nextBase = [];
        Dictionary<UiProperty, AppliedSetter> nextVisual = [];
        List<StyleRule> matchedRules = [];

        foreach (StyleRule rule in styleSheet.Rules)
        {
            if (!rule.Matches(element))
            {
                continue;
            }

            matchedRules.Add(rule);
            Dictionary<UiProperty, AppliedSetter> target = rule.Source == UiPropertyValueSource.StyleVisualState
                ? nextVisual
                : nextBase;

            foreach (Setter setter in rule.Setters)
            {
                target[setter.Property] = new AppliedSetter(setter, rule);
            }
        }

        List<StyleDiagnostics.AppliedValue> appliedValues = [];
        List<StyleDiagnostics.ClearedValue> clearedValues = [];
        ApplySource(element, UiPropertyValueSource.StyleBase, state.BaseValues, nextBase, themeProvider, appliedValues, clearedValues);
        ApplySource(element, UiPropertyValueSource.StyleVisualState, state.VisualStateValues, nextVisual, themeProvider, appliedValues, clearedValues);

        state.LastMatchedRules = matchedRules;
        state.LastAppliedValues = appliedValues;
        state.LastClearedValues = clearedValues;
        return new StyleApplicationResult(matchedRules, appliedValues, clearedValues);
    }

    internal void Clear(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!states.TryGetValue(element, out AppliedStyleState? state))
        {
            return;
        }

        ClearSource(element, UiPropertyValueSource.StyleBase, state.BaseValues);
        ClearSource(element, UiPropertyValueSource.StyleVisualState, state.VisualStateValues);
        states.Remove(element);
    }

    public StyleDiagnostics.Snapshot GetDiagnostics(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!states.TryGetValue(element, out AppliedStyleState? state))
        {
            return new StyleDiagnostics.Snapshot([], [], []);
        }

        return new StyleDiagnostics.Snapshot(state.LastMatchedRules, state.LastAppliedValues, state.LastClearedValues);
    }

    private static void ApplySource(
        UIElement element,
        UiPropertyValueSource source,
        Dictionary<UiProperty, AppliedSetter> previous,
        Dictionary<UiProperty, AppliedSetter> next,
        ThemeProvider? themeProvider,
        List<StyleDiagnostics.AppliedValue> appliedValues,
        List<StyleDiagnostics.ClearedValue> clearedValues)
    {
        foreach ((UiProperty property, AppliedSetter applied) in next)
        {
            applied.Setter.Apply(element, source, themeProvider);
            appliedValues.Add(new StyleDiagnostics.AppliedValue(
                property,
                element.GetValue(property),
                element.GetValueSource(property),
                applied.Rule));
        }

        foreach ((UiProperty property, AppliedSetter old) in previous)
        {
            if (next.ContainsKey(property))
            {
                continue;
            }

            old.Setter.Clear(element, source);
            clearedValues.Add(new StyleDiagnostics.ClearedValue(property, source, old.Rule));
        }

        previous.Clear();
        foreach ((UiProperty property, AppliedSetter applied) in next)
        {
            previous[property] = applied;
        }
    }

    private static void ClearSource(
        UIElement element,
        UiPropertyValueSource source,
        Dictionary<UiProperty, AppliedSetter> previous)
    {
        foreach (AppliedSetter old in previous.Values)
        {
            old.Setter.Clear(element, source);
        }

        previous.Clear();
    }

    private sealed class AppliedStyleState
    {
        public Dictionary<UiProperty, AppliedSetter> BaseValues { get; } = [];

        public Dictionary<UiProperty, AppliedSetter> VisualStateValues { get; } = [];

        public IReadOnlyList<StyleRule> LastMatchedRules { get; set; } = [];

        public IReadOnlyList<StyleDiagnostics.AppliedValue> LastAppliedValues { get; set; } = [];

        public IReadOnlyList<StyleDiagnostics.ClearedValue> LastClearedValues { get; set; } = [];
    }

    private readonly record struct AppliedSetter(Setter Setter, StyleRule Rule);
}

public sealed class StyleApplicationResult
{
    public StyleApplicationResult(
        IReadOnlyList<StyleRule> matchedRules,
        IReadOnlyList<StyleDiagnostics.AppliedValue> appliedValues,
        IReadOnlyList<StyleDiagnostics.ClearedValue> clearedValues)
    {
        MatchedRules = matchedRules;
        AppliedValues = appliedValues;
        ClearedValues = clearedValues;
    }

    public IReadOnlyList<StyleRule> MatchedRules { get; }

    public IReadOnlyList<StyleDiagnostics.AppliedValue> AppliedValues { get; }

    public IReadOnlyList<StyleDiagnostics.ClearedValue> ClearedValues { get; }
}
