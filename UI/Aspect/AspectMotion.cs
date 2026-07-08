using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class AspectMotion
{
    public AspectMotion(UiProperty property, string tokenName, AspectMotionSource source = AspectMotionSource.All)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (string.IsNullOrWhiteSpace(tokenName))
        {
            throw new ArgumentException("Aspect motion token name cannot be empty.", nameof(tokenName));
        }

        TokenName = tokenName;
        Source = source;
    }

    public UiProperty Property { get; }

    public string TokenName { get; }

    public AspectMotionSource Source { get; }
}

[Flags]
public enum AspectMotionSource
{
    None = 0,
    Base = 1 << 0,
    State = 1 << 1,
    Variant = 1 << 2,
    Data = 1 << 3,
    All = Base | State | Variant | Data
}
