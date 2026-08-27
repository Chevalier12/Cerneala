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
        Conditions = Array.AsReadOnly((conditions ?? []).Select(
            condition => condition ?? throw new ArgumentException("Aspect target conditions cannot contain null.", nameof(conditions))).ToArray());
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
        return MatchesStructure(context) && Conditions.All(condition => condition.Evaluate(context).Matches);
    }

    internal bool MatchesStructure(AspectMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GetStructureMismatch(context) is null;
    }

    internal string? GetStructureMismatch(AspectMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ElementType.IsInstanceOfType(context.Element))
        {
            return "target type mismatch";
        }

        if (Slot is null)
        {
            return null;
        }

        if (!Equals(Slot, context.SlotPath?.Slot) ||
            context.OwnerComponent is null ||
            !Slot.OwnerType.IsInstanceOfType(context.OwnerComponent) ||
            !Slot.TargetType.IsInstanceOfType(context.Element))
        {
            return "slot mismatch";
        }

        return null;
    }

    public override string ToString()
    {
        return Slot is null ? ElementType.Name : $"{ElementType.Name}@{Slot.Name}";
    }
}
