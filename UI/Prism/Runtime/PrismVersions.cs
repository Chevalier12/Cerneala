namespace Cerneala.UI.Prism.Runtime;

public readonly record struct PrismStructuralVersion(long Value)
{
    internal PrismStructuralVersion Next() => new(checked(Value + 1));
}

public readonly record struct PrismValueVersion(long Value)
{
    internal PrismValueVersion Next() => new(checked(Value + 1));
}
