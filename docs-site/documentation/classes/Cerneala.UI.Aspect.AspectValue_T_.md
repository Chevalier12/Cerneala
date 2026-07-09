# AspectValue<T> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectValue{T}.cs`

Represents a typed aspect value that resolves either a literal value, a referenced aspect token, or a computed value.

```csharp
public sealed class AspectValue<T> : AspectValue
```

Inheritance:
`object` -> `AspectValue` -> `AspectValue<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The type produced when the aspect value is resolved. |

## Examples

Create a literal aspect value and resolve it:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

AspectValue<Thickness> padding = AspectValue<Thickness>.Literal(new Thickness(8));

object? resolved = padding.Resolve(
    new AspectResolutionContext(new Button(), new AspectEnvironment("example")));
```

Create a token-backed aspect value and resolve it through an environment:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");

AspectEnvironment environment = new("example");
environment.Set(accent, DrawColor.White);

AspectValue<DrawColor> value = accent.Ref();
object? resolved = value.Resolve(new AspectResolutionContext(new Button(), environment));
```

Create a computed aspect value with explicit dependencies:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<float> baseSize = AspectToken.Float("app.baseSize");

AspectValue<float> largeSize = AspectValue<float>.Computed(
    context =>
    {
        if (!context.Environment.TryGet(baseSize, out float size))
        {
            return 16f;
        }

        return size * 1.25f;
    },
    [baseSize]);
```

## Remarks

`AspectValue<T>` is the typed implementation of the non-generic `AspectValue` contract. The class has private constructors; create values with `Literal(T)`, `Token(AspectToken<T>)`, or `Computed(Func<AspectResolutionContext, T>, IReadOnlyList<AspectToken>)`.

`ValueType` always returns `typeof(T)`. `Dependencies` is empty for literal values, contains the referenced token for token-backed values, and contains a copied array of the dependencies passed to `Computed`.

`Resolve(AspectResolutionContext)` requires a non-null context. Computed values call the supplied delegate. Token-backed values call `context.Environment.TryGet(token, out T value)` and throw `InvalidOperationException` when the token is not present or cannot be retrieved as `T`. Literal values return the stored literal, including `null` when `T` allows it.

`Token` and `Computed` validate their required arguments and throw `ArgumentNullException` for a null token, compute delegate, or dependencies list. `Resolve` throws `ArgumentNullException` for a null context.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueType` | `Type` | Gets `typeof(T)`, the runtime type this aspect value resolves. |
| `Dependencies` | `IReadOnlyList<AspectToken>` | Gets the tokens needed to resolve this value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Literal(T value)` | `AspectValue<T>` | Creates a value that resolves to the supplied literal. |
| `Token(AspectToken<T> token)` | `AspectValue<T>` | Creates a value that resolves the supplied token from the context environment. |
| `Computed(Func<AspectResolutionContext, T> compute, IReadOnlyList<AspectToken> dependencies)` | `AspectValue<T>` | Creates a value that resolves by invoking `compute` and reports the supplied dependency tokens. |
| `Resolve(AspectResolutionContext context)` | `object?` | Resolves the typed value using the supplied context. |

## Applies to

Cerneala UI aspect declarations, token defaults, aspect rule sets, and aspect environment resolution.

## See also

- `Cerneala.UI.Aspect.AspectValue`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectResolutionContext`
- `Cerneala.UI.Aspect.AspectDeclaration`
