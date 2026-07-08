namespace Cerneala.UI.Aspect;

public sealed class AspectTokenDefinition
{
    public AspectTokenDefinition(AspectToken token, AspectValue defaultValue)
    {
        Token = token ?? throw new ArgumentNullException(nameof(token));
        DefaultValue = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
        if (token.ValueType != defaultValue.ValueType)
        {
            throw new ArgumentException("Token default value type must match token value type.", nameof(defaultValue));
        }
    }

    public AspectToken Token { get; }

    public AspectValue DefaultValue { get; }
}
