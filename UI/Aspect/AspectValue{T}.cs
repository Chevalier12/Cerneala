namespace Cerneala.UI.Aspect;

public sealed class AspectValue<T> : AspectValue
{
    private static readonly IReadOnlyList<AspectToken> NoDependencies = [];

    private readonly T? literal;
    private readonly AspectToken<T>? token;
    private readonly Func<AspectResolutionContext, T>? compute;
    private readonly IReadOnlyList<AspectToken> dependencies;

    private AspectValue(T? literal)
    {
        this.literal = literal;
        dependencies = NoDependencies;
    }

    private AspectValue(AspectToken<T> token)
    {
        this.token = token ?? throw new ArgumentNullException(nameof(token));
        dependencies = [token];
    }

    private AspectValue(Func<AspectResolutionContext, T> compute, IReadOnlyList<AspectToken> dependencies)
    {
        this.compute = compute ?? throw new ArgumentNullException(nameof(compute));
        this.dependencies = dependencies?.ToArray() ?? throw new ArgumentNullException(nameof(dependencies));
    }

    public override Type ValueType => typeof(T);

    public override IReadOnlyList<AspectToken> Dependencies => dependencies;

    public static AspectValue<T> Literal(T value)
    {
        return new AspectValue<T>(value);
    }

    public static AspectValue<T> Token(AspectToken<T> token)
    {
        return new AspectValue<T>(token);
    }

    public static AspectValue<T> Computed(Func<AspectResolutionContext, T> compute, IReadOnlyList<AspectToken> dependencies)
    {
        return new AspectValue<T>(compute, dependencies);
    }

    public override object? Resolve(AspectResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (compute is not null)
        {
            return compute(context);
        }

        if (token is not null)
        {
            if (context.Environment.TryGet(token, out T value))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Aspect token '{token.Name}' with value type '{token.ValueType.FullName}' was not found in environment '{context.Environment.Name}'.");
        }

        return literal;
    }
}
