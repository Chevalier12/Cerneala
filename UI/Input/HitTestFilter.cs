using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class HitTestFilter
{
    public static HitTestFilter IncludeAll { get; } = new();

    private readonly Func<UIElement, HitTestFilterBehavior>? predicate;

    public HitTestFilter(Func<UIElement, HitTestFilterBehavior>? predicate = null)
    {
        this.predicate = predicate;
    }

    public HitTestFilterBehavior Evaluate(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return predicate?.Invoke(element) ?? HitTestFilterBehavior.Include;
    }
}

public enum HitTestFilterBehavior
{
    Include,
    Exclude,
    ExcludeSubtree
}
