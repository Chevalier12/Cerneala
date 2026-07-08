using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Diagnostics;

public static class AspectTrace
{
    public static AspectTraceSnapshot Capture(UIElement element, UiProperty property, AspectDiagnostics.Snapshot? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);

        List<string> lines = [$"Aspect trace for {property.DiagnosticName}"];
        if (diagnostics?.ResolvedAspect is null)
        {
            lines.Add("No aspect diagnostics.");
            return new AspectTraceSnapshot(lines);
        }

        if (diagnostics.ResolvedAspect.Values.TryGetValue(property, out ResolvedAspectValue? value))
        {
            lines.Add($"winner: {value.SourceDeclaration.DiagnosticName ?? value.Property.Name} value={value.Value}");
        }

        foreach (AspectResolutionStep step in diagnostics.ResolutionSteps)
        {
            lines.Add($"{step.PackageName} {step.RuleName} {step.Target} {step.Layer} {step.Specificity} order={step.DeclarationOrder} {step.Outcome}");
        }

        foreach (RejectedAspectDeclaration rejected in diagnostics.ResolvedAspect.RejectedDeclarations)
        {
            lines.Add($"rejected: {rejected.Rejected.DiagnosticName ?? rejected.Rejected.Property.Name} because {rejected.Reason}");
        }

        foreach (AspectTokenTrace token in diagnostics.TokenTraces)
        {
            lines.Add($"token: {token.Token.Name} provider={token.ProviderName} raw={token.RawValue} resolved={token.ResolvedValue}");
        }

        if (diagnostics.ResolvedAspect.Dependencies.Slot is not null)
        {
            lines.Add($"slot: {diagnostics.ResolvedAspect.Dependencies.Slot}");
        }

        foreach (AspectVariantKey variant in diagnostics.ResolvedAspect.Dependencies.Variants)
        {
            lines.Add($"variant: {variant.Name}");
        }

        return new AspectTraceSnapshot(lines);
    }
}

public sealed class AspectTraceSnapshot
{
    public AspectTraceSnapshot(IReadOnlyList<string> lines)
    {
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
    }

    public IReadOnlyList<string> Lines { get; }
}
