using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public enum AspectConditionDependencyKind
{
    State,
    Variant,
    UiProperty,
    DataContext,
    Token,
    Predicate
}

public sealed record AspectConditionDependency(
    AspectConditionDependencyKind Kind,
    AspectState? State = null,
    AspectVariantKey? Variant = null,
    UiProperty? Property = null,
    AspectDataDependency? Data = null,
    AspectToken? Token = null,
    string? DiagnosticName = null);
