# AspectRuleSet Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectRuleSet.cs`

Represents a named set of aspect declarations that applies to a target and participates in aspect cascade resolution.

```csharp
public sealed class AspectRuleSet
```

Inheritance:
`object` -> `AspectRuleSet`

## Examples

Create a rule set that assigns a background value to `Button` controls:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    AspectValue<Color>.Literal(Color.Black));

AspectRuleSet rule = new(
    "button-background",
    AspectLayer.App,
    new AspectTarget(typeof(Button)),
    [declaration],
    declarationOrder: 0);
```

Resolve the winning declaration for a matching context:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;

Button button = new();
AspectDeclaration first = new(Control.BackgroundProperty, AspectValue<Color>.Literal(Color.White));
AspectDeclaration second = new(Control.BackgroundProperty, AspectValue<Color>.Literal(Color.Black));

AspectRuleSet themeRule = new("theme", AspectLayer.Theme, new AspectTarget(typeof(Button)), [first], 10);
AspectRuleSet appRule = new("app", AspectLayer.App, new AspectTarget(typeof(Button)), [second], 1);

AspectMatchContext context = new(
    button,
    ownerComponent: button,
    slotPath: null,
    states: AspectStateSet.Empty,
    variants: AspectVariantSet.Empty,
    environmentVersion: 0,
    dataContext: AspectDataContext.Empty);

IReadOnlyDictionary<UiProperty, AspectDeclaration> resolved =
    AspectRuleSet.ResolveDeclarations([themeRule, appRule], context);
```

## Remarks

`AspectRuleSet` groups the declarations that should be considered together for one aspect target. The target decides whether the rule matches an `AspectMatchContext`; the declarations provide the `UiProperty` values that may win during resolution.

Cascade resolution compares matching rules by layer order first, target specificity second, and declaration order last. A rule in a higher `AspectLayer.Order` wins before declaration order is considered. Within the same layer, a more specific target wins. If both layer and specificity are equal, the rule with the higher `DeclarationOrder` wins.

`ResolveDeclarations(IEnumerable<AspectRuleSet>, AspectMatchContext)` returns the winning raw `AspectDeclaration` for each `UiProperty`. Runtime value resolution, token lookup, rejected declaration diagnostics, and dependency tracking are handled by `AspectEngine.Resolve`.

`PackageName` is set internally when packages are merged into an `AspectCatalog`; it is used by diagnostics and is not set through the public constructor.

## Constructors

| Name | Description |
| --- | --- |
| `AspectRuleSet(string name, AspectLayer layer, AspectTarget target, IReadOnlyList<AspectDeclaration> declarations, int declarationOrder)` | Initializes a named rule set for `target` in `layer`, with the supplied declarations and cascade declaration order. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the rule set name used for identification and diagnostics. |
| `Layer` | `AspectLayer` | Gets the cascade layer for the rule set. Higher layer order wins before specificity and declaration order. |
| `Target` | `AspectTarget` | Gets the target type, optional slot, and conditions used to match an `AspectMatchContext`. |
| `Declarations` | `IReadOnlyList<AspectDeclaration>` | Gets the declarations contributed by this rule set. |
| `DeclarationOrder` | `int` | Gets the declaration order used as the final cascade tie-breaker. |
| `PackageName` | `string?` | Gets the package name assigned internally during catalog building, or `null` before assignment. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(AspectMatchContext context)` | `bool` | Returns `true` when `Target` matches the supplied context. |
| `ResolveDeclarations(IEnumerable<AspectRuleSet> rules, AspectMatchContext context)` | `IReadOnlyDictionary<UiProperty, AspectDeclaration>` | Filters matching rules and returns the winning declaration for each UI property using the aspect cascade key. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectRuleSet(...)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `AspectRuleSet(...)` | `ArgumentNullException` | `layer`, `target`, or `declarations` is `null`. |
| `ResolveDeclarations(IEnumerable<AspectRuleSet>, AspectMatchContext)` | `ArgumentNullException` | `rules` or `context` is `null`. |

## Applies to

Cerneala UI aspect matching and cascade resolution.

## See also

- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Aspect.AspectDeclaration`
- `Cerneala.UI.Aspect.AspectLayer`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.AspectCatalog`
