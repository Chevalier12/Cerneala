namespace Cerneala.UI.Aspect;

public abstract class AspectValue
{
    public abstract Type ValueType { get; }

    public abstract IReadOnlyList<AspectToken> Dependencies { get; }

    public abstract object? Resolve(AspectResolutionContext context);

    // A literal resolves to the same value in every context; a token or computed value has no value of its own.
    internal virtual bool TryGetLiteral(out object? value)
    {
        value = null;
        return false;
    }
}
