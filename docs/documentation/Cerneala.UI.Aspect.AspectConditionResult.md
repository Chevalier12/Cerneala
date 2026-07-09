# AspectConditionResult Class

## Definition

Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionResult.cs`

Represents the match state, invalidation dependencies, diagnostics, and child results produced by evaluating an `AspectCondition`.

```csharp
public sealed class AspectConditionResult
```

Inheritance:
`object` -> `AspectConditionResult`

## Examples

Evaluate a condition and inspect its result:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.State(AspectState.Hover);
AspectMatchContext context = new(
    new Button(),
    states: AspectStateSet.Empty.Add(AspectState.Hover));

AspectConditionResult result = condition.Evaluate(context);

bool matched = result.Matches;
IReadOnlyList<AspectConditionDependency> dependencies = result.Dependencies;
string diagnostic = result.DiagnosticText;
```

Inspect child results from a compound condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.All(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.State(AspectState.Focus));

AspectConditionResult result = condition.Evaluate(new AspectMatchContext(new Button()));

foreach (AspectConditionResult child in result.Children)
{
    Console.WriteLine($"{child.DiagnosticText}: {child.Matches}");
}
```

## Remarks

`AspectConditionResult` is returned by `AspectCondition.Evaluate(AspectMatchContext)` and by the internal aspect condition nodes used by the aspect matching pipeline.

`Matches` is the boolean result of the evaluated condition. For compound conditions, `All` matches when every child result matches, `Any` matches when at least one child result matches, and `Not` stores the child result while inverting its `Matches` value.

`Dependencies` records the inputs that affected evaluation. Built-in condition nodes report dependencies for aspect states, variants, UI properties, data context entries, or custom predicate diagnostics. `AspectEngine.Resolve` collects these dependencies into the resolved aspect dependency set so later invalidation can react to relevant input changes.

`DiagnosticText` stores short human-readable text supplied by the condition node. The constructor normalizes a `null` diagnostic string to `string.Empty`.

`Children` contains nested results for compound conditions. Simple conditions use an empty child collection. The constructor requires a non-null `Dependencies` collection and uses an empty child collection when `children` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectConditionResult(bool matches, IReadOnlyList<AspectConditionDependency> dependencies, string diagnosticText, IReadOnlyList<AspectConditionResult>? children = null)` | Initializes a condition result with match state, dependency records, diagnostic text, and optional child results. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Matches` | `bool` | Gets whether the evaluated condition matched the supplied `AspectMatchContext`. |
| `Dependencies` | `IReadOnlyList<AspectConditionDependency>` | Gets the dependencies reported by the evaluated condition for invalidation tracking. |
| `DiagnosticText` | `string` | Gets the diagnostic text describing the evaluated condition result. |
| `Children` | `IReadOnlyList<AspectConditionResult>` | Gets child condition results for compound conditions, or an empty collection for simple conditions. |

## Applies to

Cerneala UI aspect condition evaluation, diagnostics, and invalidation dependency tracking.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectEngine`
