using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Styling;

namespace Cerneala.UI.Diagnostics;

public static class StyleTrace
{
    public static StyleTraceSnapshot Capture(UIElement element, UiProperty property, StyleDiagnostics.Snapshot? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);

        object? effectiveValue = element.GetValue(property);
        UiPropertyValueSource effectiveSource = element.GetValueSource(property);
        IReadOnlyList<StyleTraceRule> matchedRules = diagnostics?.MatchedRules
            .Select(rule => new StyleTraceRule(rule.Selector.Description, rule.Source, rule.IsVisualStateRule))
            .ToArray() ?? [];
        IReadOnlyList<StyleTraceAppliedValue> appliedValues = diagnostics?.AppliedValues
            .Where(value => ReferenceEquals(value.Property, property))
            .Select(value => new StyleTraceAppliedValue(value.Property.DiagnosticName, value.Value, value.Source, value.Rule.Selector.Description))
            .ToArray() ?? [];
        IReadOnlyList<StyleTraceClearedValue> clearedValues = diagnostics?.ClearedValues
            .Where(value => ReferenceEquals(value.Property, property))
            .Select(value => new StyleTraceClearedValue(value.Property.DiagnosticName, value.Source, value.Rule.Selector.Description))
            .ToArray() ?? [];

        return new StyleTraceSnapshot(
            element.ElementId?.ToString(),
            element.GetType().Name,
            property.DiagnosticName,
            effectiveValue,
            effectiveSource,
            matchedRules,
            appliedValues,
            clearedValues);
    }
}

public sealed record StyleTraceSnapshot(
    string? ElementId,
    string ElementType,
    string PropertyName,
    object? EffectiveValue,
    UiPropertyValueSource EffectiveSource,
    IReadOnlyList<StyleTraceRule> MatchedRules,
    IReadOnlyList<StyleTraceAppliedValue> AppliedValues,
    IReadOnlyList<StyleTraceClearedValue> ClearedValues)
{
    public override string ToString()
    {
        return $"{ElementType}#{ElementId ?? "unattached"} {PropertyName}={EffectiveValue ?? "null"} source={EffectiveSource} matchedRules={MatchedRules.Count} applied={AppliedValues.Count} cleared={ClearedValues.Count}";
    }
}

public sealed record StyleTraceRule(string Selector, UiPropertyValueSource Source, bool IsVisualStateRule);

public sealed record StyleTraceAppliedValue(string PropertyName, object? Value, UiPropertyValueSource Source, string RuleSelector);

public sealed record StyleTraceClearedValue(string PropertyName, UiPropertyValueSource Source, string RuleSelector);
