namespace Cerneala.UI.Aspect;

internal sealed class AspectEngineElementState
{
    public ResolvedAspect? LastResolved { get; set; }

    public AspectDiagnostics.Snapshot Diagnostics { get; set; } = new();
}
