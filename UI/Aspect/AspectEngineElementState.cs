namespace Cerneala.UI.Aspect;

using Cerneala.UI.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Theming;

internal sealed class AspectEngineElementState
{
    // Counts the Clear calls, so that an apply sees a Clear that a property-change callback ran during that apply.
    public int ClearCount { get; set; }

    public ResolvedAspect? LastResolved { get; set; }

    public ThemeProvider? LastThemeProvider { get; set; }

    // The Motion transitions that this engine started for the element, by property.
    public Dictionary<UiProperty, MotionHandle> Animations { get; } = new(ReferenceEqualityComparer.Instance);

    public AspectRuleEvaluationSnapshot[] RuleEvaluations { get; set; } = [];

    public AspectEnvironment? DiagnosticsEnvironment { get; set; }

    public AspectEngineCounters DiagnosticsCounters { get; set; } = new();

    public AspectDiagnostics.Snapshot? Diagnostics { get; set; }
}

internal readonly record struct AspectRuleEvaluationSnapshot(
    AspectRuleSet Rule,
    IReadOnlyList<AspectConditionResult> Conditions,
    string Outcome);
