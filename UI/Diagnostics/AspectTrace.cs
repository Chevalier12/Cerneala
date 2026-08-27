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
            AspectRuleSet rule = value.SourceRule;
            lines.Add(
                $"winner: package={rule.PackageName} document={rule.Origin.Document ?? "-"} " +
                $"origin={rule.Origin.Kind} origin-name={rule.Origin.Name ?? "-"} scope={rule.Scope} " +
                $"rule={rule.Name} layer={rule.Layer} source-order={rule.SourceOrder} " +
                $"specificity={rule.Target.Specificity} declaration-order={rule.DeclarationOrder} " +
                $"declaration={value.SourceDeclaration.DiagnosticName ?? value.Property.Name} value={value.Value}");
        }

        foreach (AspectResolutionStep step in diagnostics.ResolutionSteps)
        {
            string conditions = string.Join(",", step.Conditions.Select(FormatCondition));
            string dependencies = string.Join(",", step.Dependencies.Select(FormatDependency));
            lines.Add(
                $"rule: package={step.PackageName} document={step.Origin.Document ?? "-"} " +
                $"origin={step.Origin.Kind} origin-name={step.Origin.Name ?? "-"} scope={step.Scope} " +
                $"rule={step.RuleName} target={step.Target} layer={step.Layer} source-order={step.SourceOrder} " +
                $"specificity={step.Specificity} declaration-order={step.DeclarationOrder} " +
                $"conditions=[{conditions}] dependencies=[{dependencies}] outcome={step.Outcome}");
        }

        foreach (RejectedAspectDeclaration rejected in diagnostics.ResolvedAspect.RejectedDeclarations)
        {
            lines.Add(
                $"rejected: package={rejected.RejectedRule.PackageName} rule={rejected.RejectedRule.Name} " +
                $"declaration={rejected.Rejected.DiagnosticName ?? rejected.Rejected.Property.Name} " +
                $"winner-package={rejected.WinningRule.PackageName} winner-rule={rejected.WinningRule.Name} " +
                $"because {rejected.Reason}");
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

    private static string FormatCondition(AspectConditionTrace condition)
    {
        string children = condition.Children.Count == 0
            ? string.Empty
            : $" children=({string.Join(";", condition.Children.Select(FormatCondition))})";
        return $"{condition.DiagnosticText}:{condition.Matches}{children}";
    }

    private static string FormatDependency(AspectConditionDependency dependency)
    {
        string name = dependency.DiagnosticName ??
            dependency.State?.Name ??
            dependency.Variant?.Name ??
            dependency.Property?.DiagnosticName ??
            dependency.Data?.ToString() ??
            dependency.Token?.Name ??
            "-";
        return $"{dependency.Kind}:{name}";
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
