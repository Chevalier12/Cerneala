using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectTarget
{
    public AspectTarget(Type elementType, AspectSlot? slot = null, IReadOnlyList<AspectCondition>? conditions = null)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        if (!typeof(UIElement).IsAssignableFrom(elementType))
        {
            throw new ArgumentException("Aspect target type must derive from UIElement.", nameof(elementType));
        }

        ElementType = elementType;
        Slot = slot;
        Conditions = conditions ?? [];
        Specificity = new AspectSpecificity(
            Component: elementType == typeof(UIElement) ? 0 : 1,
            Slot: slot is null ? 0 : 1) +
            Conditions.Aggregate(new AspectSpecificity(), (current, condition) => current + condition.Specificity);
    }

    public Type ElementType { get; }

    public AspectSlot? Slot { get; }

    public IReadOnlyList<AspectCondition> Conditions { get; }

    public AspectSpecificity Specificity { get; }

    public bool Matches(AspectMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ElementType.IsInstanceOfType(context.Element))
        {
            return false;
        }

        if (Slot is not null && !Equals(Slot, context.SlotPath?.Slot))
        {
            return false;
        }

        return Conditions.All(condition => condition.Evaluate(context).Matches);
    }

    public override string ToString()
    {
        return Slot is null ? ElementType.Name : $"{ElementType.Name}@{Slot.Name}";
    }
}
