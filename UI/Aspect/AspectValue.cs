namespace Cerneala.UI.Aspect;

public abstract class AspectValue
{
    public abstract Type ValueType { get; }

    public abstract IReadOnlyList<AspectToken> Dependencies { get; }

    public abstract object? Resolve(AspectResolutionContext context);
}
