namespace Cerneala.UI.Aspect;

using Cerneala.UI.Theming;

internal sealed class AspectEngineElementState
{
    public ResolvedAspect? LastResolved { get; set; }

    public ThemeProvider? LastThemeProvider { get; set; }

    public AspectDiagnostics.Snapshot Diagnostics { get; set; } = new();
}
