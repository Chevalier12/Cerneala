using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cerneala.UI.Motion;

internal static class ObjectMotionExpressionPropertyCache<TTarget, TValue>
    where TTarget : class
{
    private static readonly ConcurrentDictionary<
        object,
        Lazy<MotionProperty<TTarget, TValue>>> properties = new();

    public static MotionProperty<TTarget, TValue> Get(
        Expression<Func<TTarget, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        MemberExpression member = GetDirectMember(expression);
        return properties.GetOrAdd(
            member.Member,
            _ => new Lazy<MotionProperty<TTarget, TValue>>(
                () => Create(expression, member),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private static MemberExpression GetDirectMember(
        Expression<Func<TTarget, TValue>> expression)
    {
        if (expression.Body is not MemberExpression member ||
            !ReferenceEquals(member.Expression, expression.Parameters[0]))
        {
            throw new ArgumentException(
                "The expression must directly select a writable instance member, such as 'target => target.Position'.",
                nameof(expression));
        }

        return member;
    }

    private static MotionProperty<TTarget, TValue> Create(
        Expression<Func<TTarget, TValue>> expression,
        MemberExpression member)
    {
        ParameterExpression value = Expression.Parameter(typeof(TValue), "value");
        Action<TTarget, TValue> setter;
        try
        {
            setter = Expression
                .Lambda<Action<TTarget, TValue>>(
                    Expression.Assign(member, value),
                    expression.Parameters[0],
                    value)
                .Compile();
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                $"Member '{member.Member.Name}' must be writable so Motion can update it.",
                nameof(expression),
                exception);
        }

        return MotionProperty.Create(
            member.Member.Name,
            expression.Compile(),
            setter);
    }
}
