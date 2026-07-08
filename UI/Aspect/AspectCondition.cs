using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class AspectCondition
{
    internal AspectCondition(AspectConditionNode node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    internal AspectConditionNode Node { get; }

    public AspectSpecificity Specificity => Node.Specificity;

    public static AspectCondition State(AspectState state)
    {
        return new AspectCondition(new StateAspectCondition(state));
    }

    public static AspectCondition Variant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)
    {
        return new AspectCondition(new VariantAspectCondition(key, value));
    }

    public static AspectPropertyConditionBuilder<TValue> Property<TValue>(UiProperty<TValue> property)
    {
        return new AspectPropertyConditionBuilder<TValue>(property);
    }

    public static AspectCondition Data<TData>(
        string diagnosticName,
        Func<TData, bool> predicate,
        params AspectDataDependency[] dependencies)
    {
        return new AspectCondition(new DataAspectCondition<TData>(diagnosticName, predicate, dependencies));
    }

    public static AspectCondition Data<TData, TValue>(
        string diagnosticName,
        Func<TData, TValue> selector,
        Func<TValue, bool> predicate,
        params AspectDataDependency[] dependencies)
    {
        return new AspectCondition(new DataAspectCondition<TData, TValue>(diagnosticName, selector, predicate, dependencies));
    }

    public static AspectCondition All(params AspectCondition[] conditions)
    {
        return new AspectCondition(new AllAspectCondition(RequireConditions(conditions)));
    }

    public static AspectCondition Any(params AspectCondition[] conditions)
    {
        return new AspectCondition(new AnyAspectCondition(RequireConditions(conditions)));
    }

    public static AspectCondition Not(AspectCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new AspectCondition(new NotAspectCondition(condition.Node));
    }

    public static AspectCondition Predicate(string diagnosticName, Func<AspectMatchContext, bool> predicate)
    {
        return new AspectCondition(new PredicateAspectCondition(diagnosticName, predicate));
    }

    public AspectConditionResult Evaluate(AspectMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Node.Evaluate(context);
    }

    private static AspectConditionNode[] RequireConditions(AspectCondition[] conditions)
    {
        if (conditions is null || conditions.Length == 0)
        {
            throw new ArgumentException("Compound aspect conditions require at least one child.", nameof(conditions));
        }

        return conditions.Select(condition => condition?.Node ?? throw new ArgumentException("Condition cannot be null.", nameof(conditions))).ToArray();
    }
}

public sealed class AspectPropertyConditionBuilder<TValue>
{
    private readonly UiProperty<TValue> property;

    internal AspectPropertyConditionBuilder(UiProperty<TValue> property)
    {
        this.property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public AspectCondition Is(TValue value)
    {
        return new AspectCondition(new PropertyAspectCondition<TValue>(
            property,
            candidate => property.Metadata.EqualityComparer.Equals(candidate, value),
            $"{property.DiagnosticName} == {value}"));
    }

    public AspectCondition Matches(Func<TValue, bool> predicate, string diagnosticName)
    {
        return new AspectCondition(new PropertyAspectCondition<TValue>(property, predicate, diagnosticName));
    }
}
