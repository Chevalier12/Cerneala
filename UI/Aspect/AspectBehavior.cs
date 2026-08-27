using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectBehavior
{
    private readonly Func<UIElement, IDisposable?> attach;

    public AspectBehavior(Type targetType, Func<UIElement, IDisposable?> attach)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        if (!typeof(UIElement).IsAssignableFrom(TargetType))
        {
            throw new ArgumentException(
                $"Aspect behavior target type '{TargetType.FullName}' must derive from UIElement.",
                nameof(targetType));
        }

        this.attach = attach ?? throw new ArgumentNullException(nameof(attach));
    }

    public Type TargetType { get; }

    internal bool Matches(UIElement element)
    {
        return TargetType.IsInstanceOfType(element);
    }

    internal IDisposable? Attach(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!Matches(element))
        {
            throw new ArgumentException(
                $"Aspect behavior for '{TargetType.FullName}' cannot attach to '{element.GetType().FullName}'.",
                nameof(element));
        }

        return attach(element);
    }
}
