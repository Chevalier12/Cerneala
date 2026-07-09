# ResolvedAspect Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/ResolvedAspect.cs`

Represents the result of resolving aspect rules for an element, including winning values, matched rules, rejected declarations, and dependency tracking data.

```csharp
public sealed class ResolvedAspect
```

Inheritance:
`object` -> `ResolvedAspect`

## Examples

Resolve aspect rules without applying them to the element:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
Button button = new();

ResolvedAspect resolved = new AspectEngine().Resolve(button, catalog, environment);

foreach ((var property, ResolvedAspectValue value) in resolved.Values)
{
    Console.WriteLine($"{property.DiagnosticName}: {value.Value}");
}
```

Inspect cascade decisions after resolving:

```csharp
foreach (RejectedAspectDeclaration rejected in resolved.RejectedDeclarations)
{
    Console.WriteLine($"{rejected.Rejected.Property.DiagnosticName}: {rejected.Reason}");
}
```

## Remarks

`ResolvedAspect` is produced by `AspectEngine.Resolve` and returned from `AspectEngine.Apply` through `AspectApplicationResult.ResolvedAspect`. It is a read-only container over collections supplied by the resolver; the constructor requires non-null values for every collection and for the dependency set.

`Values` is keyed by `UiProperty` and stores only the winning `ResolvedAspectValue` for each property. When several matching declarations target the same property, `AspectEngine` keeps the declaration with the strongest cascade key and records the losing declaration in `RejectedDeclarations`.

`MatchedRules` contains the `AspectRuleSet` instances whose targets matched the resolution context. `Dependencies` records the tokens, states, variants, properties, data dependencies, slot, catalog version, and environment version observed during resolution so the aspect system can diagnose and track invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `ResolvedAspect(IReadOnlyDictionary<UiProperty, ResolvedAspectValue> values, IReadOnlyList<AspectRuleSet> matchedRules, IReadOnlyList<RejectedAspectDeclaration> rejectedDeclarations, AspectDependencySet dependencies)` | Initializes a resolved aspect result. Throws `ArgumentNullException` when any argument is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Values` | `IReadOnlyDictionary<UiProperty, ResolvedAspectValue>` | Gets the winning resolved value for each aspect-controlled UI property. |
| `MatchedRules` | `IReadOnlyList<AspectRuleSet>` | Gets the rule sets that matched the element and resolution context. |
| `RejectedDeclarations` | `IReadOnlyList<RejectedAspectDeclaration>` | Gets declarations that lost to another declaration for the same property, with the recorded reason. |
| `Dependencies` | `AspectDependencySet` | Gets the dependency set captured during resolution for diagnostics and invalidation tracking. |

## Applies to

Cerneala UI aspect resolution results returned by `AspectEngine`.

## See also

- `AspectEngine`
- `AspectApplicationResult`
- `ResolvedAspectValue`
- `RejectedAspectDeclaration`
- `AspectDependencySet`
