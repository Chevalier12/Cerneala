# AspectLayer Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectLayer.cs`

Represents a named cascade layer used to order aspect rule resolution.

```csharp
public sealed class AspectLayer : IEquatable<AspectLayer>, IComparable<AspectLayer>
```

Inheritance:
`object` -> `AspectLayer`

Implements:
`IEquatable<AspectLayer>`, `IComparable<AspectLayer>`

## Examples

Create a rule in the application layer:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectRuleSet rule = new(
    "button.primary",
    AspectLayer.App,
    new AspectTarget(typeof(Button)),
    [new AspectDeclaration(Control.BackgroundProperty, AspectValue<DrawColor>.Literal(DrawColor.Black))],
    declarationOrder: 0);
```

Create a custom layer between existing layers:

```csharp
using Cerneala.UI.Aspect;

AspectLayer designSystemLayer = new("DesignSystem", 250);

bool appLayerWinsLater = AspectLayer.App.CompareTo(designSystemLayer) > 0;
```

## Remarks

`AspectLayer` is one part of the aspect cascade key used by `AspectRuleSet.ResolveDeclarations`. When multiple matching rules set the same UI property, the resolver compares layer order first, then target specificity, then declaration order. A larger `Order` value wins over a smaller one.

The built-in layers are ordered as `Reset` (0), `Theme` (100), `Component` (200), `App` (300), `User` (400), and `Runtime` (500). This lets runtime state rules override application rules, and application rules override theme rules, when specificity and declaration order would otherwise be considered later.

Layer equality uses both `Name` and `Order` with ordinal name comparison. `CompareTo` uses only `Order`, so two different layers with the same order compare as equal for sorting even though `Equals` can still return `false`.

The constructor rejects `null`, empty, or whitespace-only names with `ArgumentException`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectLayer(string name, int order)` | Initializes a layer with a non-empty `Name` and numeric cascade `Order`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Reset` | `AspectLayer` | Gets the built-in reset layer with order `0`. |
| `Theme` | `AspectLayer` | Gets the built-in theme layer with order `100`. |
| `Component` | `AspectLayer` | Gets the built-in component layer with order `200`. |
| `App` | `AspectLayer` | Gets the built-in application layer with order `300`. |
| `User` | `AspectLayer` | Gets the built-in user layer with order `400`. |
| `Runtime` | `AspectLayer` | Gets the built-in runtime layer with order `500`. |
| `Name` | `string` | Gets the layer name. |
| `Order` | `int` | Gets the numeric cascade order used by sorting and aspect resolution. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CompareTo(AspectLayer? other)` | `int` | Compares this layer with another layer by `Order`; a non-null layer sorts after `null`. |
| `Equals(AspectLayer? other)` | `bool` | Returns `true` when both layers have the same `Order` and ordinal `Name`. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an equal `AspectLayer`. |
| `GetHashCode()` | `int` | Returns a hash code based on the ordinal `Name` and `Order`. |
| `ToString()` | `string` | Returns the layer as `Name:Order`. |

## Applies to

Cerneala UI aspect rule ordering and declaration resolution.

## See also

- `AspectRuleSet`
- `AspectTarget`
- `AspectSpecificity`
