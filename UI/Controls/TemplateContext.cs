using System.Collections.ObjectModel;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public class TemplateContext
{
    private readonly List<TemplateBinding> bindings = [];

    public TemplateContext(Control owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public Control Owner { get; }

    public IReadOnlyList<TemplateBinding> Bindings => new ReadOnlyCollection<TemplateBinding>(bindings);

    public TemplateBinding<T> Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty)
    {
        TemplateBinding<T> binding = new(sourceProperty, target, targetProperty);
        bindings.Add(binding);
        return binding;
    }

    public TemplateBinding Bind(UiProperty sourceProperty, UIElement target, UiProperty targetProperty)
    {
        TemplateBinding binding = TemplateBinding.Create(sourceProperty, target, targetProperty);
        bindings.Add(binding);
        return binding;
    }
}

public sealed class TemplateContext<TControl> : TemplateContext
    where TControl : Control
{
    public TemplateContext(TControl owner)
        : base(owner)
    {
        Owner = owner;
    }

    public new TControl Owner { get; }
}
