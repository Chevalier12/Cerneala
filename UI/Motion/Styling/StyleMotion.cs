using Cerneala.UI.Core;

namespace Cerneala.UI.Motion.Styling;

public abstract class StyleMotion
{
    private protected StyleMotion(UiProperty property, string tokenName, StyleMotionScope scope)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        TokenName = string.IsNullOrWhiteSpace(tokenName)
            ? throw new ArgumentException("Style motion token name cannot be empty.", nameof(tokenName))
            : tokenName;
        Scope = scope;
    }

    public UiProperty Property { get; }

    public string TokenName { get; }

    public StyleMotionScope Scope { get; }

    public bool AppliesTo(UiPropertyValueSource source)
    {
        return Scope == StyleMotionScope.Both ||
            (Scope == StyleMotionScope.BaseChanges && source == UiPropertyValueSource.StyleBase) ||
            (Scope == StyleMotionScope.VisualStateChanges && source == UiPropertyValueSource.StyleVisualState);
    }
}

public sealed class StyleMotion<T> : StyleMotion
{
    public StyleMotion(UiProperty<T> property, string tokenName, StyleMotionScope scope = StyleMotionScope.Both)
        : base(property, tokenName, scope)
    {
        Property = property;
    }

    public new UiProperty<T> Property { get; }
}

public enum StyleMotionScope
{
    BaseChanges,
    VisualStateChanges,
    Both
}
