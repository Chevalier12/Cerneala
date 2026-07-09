# Theme Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/Theme.cs`

Stores typed theme values by `ThemeKey<T>` and exposes typed lookup helpers for UI theming.

```csharp
public sealed class Theme
```

Inheritance:
`object` -> `Theme`

## Examples

Create a named theme, store a typed color value, and read it back:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemeKey<DrawColor> accentKey = new("Accent");
Theme theme = new Theme("Editor").Set(accentKey, DrawColor.White);

if (theme.TryGet(accentKey, out DrawColor accent))
{
    Console.WriteLine(accent);
}

DrawColor requiredAccent = theme.Get(accentKey);
```

## Remarks

`Theme` is an in-memory typed value container for theme data. Entries are keyed by both the `ThemeKey<T>.Key` string and the generic value type `T`, so two keys with the same string but different value types are stored as separate entries.

The optional `Name` identifies the theme for callers that need a display or diagnostic name. The constructor accepts `null`, but throws `ArgumentException` when a non-null name is empty or whitespace.

`Set<T>` stores the value for the supplied key and returns the same `Theme` instance, allowing chained theme construction. `DefaultTheme.Create()` uses this pattern to build the built-in theme.

`TryGet<T>` returns `true` only when a matching typed value is present. It also treats a stored `null` as a valid result for nullable or reference-type values. `Get<T>` returns the typed value or throws `KeyNotFoundException` when the value is missing.

`ThemeProvider` wraps a `Theme` when consumers need change notifications, and `ThemeTokenBridge.CreateEnvironment` projects selected `DefaultTheme` values into aspect tokens.

## Constructors

| Name | Description |
| --- | --- |
| `Theme(string? name = null)` | Initializes an empty theme with an optional name. Throws `ArgumentException` when `name` is empty or whitespace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | Gets the optional theme name supplied to the constructor. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(ThemeKey<T> key, T value)` | `Theme` | Stores `value` for the typed theme key and returns this theme instance. |
| `TryGet<T>(ThemeKey<T> key, out T value)` | `bool` | Attempts to retrieve a typed theme value. Returns `true` when a compatible entry is present. |
| `Get<T>(ThemeKey<T> key)` | `T` | Retrieves a typed theme value, or throws `KeyNotFoundException` when no matching entry exists. |

## Applies to

`Cerneala.UI.Theming` in the `Cerneala` project.

## See also

- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Theming.ThemeProvider`
- `Cerneala.UI.Aspect.ThemeTokenBridge`
