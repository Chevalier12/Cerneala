using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public abstract class TemplateTokenBinding
{
    public abstract void Attach();

    public abstract void Detach();
}

public sealed class TemplateTokenBinding<T> : TemplateTokenBinding
{
    private readonly AspectToken<T> token;
    private readonly UIElement target;
    private readonly UiProperty<T> targetProperty;
    private readonly AspectEnvironment environment;

    public TemplateTokenBinding(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty, AspectEnvironment environment)
    {
        this.token = token ?? throw new ArgumentNullException(nameof(token));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.targetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public override void Attach()
    {
        if (environment.TryGet(token, out T value))
        {
            target.SetValue(targetProperty, value, UiPropertyValueSource.TemplateBinding);
        }
    }

    public override void Detach()
    {
        target.ClearValue(targetProperty, UiPropertyValueSource.TemplateBinding);
    }
}
