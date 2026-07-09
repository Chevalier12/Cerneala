# MotionTokens Class

## Definition
Namespace: `Cerneala.UI.Motion.States`

Assembly/Project: `Cerneala`

Source: `UI/Motion/States/MotionTokens.cs`

Stores named motion specifications for resolving shared animation timing and spring behavior.

```csharp
public sealed class MotionTokens
```

Inheritance:
`object` -> `MotionTokens`

## Examples

Create a token set and resolve a named motion specification:

```csharp
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.States;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

MotionTokens tokens = new MotionTokens()
    .Set("fade", MotionFactory.Tween(TimeSpan.FromMilliseconds(120), Easings.Standard))
    .Set("layout", MotionFactory.Spring(stiffness: 520, damping: 38));

MotionSpec fadeSpec = tokens.Get("fade");

if (tokens.TryGet("layout", out MotionSpec? layoutSpec))
{
    // layoutSpec is the spring specification registered for the layout token.
}
```

Use the default theme token catalog:

```csharp
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.States;

MotionTokens defaults = ThemeMotionTokens.CreateDefault();
MotionSpec enterSpec = defaults.Get(ThemeMotionTokens.Enter);
```

## Remarks

`MotionTokens` is a mutable, ordinal string-keyed registry of `MotionSpec` instances. Use `Set` to add or replace a token, `TryGet` when missing tokens are expected, and `Get` when a missing token should fail immediately.

Token names must not be null, empty, or whitespace. `Set` also rejects a null `MotionSpec`. `Get` throws `KeyNotFoundException` when the token name is valid but no specification has been registered for it.

`DefaultDuration` is a convenience duration value of 200 milliseconds. The class does not automatically apply that duration to registered specs.

`ThemeMotionTokens.CreateDefault()` builds a populated `MotionTokens` instance for the built-in theme motion names, and `DefaultTheme.Create()` registers that instance under `ThemeMotionTokens.Key`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTokens()` | Initializes an empty token registry with `DefaultDuration` set to 200 milliseconds. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DefaultDuration` | `TimeSpan` | Gets the default convenience duration, initialized to 200 milliseconds. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set(string name, MotionSpec spec)` | `MotionTokens` | Adds or replaces the motion specification for `name`, then returns the same registry for chaining. |
| `TryGet(string name, out MotionSpec spec)` | `bool` | Attempts to resolve the specification registered for `name`. |
| `Get(string name)` | `MotionSpec` | Returns the specification registered for `name`, or throws `KeyNotFoundException` if the token is missing. |

## Applies to

Cerneala UI motion systems, theme motion token resolution, and application code that shares named `MotionSpec` instances.

## See also

- `Cerneala.UI.Motion.States.ThemeMotionTokens`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Theming.DefaultTheme`
