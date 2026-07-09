# AspectToken<T> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectToken{T}.cs`

Represents a strongly typed aspect token whose values must use `T`.

```csharp
public sealed class AspectToken<T> : AspectToken
```

Inheritance:
`object` -> `AspectToken` -> `AspectToken<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type stored and resolved for the token. |

## Examples

Create a typed color token, store a value in an aspect environment, and resolve a token reference:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");

AspectEnvironment environment = new("root");
environment.Set(accent, DrawColor.White);

AspectResolutionContext context = new(new Button(), environment);
object? resolved = accent.Ref().Resolve(context);
```

Create a custom typed token through the generic factory:

```csharp
using Cerneala.UI.Aspect;

AspectToken<double> opacity = AspectToken.Create<double>("app.opacity");

Console.WriteLine(opacity.Name);      // app.opacity
Console.WriteLine(opacity.ValueType); // System.Double
```

## Remarks

`AspectToken<T>` is the typed form of `AspectToken`. Its internal constructor passes the token name and `typeof(T)` to the base `AspectToken`, so `Name` and `ValueType` are inherited from the non-generic base class.

Create instances with `AspectToken.Create<T>(string)` or with one of the typed helpers on `AspectToken`, such as `Color(string)`, `Thickness(string)`, `Float(string)`, `String(string)`, or `Motion(string)`. The constructor is not public.

`Ref()` returns an `AspectValue<T>` token reference. When that value is resolved, it reads the matching token from the supplied `AspectResolutionContext.Environment`. If the token is missing from the environment, resolution throws an `InvalidOperationException` that includes the token name and expected value type.

Equality and hash code behavior are inherited from `AspectToken`: two tokens are equal when they have the same ordinal `Name` and the same `ValueType`. Tokens with the same name but different value types are different tokens.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Inherited. Gets the token name. |
| `ValueType` | `Type` | Inherited. Gets `typeof(T)` for this typed token. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Ref()` | `AspectValue<T>` | Creates an `AspectValue<T>` that resolves this token through an `AspectResolutionContext`. |
| `Equals(AspectToken? other)` | `bool` | Inherited. Returns `true` when `other` has the same name and value type. |
| `Equals(object? obj)` | `bool` | Inherited. Returns `true` when `obj` is an equal `AspectToken`. |
| `GetHashCode()` | `int` | Inherited. Returns a hash code based on `Name` and `ValueType`. |
| `ToString()` | `string` | Inherited. Returns a diagnostic label in the form `ValueType.FullName:Name`. |

## Applies to

Cerneala UI aspect tokens, aspect environments, token defaults, and token-based aspect value resolution.

## See also

- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectResolutionContext`
- `Cerneala.UI.Aspect.AspectRef`
