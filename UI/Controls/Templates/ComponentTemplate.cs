using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public abstract class ComponentTemplate
{
    private protected ComponentTemplate(Type ownerType, string name)
    {
        if (!typeof(Control).IsAssignableFrom(ownerType))
        {
            throw new ArgumentException("Component template owner type must derive from Control.", nameof(ownerType));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Component template name cannot be empty.", nameof(name));
        }

        OwnerType = ownerType;
        Name = name;
    }

    public Type OwnerType { get; }

    public string Name { get; }

    public ComponentTemplateInstance CreateInstance(Control owner, ComponentTemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(context);
        if (!OwnerType.IsInstanceOfType(owner))
        {
            throw new InvalidOperationException($"Template for '{OwnerType.FullName}' cannot be applied to '{owner.GetType().FullName}'.");
        }

        return CreateInstanceCore(owner, context);
    }

    private protected abstract ComponentTemplateInstance CreateInstanceCore(Control owner, ComponentTemplateContext context);
}

public sealed class ComponentTemplate<TControl> : ComponentTemplate
    where TControl : Control
{
    private readonly Func<ComponentTemplateContext<TControl>, UIElement?> factory;

    public ComponentTemplate(string name, Func<ComponentTemplateContext<TControl>, UIElement?> factory)
        : base(typeof(TControl), name)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    private protected override ComponentTemplateInstance CreateInstanceCore(Control owner, ComponentTemplateContext context)
    {
        ComponentTemplateContext<TControl> typed = context is ComponentTemplateContext<TControl> typedContext
            ? typedContext
            : new ComponentTemplateContext<TControl>((TControl)owner, context.Environment, context.States, context.Variants);
        UIElement? root = factory(typed);
        return new ComponentTemplateInstance(root, typed.Bindings, typed.TokenBindings, typed.Slots, typed.Parts);
    }
}
