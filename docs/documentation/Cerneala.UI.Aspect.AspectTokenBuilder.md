# AspectTokenBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectPackageBuilder.cs`

Collects aspect token default values for an `AspectPackageBuilder.Tokens` callback.

```csharp
public sealed class AspectTokenBuilder
```

Inheritance:
`object` -> `AspectTokenBuilder`

## Examples

Set default values for package tokens:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");
AspectToken<Thickness> padding = AspectToken.Thickness("app.padding");

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens
        .Set(accent, new DrawColor(37, 99, 235))
        .Set(padding, new Thickness(8)));
```

Register the package and read the default through the built catalog:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");

AspectCatalog catalog = new AspectRegistry()
    .Register(AspectPackage.Create("App")
        .Tokens(tokens => tokens.Set(accent, DrawColor.White)))
    .BuildCatalog();

bool found = catalog.TryGetTokenDefault(accent, out AspectValue? defaultValue);
```

## Remarks

`AspectTokenBuilder` instances are supplied by `AspectPackageBuilder.Tokens(Action<AspectTokenBuilder>)`. The constructor is internal, so package authors normally use the builder only inside the `Tokens` callback.

Each call to `Set<T>` appends an `AspectTokenDefinition` to the package being built. The supplied value is wrapped with `AspectValue<T>.Literal`, so the package contributes a literal default for that token.

`Set<T>` returns the same `AspectTokenBuilder` instance, which allows multiple token defaults to be chained in a single callback. The collected definitions become entries in `AspectPackage.Tokens` when `AspectPackageBuilder.Build()` materializes the package.

Catalog creation merges token defaults from registered packages in registration order. If later packages contribute the same token identity, the later default replaces the earlier one. If the same token name is registered with different value types, `AspectCatalog` creation throws an `InvalidOperationException`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(AspectToken<T> token, T value)` | `AspectTokenBuilder` | Adds a literal default value definition for `token` and returns this builder. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Set<T>(AspectToken<T>, T)` | `ArgumentNullException` | `token` is `null`. |

## Applies to

Cerneala UI aspect package composition and token default registration.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectTokenDefinition`
- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectCatalog`
