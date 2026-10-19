using System.Text.RegularExpressions;
using Cerneala.UI.Accessibility;

namespace Cerneala.UI.Servo;

public sealed class ServoTarget
{
    private readonly Func<string, bool>? nameMatcher;
    private readonly Func<ServoElement, bool>[] predicates;

    private ServoTarget(
        string? id,
        Func<string, bool>? nameMatcher,
        SemanticsRole? role,
        ServoTarget? scope,
        Func<ServoElement, bool>[] predicates,
        Index? position)
    {
        Id = id;
        this.nameMatcher = nameMatcher;
        Role = role;
        Scope = scope;
        this.predicates = predicates;
        Position = position;
    }

    internal string? Id { get; }

    internal SemanticsRole? Role { get; }

    internal ServoTarget? Scope { get; }

    internal Index? Position { get; }

    internal bool HasPredicates => predicates.Length > 0;

    public static ServoTarget ById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return new ServoTarget(id, null, null, null, [], null);
    }

    public static ServoTarget ByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ServoTarget(null, ExactName(name), null, null, [], null);
    }

    public static ServoTarget ByRole(SemanticsRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        return new ServoTarget(null, null, role, null, [], null);
    }

    public ServoTarget WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return WithNameMatcher(ExactName(name));
    }

    public ServoTarget WithNameContaining(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return WithNameMatcher(candidate => candidate.Contains(value, StringComparison.Ordinal));
    }

    public ServoTarget WithNameMatching(Regex pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return WithNameMatcher(pattern.IsMatch);
    }

    public ServoTarget Where(Func<ServoElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new ServoTarget(Id, nameMatcher, Role, Scope, [.. predicates, predicate], Position);
    }

    public ServoTarget Within(ServoTarget scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ServoTarget(Id, nameMatcher, Role, scope, predicates, Position);
    }

    public ServoTarget Nth(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return WithPosition(new Index(index));
    }

    public ServoTarget First() => WithPosition(Index.Start);

    public ServoTarget Last() => WithPosition(Index.FromEnd(1));

    internal bool MatchesName(string? name)
    {
        return nameMatcher is null || (name is not null && nameMatcher(name));
    }

    internal bool MatchesPredicates(ServoElement element)
    {
        foreach (Func<ServoElement, bool> predicate in predicates)
        {
            if (!predicate(element))
            {
                return false;
            }
        }

        return true;
    }

    private static Func<string, bool> ExactName(string name) =>
        candidate => string.Equals(candidate, name, StringComparison.Ordinal);

    private ServoTarget WithNameMatcher(Func<string, bool> matcher) =>
        new(Id, matcher, Role, Scope, predicates, Position);

    private ServoTarget WithPosition(Index position) =>
        new(Id, nameMatcher, Role, Scope, predicates, position);
}
