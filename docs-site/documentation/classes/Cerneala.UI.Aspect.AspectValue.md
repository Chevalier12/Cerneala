# AspectValue Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectValue.cs`

Defines the non-generic base contract for aspect values that can resolve to a runtime value from an `AspectResolutionContext`.

```csharp
public abstract class AspectValue
```

Inheritance:
`object` -> `AspectValue`

Derived:
`AspectValue<T>`

## Examples

Use an `AspectValue` returned from an aspect catalog without knowing its generic value type at the call site:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<Color> accent = AspectToken.Color("app.accent");

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(accent, Color.White));

AspectCatalog catalog = new AspectRegistry()
    .Register(package)
    .BuildCatalog();

if (catalog.TryGetTokenDefault(accent, out AspectValue value))
{
    object? resolved = value.Resolve(
        new AspectResolutionContext(new Button(), new AspectEnvironment("example")));
}
```

## Remarks

`AspectValue` is the type-erased base used by catalogs, declarations, and processors when they need to store or resolve aspect values without carrying the generic `T` type parameter.

The concrete `AspectValue<T>` implementation reports `typeof(T)` from `ValueType`, exposes the tokens it depends on through `Dependencies`, and resolves one of three value shapes: a literal value, another token from the `AspectEnvironment`, or a computed value produced from the `AspectResolutionContext`.

`Resolve(AspectResolutionContext)` returns `object?` so callers that use the base type must cast or validate the result when they need a concrete value type. Token-backed resolution can throw when the referenced token is missing from the environment; computed values can also throw whatever their supplied delegate throws.

`AspectProcessor` resolves catalog token defaults through this base contract before storing the resolved values in its shared `AspectEnvironment`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueType` | `Type` | Gets the value type produced by this aspect value. |
| `Dependencies` | `IReadOnlyList<AspectToken>` | Gets the aspect tokens required to resolve this value. Literal values have no dependencies; token and computed values report the dependencies supplied by their concrete implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve(AspectResolutionContext context)` | `object?` | Resolves the aspect value for the supplied context. The concrete implementation decides whether the result comes from a literal, an environment token lookup, or a computation. |

## Applies to

Cerneala UI aspect package defaults, aspect declarations, aspect catalogs, and aspect processing.

## See also

- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectResolutionContext`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectCatalog`
