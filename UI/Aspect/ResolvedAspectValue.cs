using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class ResolvedAspectValue
{
    internal ResolvedAspectValue(
        UiProperty property,
        object? value,
        AspectRuleSet sourceRule,
        AspectDeclaration sourceDeclaration,
        AspectCascadeKey cascadeKey,
        AspectMotion? motion,
        AspectMotionSource motionSource)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value;
        SourceRule = sourceRule ?? throw new ArgumentNullException(nameof(sourceRule));
        SourceDeclaration = sourceDeclaration ?? throw new ArgumentNullException(nameof(sourceDeclaration));
        CascadeKey = cascadeKey;
        Motion = motion;
        MotionSource = motionSource;
    }

    public UiProperty Property { get; }

    public object? Value { get; }

    public AspectRuleSet SourceRule { get; }

    public AspectDeclaration SourceDeclaration { get; }

    internal AspectCascadeKey CascadeKey { get; }

    public AspectMotion? Motion { get; }

    internal AspectMotionSource MotionSource { get; }
}
