namespace Cerneala.UI.Aspect;

public sealed class AspectRuleSetBuilder
{
    private readonly List<AspectDeclaration> declarations = [];

    public AspectRuleSetBuilder(string name, AspectLayer layer, AspectTarget target, int declarationOrder)
    {
        Name = name;
        Layer = layer;
        Target = target;
        DeclarationOrder = declarationOrder;
    }

    public string Name { get; }

    public AspectLayer Layer { get; }

    public AspectTarget Target { get; }

    public int DeclarationOrder { get; }

    public AspectRuleSetBuilder Set<T>(Cerneala.UI.Core.UiProperty<T> property, AspectValue<T> value, string? diagnosticName = null)
    {
        declarations.Add(new AspectDeclaration(property, value, diagnosticName: diagnosticName));
        return this;
    }

    public AspectRuleSet Build()
    {
        return new AspectRuleSet(Name, Layer, Target, declarations.ToArray(), DeclarationOrder);
    }
}
