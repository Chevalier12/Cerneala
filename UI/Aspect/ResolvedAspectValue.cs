using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class ResolvedAspectValue
{
    internal ResolvedAspectValue(UiProperty property, object? value, AspectDeclaration sourceDeclaration, AspectCascadeKey cascadeKey, AspectMotion? motion)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value;
        SourceDeclaration = sourceDeclaration ?? throw new ArgumentNullException(nameof(sourceDeclaration));
        CascadeKey = cascadeKey;
        Motion = motion;
    }

    public UiProperty Property { get; }

    public object? Value { get; }

    public AspectDeclaration SourceDeclaration { get; }

    internal AspectCascadeKey CascadeKey { get; }

    public AspectMotion? Motion { get; }
}
