using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism.Execution;

internal sealed class PrismGraphFallbackTracker
{
    private readonly PrismExecutionDiagnostics diagnostics;
    private bool[] bypassedScopes = [];

    public PrismGraphFallbackTracker(
        PrismExecutionDiagnostics diagnostics) =>
        this.diagnostics = diagnostics;

    public void Prepare(int requiredScopeSlots)
    {
        if (bypassedScopes.Length < requiredScopeSlots)
        {
            Array.Resize(ref bypassedScopes, requiredScopeSlots);
        }
        Array.Clear(bypassedScopes);
    }

    public PrismFallbackAction Record(
        PrismGraphNode node,
        PrismFallbackReason reason,
        string detail)
    {
        PrismFallbackAction action = diagnostics.Record(
            node.Id,
            node.AnalysisScopeIndex,
            reason,
            detail);
        if (action == PrismFallbackAction.BypassComposition &&
            (uint)node.AnalysisScopeIndex <
                (uint)bypassedScopes.Length)
        {
            bypassedScopes[node.AnalysisScopeIndex] = true;
        }
        return action;
    }

    public bool IsScopeBypassed(int scopeIndex) =>
        (uint)scopeIndex < (uint)bypassedScopes.Length &&
        bypassedScopes[scopeIndex];
}
