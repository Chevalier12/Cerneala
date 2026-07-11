# AspectToken Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectToken.cs`

Represents a named, typed key used to register, compare, and resolve aspect values.

```csharp
public abstract class AspectToken : IEquatable<AspectToken>
```

Inheritance:
`object` -> `AspectToken`

Derived:
`AspectToken<T>`

Implements:
`IEquatable<AspectToken>`

## Examples

Create typed tokens for common aspect value types:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

AspectToken<Color> accent = AspectToken.Color("app.accent");
AspectToken<Thickness> padding = AspectToken.Thickness("app.padding");
AspectToken<string> fontFamily = AspectToken.String("app.font-family");
AspectToken<float> opacity = AspectToken.Float("app.opacity");
```

Use token identity when registering a default value:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accent = AspectToken.Color("app.accent");

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(accent, Color.White))
    .Build();
```

## Remarks

`AspectToken` is the non-generic base for typed aspect tokens. Each token has a non-empty `Name` and a `ValueType`. The constructor is `private protected`, so callers create tokens through the static factory methods or through the derived `AspectToken<T>` type returned by those factories.

Token equality uses ordinal name comparison and exact `ValueType` equality. Two tokens with the same name and the same value type compare equal, even when they are separate instances. Tokens with the same name but different value types do not compare equal.

`AspectCatalog` also checks token names across packages. If the same token name is registered with different value types, catalog creation throws `InvalidOperationException`.

`AspectToken<T>.Ref()` creates an `AspectValue<T>` that resolves the token from an `AspectEnvironment`. If the environment does not contain the token, resolution throws `InvalidOperationException` with the token name and value type in the diagnostic message.

`ToString()` returns a diagnostic string containing `ValueType.FullName` and `Name`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the non-empty token name used for equality, hashing, diagnostics, and catalog conflict checks. |
| `ValueType` | `Type` | Gets the value type carried by the token. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create<T>(string name)` | `AspectToken<T>` | Creates a typed token with the supplied non-empty name and `typeof(T)` as its value type. |
| `Color(string name)` | `AspectToken<Color>` | Creates a `Color` token. |
| `Thickness(string name)` | `AspectToken<Thickness>` | Creates a `Thickness` token. |
| `Float(string name)` | `AspectToken<float>` | Creates a `float` token. |
| `String(string name)` | `AspectToken<string>` | Creates a `string` token. |
| `Motion(string name)` | `AspectToken<MotionSpec>` | Creates a `MotionSpec` token. |
| `Equals(AspectToken? other)` | `bool` | Returns `true` when `other` has the same name using ordinal comparison and the same `ValueType`. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an `AspectToken` equal to this token. |
| `GetHashCode()` | `int` | Returns a hash code based on the ordinal `Name` hash and `ValueType`. |
| `ToString()` | `string` | Returns a diagnostic string in the form `ValueType.FullName:Name`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Create<T>(string name)` and typed factory methods | `ArgumentException` | `name` is null, empty, or whitespace. |

## Applies to

Cerneala UI aspect tokens, aspect package defaults, aspect environments, and aspect value resolution.

## See also

- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.DefaultAspectTokens`
