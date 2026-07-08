using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

internal abstract class AspectConditionNode
{
    public abstract AspectSpecificity Specificity { get; }

    public abstract AspectConditionResult Evaluate(AspectMatchContext context);
}

internal sealed class StateAspectCondition : AspectConditionNode
{
    public StateAspectCondition(AspectState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public AspectState State { get; }

    public override AspectSpecificity Specificity => new(State: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        bool matches = context.States.Contains(State);
        return new AspectConditionResult(
            matches,
            [new AspectConditionDependency(AspectConditionDependencyKind.State, State: State)],
            matches ? $"state {State.Name} matched" : $"state {State.Name} missing");
    }
}

internal sealed class VariantAspectCondition : AspectConditionNode
{
    private readonly AspectVariantKey key;
    private readonly object? expectedValue;

    public VariantAspectCondition(AspectVariantKey key, object? expectedValue)
    {
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.expectedValue = expectedValue;
    }

    public override AspectSpecificity Specificity => new(Variant: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        bool hasValue = context.Variants.TryGetUntyped(key, out object? actual);
        bool matches = hasValue && Equals(actual, expectedValue);
        return new AspectConditionResult(
            matches,
            [new AspectConditionDependency(AspectConditionDependencyKind.Variant, Variant: key)],
            matches ? $"variant {key.Name} matched" : $"variant {key.Name} did not match");
    }
}

internal sealed class PropertyAspectCondition<TValue> : AspectConditionNode
{
    private readonly UiProperty<TValue> property;
    private readonly Func<TValue, bool> predicate;
    private readonly string diagnosticName;

    public PropertyAspectCondition(UiProperty<TValue> property, Func<TValue, bool> predicate, string diagnosticName)
    {
        this.property = property ?? throw new ArgumentNullException(nameof(property));
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        if (string.IsNullOrWhiteSpace(diagnosticName))
        {
            throw new ArgumentException("Property condition diagnostic name cannot be empty.", nameof(diagnosticName));
        }

        this.diagnosticName = diagnosticName;
    }

    public override AspectSpecificity Specificity => new(Property: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        TValue value = context.Element.GetValue(property);
        bool matches = predicate(value);
        return new AspectConditionResult(
            matches,
            [new AspectConditionDependency(AspectConditionDependencyKind.UiProperty, Property: property)],
            matches ? $"{diagnosticName} matched" : $"{diagnosticName} did not match");
    }
}

internal sealed class DataAspectCondition<TData> : AspectConditionNode
{
    private readonly string diagnosticName;
    private readonly Func<TData, bool> predicate;
    private readonly IReadOnlyList<AspectDataDependency> dataDependencies;

    public DataAspectCondition(string diagnosticName, Func<TData, bool> predicate, IReadOnlyList<AspectDataDependency> dependencies)
    {
        if (string.IsNullOrWhiteSpace(diagnosticName))
        {
            throw new ArgumentException("Data condition diagnostic name cannot be empty.", nameof(diagnosticName));
        }

        if (dependencies is null || dependencies.Count == 0)
        {
            throw new ArgumentException("Data conditions must declare at least one dependency.", nameof(dependencies));
        }

        this.diagnosticName = diagnosticName;
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        dataDependencies = dependencies.ToArray();
    }

    public override AspectSpecificity Specificity => new(Data: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        bool matches = context.Data is TData typed && predicate(typed);
        return new AspectConditionResult(
            matches,
            dataDependencies.Select(dependency => new AspectConditionDependency(AspectConditionDependencyKind.DataContext, Data: dependency)).ToArray(),
            matches ? $"{diagnosticName} matched" : $"{diagnosticName} did not match");
    }
}

internal sealed class DataAspectCondition<TData, TValue> : AspectConditionNode
{
    private readonly string diagnosticName;
    private readonly Func<TData, TValue> selector;
    private readonly Func<TValue, bool> predicate;
    private readonly IReadOnlyList<AspectDataDependency> dataDependencies;

