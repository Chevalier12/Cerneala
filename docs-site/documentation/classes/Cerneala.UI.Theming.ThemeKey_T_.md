# ThemeKey<T> Struct

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemeKey{T}.cs`

Identifies a theme entry by a non-empty string key and a typed value contract.

```csharp
public readonly record struct ThemeKey<T>
```

Inheritance:
`ValueType` -> `ThemeKey<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type associated with the theme entry. |

## Examples

Create a typed color key, store a value in a theme, and retrieve it with the same key:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemeKey<Color> accentKey = new("Accent");
Theme theme = new Theme("Editor").Set(accentKey, Color.White);

Color accent = theme.Get(accentKey);
```

Keys with the same text but different value types are separate theme entries:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemeKey<Color> colorKey = new("Accent");
ThemeKey<string> labelKey = new("Accent");

Theme theme = new Theme()
    .Set(colorKey, Color.White)
    .Set(labelKey, "Primary accent");

Color accent = theme.Get(colorKey);
string label = theme.Get(labelKey);
```

## Remarks

`ThemeKey<T>` is the typed key used by `Theme`, `ThemeProvider`, and `ThemeResource<T>`. The `Key` string names the theme entry, while the generic type parameter `T` is part of the lookup identity. `Theme` stores values by both the key text and `typeof(T)`, so matching only the text is not enough to retrieve a value of another type.

The constructor rejects `null`, empty, and whitespace-only key text by throwing `ArgumentException`. The key text is otherwise stored exactly as supplied.

`ValueType` returns `typeof(T)` for callers that need the runtime value contract. `ToString()` formats the key as the full name of `T` followed by the key text, which is also used in missing-value exception messages from `Theme.Get<T>`.

`ThemeTokenBridge.ToToken<T>` uses `Key` to create aspect token names in the `theme.` namespace.

## Constructors

| Name | Description |
| --- | --- |
| `ThemeKey<T>(string key)` | Initializes a typed theme key from non-empty key text. Throws `ArgumentException` when `key` is `null`, empty, or whitespace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `string` | Gets the theme key text supplied to the constructor. |
| `ValueType` | `Type` | Gets the runtime type represented by the generic type parameter `T`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns a diagnostic string in the form `<full value type name>:<key>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ThemeKey<T>(string key)` | `ArgumentException` | `key` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI theming APIs that store, resolve, or project typed theme values.

## See also

- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeProvider`
- `Cerneala.UI.Theming.ThemeResource<T>`
- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Aspect.ThemeTokenBridge`
