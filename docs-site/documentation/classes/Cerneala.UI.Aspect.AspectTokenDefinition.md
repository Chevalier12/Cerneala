# AspectTokenDefinition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectTokenDefinition.cs`

Pairs an aspect token with the default value contributed for that token by an aspect package.

```csharp
public sealed class AspectTokenDefinition
```

Inheritance:
`object` -> `AspectTokenDefinition`

## Examples

Create a definition directly when the token and default value are already available:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accent = AspectToken.Color("app.accent");

var definition = new AspectTokenDefinition(
    accent,
    AspectValue<Color>.Literal(Color.White));
```

Most package code creates token definitions through `AspectTokenBuilder.Set<T>`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accent = AspectToken.Color("app.accent");

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(accent, Color.White))
    .Build();
```

## Remarks

`AspectTokenDefinition` is the catalog input for package-level token defaults. `AspectPackage.Tokens` stores these definitions, and `AspectCatalog.FromPackages` copies each `Token` and `DefaultValue` pair into the catalog default map.

The constructor requires both arguments to be non-null and requires `token.ValueType` to match `defaultValue.ValueType`. This keeps a token such as `AspectToken<Color>` from being registered with an `AspectValue` whose runtime value type is something else.

When multiple packages contribute the same token identity, catalog creation stores the later default value for that token. If packages use the same token name with different value types, `AspectCatalog` throws an `InvalidOperationException` while building the catalog.

## Constructors

| Name | Description |
| --- | --- |
| `AspectTokenDefinition(AspectToken token, AspectValue defaultValue)` | Initializes a token definition after validating that both arguments are present and have matching value types. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Token` | `AspectToken` | Gets the named, typed token whose default is being registered. |
| `DefaultValue` | `AspectValue` | Gets the default aspect value associated with `Token`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectTokenDefinition(AspectToken, AspectValue)` | `ArgumentNullException` | `token` or `defaultValue` is `null`. |
| `AspectTokenDefinition(AspectToken, AspectValue)` | `ArgumentException` | `defaultValue.ValueType` does not match `token.ValueType`. |

## Applies to

Cerneala UI aspect package composition and catalog token default registration.

## See also

- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectValue`
- `Cerneala.UI.Aspect.AspectTokenBuilder`
- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectCatalog`