    public DataAspectCondition(string diagnosticName, Func<TData, TValue> selector, Func<TValue, bool> predicate, IReadOnlyList<AspectDataDependency> dependencies)
    {
        if (string.IsNullOrWhiteSpace(diagnosticName))
        {
            throw new ArgumentException("Data condition diagnostic name cannot be empty.", nameof(diagnosticName));
        }

        if (dependencies is null || dependencies.Count == 0)
        {
            throw new ArgumentException("Data conditions must declare at least one dependency.", nameof(dependencies));
        }

        this.diagnosticName = diagnosticName;
        this.selector = selector ?? throw new ArgumentNullException(nameof(selector));
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        dataDependencies = dependencies.ToArray();
    }

    public override AspectSpecificity Specificity => new(Data: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        bool matches = context.Data is TData typed && predicate(selector(typed));
        return new AspectConditionResult(
            matches,
            dataDependencies.Select(dependency => new AspectConditionDependency(AspectConditionDependencyKind.DataContext, Data: dependency)).ToArray(),
            matches ? $"{diagnosticName} matched" : $"{diagnosticName} did not match");
    }
}

internal sealed class AllAspectCondition : AspectConditionNode
{
    private readonly IReadOnlyList<AspectConditionNode> children;

    public AllAspectCondition(IReadOnlyList<AspectConditionNode> children)
    {
        this.children = children ?? throw new ArgumentNullException(nameof(children));
    }

    public override AspectSpecificity Specificity => children.Aggregate(new AspectSpecificity(Compound: 1), (current, child) => current + child.Specificity);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        AspectConditionResult[] results = children.Select(child => child.Evaluate(context)).ToArray();
        return new AspectConditionResult(
            results.All(result => result.Matches),
            results.SelectMany(result => result.Dependencies).ToArray(),
            "all",
            results);
    }
}

internal sealed class AnyAspectCondition : AspectConditionNode
{
    private readonly IReadOnlyList<AspectConditionNode> children;

    public AnyAspectCondition(IReadOnlyList<AspectConditionNode> children)
    {
        this.children = children ?? throw new ArgumentNullException(nameof(children));
    }

    public override AspectSpecificity Specificity => children.Aggregate(new AspectSpecificity(Compound: 1), (current, child) => current + child.Specificity);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        AspectConditionResult[] results = children.Select(child => child.Evaluate(context)).ToArray();
        return new AspectConditionResult(
            results.Any(result => result.Matches),
            results.SelectMany(result => result.Dependencies).ToArray(),
            "any",
            results);
    }
}

internal sealed class NotAspectCondition : AspectConditionNode
{
    private readonly AspectConditionNode child;

    public NotAspectCondition(AspectConditionNode child)
    {
        this.child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public override AspectSpecificity Specificity => new AspectSpecificity(Compound: 1) + child.Specificity;

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        AspectConditionResult result = child.Evaluate(context);
        return new AspectConditionResult(
            !result.Matches,
            result.Dependencies,
            "not",
            [result]);
    }
}

internal sealed class PredicateAspectCondition : AspectConditionNode
{
    private readonly string diagnosticName;
    private readonly Func<AspectMatchContext, bool> predicate;

    public PredicateAspectCondition(string diagnosticName, Func<AspectMatchContext, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(diagnosticName))
        {
            throw new ArgumentException("Predicate condition diagnostic name cannot be empty.", nameof(diagnosticName));
        }

        this.diagnosticName = diagnosticName;
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public override AspectSpecificity Specificity => new(Predicate: 1);

    public override AspectConditionResult Evaluate(AspectMatchContext context)
    {
        bool matches = predicate(context);
        return new AspectConditionResult(
            matches,
            [new AspectConditionDependency(AspectConditionDependencyKind.Predicate, DiagnosticName: diagnosticName)],
            matches ? $"{diagnosticName} matched" : $"{diagnosticName} did not match");
    }
}
