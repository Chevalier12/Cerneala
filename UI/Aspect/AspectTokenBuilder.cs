namespace Cerneala.UI.Aspect;

public sealed class AspectTokenBuilder
{
    private readonly List<AspectTokenDefinition> tokens;

    internal AspectTokenBuilder(List<AspectTokenDefinition> tokens)
    {
        this.tokens = tokens;
    }

    public AspectTokenBuilder Set<T>(AspectToken<T> token, T value)
    {
        tokens.Add(new AspectTokenDefinition(token, AspectValue<T>.Literal(value)));
        return this;
    }
}
