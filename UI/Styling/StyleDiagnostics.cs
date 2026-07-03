using Cerneala.UI.Core;

namespace Cerneala.UI.Styling;

public static class StyleDiagnostics
{
    public sealed class Snapshot
    {
        public Snapshot(
            IReadOnlyList<StyleRule> matchedRules,
            IReadOnlyList<AppliedValue> appliedValues,
            IReadOnlyList<ClearedValue> clearedValues)
        {
            MatchedRules = matchedRules;
            AppliedValues = appliedValues;
            ClearedValues = clearedValues;
        }

        public IReadOnlyList<StyleRule> MatchedRules { get; }

        public IReadOnlyList<AppliedValue> AppliedValues { get; }

        public IReadOnlyList<ClearedValue> ClearedValues { get; }
    }

    public sealed record AppliedValue(UiProperty Property, object? Value, UiPropertyValueSource Source, StyleRule Rule);

    public sealed record ClearedValue(UiProperty Property, UiPropertyValueSource Source, StyleRule Rule);
}
