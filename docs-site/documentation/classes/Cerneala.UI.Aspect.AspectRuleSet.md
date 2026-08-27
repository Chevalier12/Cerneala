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
using Cerneala.UI.Media;

AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    AspectValue<Brush?>.Literal(new SolidColorBrush(Color.Black)));

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
using Cerneala.UI.Media;

Button button = new();
AspectDeclaration first = new(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)));
AspectDeclaration second = new(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.Black)));

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

Cascade resolution compares matching rules by layer order first, internal source/scope order second, target specificity third, and declaration order last. A rule in a higher `AspectLayer.Order` wins before source scope is considered. Within the same layer, inner/runtime-composed sources win over outer sources, then a more specific target wins. If those coordinates are equal, the rule with the higher `DeclarationOrder` wins. Ordinary standalone rules have source order `0`.

`ResolveDeclarations(IEnumerable<AspectRuleSet>, AspectMatchContext)` returns the winning raw `AspectDeclaration` for each `UiProperty`. Runtime value resolution, token lookup, rejected declaration diagnostics, and dependency tracking are handled by `AspectEngine.Resolve`.

The declaration input is copied into an immutable snapshot. Later changes to the caller-owned list do not change the rule.

`PackageName`, `SourceOrder`, `Origin`, and `Scope` are assigned to the catalog-owned projection created while packages are merged into an `AspectCatalog`. The reusable source rule remains unchanged, and building another catalog from that rule cannot rewrite metadata reported by an existing catalog. Origin metadata is diagnostic only; source order remains the only scope coordinate in the cascade key.

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
| `Declarations` | `IReadOnlyList<AspectDeclaration>` | Gets the immutable declaration snapshot contributed by this rule set. |
| `DeclarationOrder` | `int` | Gets the declaration order used as the final cascade tie-breaker. |
| `PackageName` | `string?` | Gets the package name on a catalog-owned rule projection, or `null` on a reusable rule that has not been projected into a catalog. |
| `SourceOrder` | `int` | Gets the composed root/application/scope/element source rank used after layer order. |
| `Origin` | `AspectOrigin` | Gets immutable authoring-origin metadata. |
| `Scope` | `string` | Gets the deterministic diagnostic scope label: `root`, `application`, `scope[n]`, or `element`. |

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
