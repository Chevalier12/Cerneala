# ServoTarget Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoTarget.cs`

Describes a reusable immutable Servo query.

```csharp
public sealed class ServoTarget
```

## Examples

```csharp
ServoTarget save = ServoTarget.ByRole(SemanticsRole.Button)
    .WithName("Save")
    .Within(ServoTarget.ById("editor-toolbar"));

ServoElement current = await servo.FindAsync(save);
```

A list has three rows, each with Servo ID `row`, and each row holds one button named `Delete`. Without a position the target is ambiguous; a position selects one match.

```csharp
ServoTarget delete = ServoTarget.ByRole(SemanticsRole.Button).WithName("Delete");

await servo.FindAsync(delete);                                    // throws ServoTargetAmbiguousException (3 matches)
await servo.ClickAsync(delete.Nth(2));                            // clicks the Delete button of the third row
await servo.FindAsync(delete.Nth(3));                             // throws ServoTargetNotFoundException (only 3 matches)
await servo.ClickAsync(delete.Within(ServoTarget.ById("row").Last())); // clicks the Delete button of the last row
```

Name refinements and predicates:

```csharp
ServoTarget item = ServoTarget.ByRole(SemanticsRole.Button).WithNameContaining("3 of");      // matches "Item 3 of 10"
ServoTarget pattern = ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex(@"^Item \d+ of \d+$"));
ServoTarget disabled = ServoTarget.ByRole(SemanticsRole.Button).Where(element => !element.IsEnabled);
```

## Remarks

ID and semantic-name comparisons are exact and ordinal. `Within` evaluates semantic ancestry each time the target is resolved; it does not retain a live container. A candidate is inside a scope when one of its semantic ancestors is a match of the scope target, including the scope's own `Within` and position.

The criteria of one target combine as follows:

- **Name slot.** `ByName`, `WithName`, `WithNameContaining`, and `WithNameMatching` share one name criterion; the last call replaces the earlier one. `WithNameContaining("Item").WithNameContaining("3")` checks only `"3"`, so it matches both `"Item 3 of 10"` and `"3 apples"`. An element with no semantic name never matches a name criterion.
- **Predicates.** Every `Where` call adds a predicate; a candidate matches only when all predicates return `true`. Predicates receive a `ServoElement` snapshot, run on the UI thread during resolution, and their exceptions propagate to the caller.
- **Position.** `Nth`, `First`, and `Last` are applied last, to the complete list of matches in semantic document order, after the ID, name, role, predicates, and `Within`. The call order does not matter: `delete.Nth(0).Within(row)` and `delete.Within(row).Nth(0)` select the same element. A later `Nth`, `First`, or `Last` replaces the earlier one. A position beyond the match count produces no match.

A target with a position resolves to at most one element for every Servo operation: `FindAsync` and the actions throw `ServoTargetNotFoundException` when the position is out of range, `FindAllAsync` returns zero or one element, and `ExistsAsync` and `WaitForAsync` with `ServoCondition.Exists` or `ServoCondition.Missing` evaluate only the selected element. A target without a position keeps strict cardinality: operations that require one element throw `ServoTargetAmbiguousException` for more than one match.

`WithNameMatching` uses `Regex.IsMatch`, so the pattern can match any part of the name; use `^` and `$` to require the whole name. The supplied `Regex` keeps its own options, for example `RegexOptions.IgnoreCase`.

`ToString()` returns a readable description, and every target failure message of Servo contains it. The criteria appear in the order ID, role, name, predicates; then the scope in parentheses after `within`; then the position. Text values are in double quotes, with `"` and `\` escaped by `\`. A `Where` predicate has no readable form, so each one appears as `where <predicate>`. `Nth(0)` reads `First()`, because both select the same match.

| Target | `ToString()` |
| --- | --- |
| `ServoTarget.ById("save")` | `Id="save"` |
| `ServoTarget.ByRole(SemanticsRole.Button).WithName("Save").Within(ServoTarget.ById("toolbar"))` | `Role=Button, Name="Save" within (Id="toolbar")` |
| `ServoTarget.ByRole(SemanticsRole.Button).WithNameContaining("3 of")` | `Role=Button, Name contains "3 of"` |
| `ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex("item", RegexOptions.IgnoreCase))` | `Role=Button, Name matches /item/ (IgnoreCase)` |
| `ServoTarget.ByRole(SemanticsRole.Button).Where(element => element.IsEnabled)` | `Role=Button, where <predicate>` |
| `ServoTarget.ByRole(SemanticsRole.Button).WithName("Delete").Within(ServoTarget.ById("row").Last()).Nth(2)` | `Role=Button, Name="Delete" within (Id="row", Last()), Nth(2)` |

## Methods

| Name | Description |
| --- | --- |
| `ById(string)` | Creates a target requiring an exact Servo ID. |
| `ByName(string)` | Creates a target requiring an exact semantic name. |
| `ByRole(SemanticsRole)` | Creates a target requiring a semantic role. |
| `WithName(string)` | Returns a new target with the same ID, role, predicates, scope, and position, and the supplied exact semantic name. An earlier name criterion is replaced, not combined: `ByName("A").WithName("B")` matches only the name `B`. |
| `WithNameContaining(string)` | Returns a new target whose name criterion requires the semantic name to contain the supplied text, compared ordinally: `WithNameContaining("save")` does not match `Save`. Replaces an earlier name criterion. Throws `ArgumentException` for null, empty, or white-space text. |
| `WithNameMatching(Regex)` | Returns a new target whose name criterion requires `pattern.IsMatch(name)`. Replaces an earlier name criterion. Throws `ArgumentNullException` for a null pattern. |
| `Where(Func<ServoElement, bool>)` | Returns a new target that additionally requires the predicate to return `true` for the candidate's snapshot. Predicates accumulate. Throws `ArgumentNullException` for a null predicate. |
| `Within(ServoTarget)` | Returns a new target constrained to descendants of matching semantic ancestors. |
| `Nth(int)` | Returns a new target that selects the match at the zero-based index in document order: `Nth(2)` selects the third match. Replaces an earlier position. Throws `ArgumentOutOfRangeException` for a negative index. |
| `First()` | Returns a new target that selects the first match in document order. Same as `Nth(0)`. |
| `Last()` | Returns a new target that selects the last match in document order. |
| `ToString()` | Returns the readable description used in Servo failure messages, for example `Role=Button, Name="Save" within (Id="toolbar")`. |

## Applies to

Servo queries and actions.
