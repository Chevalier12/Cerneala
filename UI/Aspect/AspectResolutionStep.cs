namespace Cerneala.UI.Aspect;

public sealed record AspectResolutionStep(
    string PackageName,
    string RuleName,
    string Target,
    AspectLayer Layer,
    AspectSpecificity Specificity,
    int DeclarationOrder,
    string Outcome);
