# ThemeResource<T> Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemeResource.cs`

Stores a typed theme key and resolves its value from a `ThemeProvider`.

```csharp
public sealed class ThemeResource<T>
```

Inheritance:
`object` -> `ThemeResource<T>`

## Examples

Resolve a value from the default theme:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemeProvider provider = new(DefaultTheme.Create());
ThemeResource<Color> accentResource = new(DefaultTheme.AccentKey);

Color accent = accentResource.Resolve(provider);
```

Use a custom typed key:

```csharp
using Cerneala.UI.Theming;

ThemeKey<double> spacingKey = new("Spacing.Small");
Theme theme = new Theme("Custom").Set(spacingKey, 4.0);
ThemeProvider provider = new(theme);

ThemeResource<double> spacingResource = new(spacingKey);
double spacing = spacingResource.Resolve(provider);
```

## Remarks

`ThemeResource<T>` is a small typed indirection around `ThemeKey<T>`. It keeps the key in `Key` and uses `ThemeProvider.Get<T>` when `Resolve` is called.

`Resolve` requires a non-null provider. Passing `null` throws `InvalidOperationException` with the resource key in the message. If the provider is present but the active theme does not contain the key, `Resolve` propagates the lookup failure from `ThemeProvider.Get<T>`.

The generic type parameter is part of the lookup identity because `ThemeKey<T>` and `Theme` store entries by both key text and value type.

## Constructors

| Name | Description |
| --- | --- |
| `ThemeResource<T>(ThemeKey<T> key)` | Initializes a theme resource for the supplied typed theme key. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `ThemeKey<T>` | Gets the typed theme key used when resolving the resource. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve(ThemeProvider? provider)` | `T` | Resolves the typed value from `provider`, or throws when no provider is supplied or the theme value is missing. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Resolve(ThemeProvider? provider)` | `InvalidOperationException` | `provider` is `null`. |
| `Resolve(ThemeProvider? provider)` | `KeyNotFoundException` | The provider's active theme does not contain the resource key. |

## Applies to

Cerneala UI theming and code that stores typed references to values resolved from a `ThemeProvider`.

## See also

- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Theming.ThemeProvider`
- `Cerneala.UI.Theming.DefaultTheme`
