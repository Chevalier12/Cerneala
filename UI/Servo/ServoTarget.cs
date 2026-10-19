using System.Text.RegularExpressions;
using Cerneala.UI.Accessibility;

namespace Cerneala.UI.Servo;

public sealed class ServoTarget
{
    private readonly NameCriterion? nameCriterion;
    private readonly Func<ServoElement, bool>[] predicates;

    private ServoTarget(
        string? id,
        NameCriterion? nameCriterion,
        SemanticsRole? role,
        ServoTarget? scope,
        Func<ServoElement, bool>[] predicates,
        Index? position)
    {
        Id = id;
        this.nameCriterion = nameCriterion;
        Role = role;
        Scope = scope;
        this.predicates = predicates;
        Position = position;
    }

    internal string? Id { get; }

    internal SemanticsRole? Role { get; }

    internal ServoTarget? Scope { get; }

    internal Index? Position { get; }

    internal bool HasName => nameCriterion is not null;

    internal bool HasPredicates => predicates.Length > 0;

    internal int PredicateCount => predicates.Length;

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
        return WithNameCriterion(ExactName(name));
    }

    public ServoTarget WithNameContaining(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return WithNameCriterion(new NameCriterion(
            candidate => candidate.Contains(value, StringComparison.Ordinal),
            $"Name contains {Quote(value)}"));
    }

    public ServoTarget WithNameMatching(Regex pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        string options = pattern.Options == RegexOptions.None ? string.Empty : $" ({pattern.Options})";
        return WithNameCriterion(new NameCriterion(pattern.IsMatch, $"Name matches /{pattern}/{options}"));
    }

    public ServoTarget Where(Func<ServoElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new ServoTarget(Id, nameCriterion, Role, Scope, [.. predicates, predicate], Position);
    }

    public ServoTarget Within(ServoTarget scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ServoTarget(Id, nameCriterion, Role, scope, predicates, Position);
    }

    public ServoTarget Nth(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return WithPosition(new Index(index));
    }

    public ServoTarget First() => WithPosition(Index.Start);

    public ServoTarget Last() => WithPosition(Index.FromEnd(1));

    // Example: ByRole(Button).WithName("Save").Within(ById("toolbar")) gives
    // Role=Button, Name="Save" within (Id="toolbar"), and ByRole(Button).Within(ById("row").Last()).Nth(2)
    // gives Role=Button within (Id="row", Last()), Nth(2).
    public override string ToString()
    {
        string description = string.Join(", ", DescribeOwnCriteria());
        if (Scope is not null)
        {
            description = $"{description} {DescribeScope()}";
        }

        return Position is Index position ? $"{description}, {DescribePosition(position)}" : description;
    }

    // The own criteria in display order: Id, Role, Name, then one entry per Where predicate.
    internal IEnumerable<string> DescribeOwnCriteria()
    {
        if (Id is not null)
        {
            yield return $"Id={Quote(Id)}";
        }

        if (Role is SemanticsRole role)
        {
            yield return $"Role={role}";
        }

        if (nameCriterion is not null)
        {
            yield return nameCriterion.Description;
        }

        foreach (Func<ServoElement, bool> _ in predicates)
        {
            yield return "where <predicate>";
        }
    }

    internal string DescribeScope() => $"within ({Scope})";

    internal static string DescribePosition(Index position) =>
        position.Equals(Index.Start) ? "First()"
        : position.Equals(Index.FromEnd(1)) ? "Last()"
        : $"Nth({position.Value})";

    internal bool MatchesName(string? name)
    {
        return nameCriterion is null || (name is not null && nameCriterion.Matches(name));
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

    internal bool MatchesPredicate(int index, ServoElement element) => predicates[index](element);

    private static NameCriterion ExactName(string name) =>
        new(candidate => string.Equals(candidate, name, StringComparison.Ordinal), $"Name={Quote(name)}");

    private static string Quote(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private ServoTarget WithNameCriterion(NameCriterion criterion) =>
        new(Id, criterion, Role, Scope, predicates, Position);

    private ServoTarget WithPosition(Index position) =>
        new(Id, nameCriterion, Role, Scope, predicates, position);

    // Description is the readable form used by ToString, for example Name contains "Item".
    private sealed record NameCriterion(Func<string, bool> Matches, string Description);
}
