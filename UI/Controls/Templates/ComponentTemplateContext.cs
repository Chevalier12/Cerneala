using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public class ComponentTemplateContext
{
    private readonly TemplateSlotMap slots = new();
    private readonly TemplatePartMap parts = new();
    private readonly List<TemplateBinding> bindings = [];
    private readonly List<TemplateTokenBinding> tokenBindings = [];

    public ComponentTemplateContext(
        Control owner,
        AspectEnvironment environment,
        AspectStateSet? states = null,
        AspectVariantSet? variants = null)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        States = states ?? AspectStateSet.Empty;
        Variants = variants ?? AspectVariantSet.Empty;
    }

    public Control Owner { get; }

    public AspectEnvironment Environment { get; }

    public AspectStateSet States { get; }

    public AspectVariantSet Variants { get; }

    public IReadOnlyList<TemplateBinding> Bindings => bindings;

    public IReadOnlyList<TemplateTokenBinding> TokenBindings => tokenBindings;

    public TemplateSlotMap Slots => slots;

    public TemplatePartMap Parts => parts;

    public void RegisterSlot(AspectSlot slot, UIElement element)
    {
        slots.Register(slot, element);
    }

    public TElement RequirePart<TElement>(string name, TElement? element)
        where TElement : UIElement
    {
        if (element is null)
        {
            throw new InvalidOperationException($"Required template part '{name}' was not provided.");
        }

        parts.Register(name, element);
        return element;
    }

    public void Bind(
        UiProperty sourceProperty,
        UIElement target,
        UiProperty targetProperty,
        UiPropertyValueSource targetSource)
    {
        bindings.Add(TemplateBinding.Create(sourceProperty, target, targetProperty, targetSource));
    }

    public void Bind<T>(
        UiProperty<T> sourceProperty,
        UIElement target,
        UiProperty<T> targetProperty,
        UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)
    {
        bindings.Add(new TemplateBinding<T>(sourceProperty, target, targetProperty, targetSource));
    }

    public void BindToken<T>(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty)
    {
        tokenBindings.Add(new TemplateTokenBinding<T>(token, target, targetProperty, Environment));
    }
}

public sealed class ComponentTemplateContext<TControl> : ComponentTemplateContext
    where TControl : Control
{
    public ComponentTemplateContext(
        TControl owner,
        AspectEnvironment environment,
        AspectStateSet? states = null,
        AspectVariantSet? variants = null)
        : base(owner, environment, states, variants)
    {
        Owner = owner;
    }

    public new TControl Owner { get; }
}
