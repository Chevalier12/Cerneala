# AspectEnvironment Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectEnvironment.cs`

Stores typed aspect token values for aspect resolution and template token binding, with optional fallback to a parent environment.

```csharp
public sealed class AspectEnvironment
```

Inheritance:
`object` -> `AspectEnvironment`

## Examples

Create an environment, set a token value, and read it back:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accentToken = AspectToken.Color("app.accent");
AspectEnvironment environment = new("app");

environment.Set(accentToken, Color.White);

if (environment.TryGet(accentToken, out Color accent))
{
    // Use accent.
}
```

Use a child scope to override only selected tokens:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accentToken = AspectToken.Color("app.accent");
AspectEnvironment root = new("root");
root.Set(accentToken, Color.White);

AspectEnvironment child = root.CreateChildScope("button");
child.Set(accentToken, Color.Black);

child.TryGet(accentToken, out Color childAccent); // Color.Black
root.TryGet(accentToken, out Color rootAccent);   // Color.White
```

## Remarks

`AspectEnvironment` is the lookup table used when an `AspectValue<T>` references an `AspectToken<T>`. `AspectResolutionContext` exposes the active environment to declaration resolution, and `AspectProcessor` keeps a default environment that is synchronized with token defaults from the current aspect catalog.

Values are stored by `AspectToken`. Tokens compare by ordinal name and value type, so separately created tokens with the same name and type address the same environment entry.

`TryGet<T>` first checks the current environment. If the token is not present, it walks to the parent environment created by `CreateChildScope`. A value found in the current environment shadows the parent value for the same token.

Calling either `Set` overload increments `Version`, including when replacing an existing value. `AspectEngine.Resolve` includes the current environment version in its match context so environment changes can participate in resolution and diagnostics.

The generic `Set<T>` overload accepts the compile-time token type. The non-generic `Set(AspectToken, object?)` overload validates non-null values against `token.ValueType` and throws `ArgumentException` when the value cannot be assigned to that token type. `TryGet<T>` returns `false` when the stored value is incompatible with the requested typed token.

## Constructors

| Name | Description |
| --- | --- |
| `AspectEnvironment(string name, AspectEnvironment? parent = null)` | Initializes an environment with a non-empty name and optional parent fallback scope. Throws `ArgumentException` when `name` is null, empty, or whitespace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the environment name used for diagnostics, including missing token errors. |
| `Version` | `int` | Gets the number of successful `Set` calls made directly on this environment. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(AspectToken<T> token, T value)` | `void` | Stores a typed token value in the current environment and increments `Version`. Throws `ArgumentNullException` when `token` is `null`. |
| `Set(AspectToken token, object? value)` | `void` | Stores a value through a non-generic token, validating non-null values against `token.ValueType`, then increments `Version`. Throws `ArgumentNullException` when `token` is `null`; throws `ArgumentException` when `value` has the wrong runtime type. |
| `TryGet<T>(AspectToken<T> token, out T value)` | `bool` | Attempts to read a typed token value from the current environment, then from parent scopes. Returns `true` only when a compatible value is found. Throws `ArgumentNullException` when `token` is `null`. |
| `CreateChildScope(string name)` | `AspectEnvironment` | Creates a child environment that falls back to the current environment for missing tokens. The same name validation as the constructor applies. |

## Applies to

Cerneala UI aspect token resolution, default aspect packages, theme token projection, and template token binding.

## See also

- `AspectToken`
- `AspectValue<T>`
- `AspectResolutionContext`
- `AspectProcessor`
- `DefaultAspectPackage`
- `ThemeTokenBridge`
