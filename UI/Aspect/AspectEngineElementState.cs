namespace Cerneala.UI.Aspect;

using Cerneala.UI.Detective;
using Cerneala.UI.Theming;

internal sealed class AspectEngineElementState
{
    public ResolvedAspect? LastResolved { get; set; }

    public ThemeProvider? LastThemeProvider { get; set; }

    public AspectRuleEvaluationSnapshot[] RuleEvaluations { get; set; } = [];

    public AspectEnvironment? DiagnosticsEnvironment { get; set; }

    public AspectEngineCounters DiagnosticsCounters { get; set; } = new();

    public AspectDiagnostics.Snapshot? Diagnostics { get; set; }
}

internal readonly record struct AspectRuleEvaluationSnapshot(
    AspectRuleSet Rule,
    IReadOnlyList<AspectConditionResult> Conditions,
    string Outcome);
