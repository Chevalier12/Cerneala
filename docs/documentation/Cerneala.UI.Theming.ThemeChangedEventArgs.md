# ThemeChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemeProvider.cs`

Provides data for the `ThemeProvider.ThemeChanged` event.

```csharp
public sealed class ThemeChangedEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `ThemeChangedEventArgs`

## Examples

```csharp
using Cerneala.UI.Theming;

ThemeProvider provider = new(DefaultTheme.Create());

provider.ThemeChanged += (_, args) =>
{
    Theme oldTheme = args.OldTheme;
    Theme newTheme = args.NewTheme;

    Console.WriteLine($"Theme changed from {oldTheme.Name} to {newTheme.Name}");
};

provider.Theme = new Theme("High contrast");
```

## Remarks

`ThemeChangedEventArgs` captures the previous and replacement `Theme` instances supplied by `ThemeProvider` after its `Theme` property is set to a different theme instance.

The constructor requires both `oldTheme` and `newTheme` to be non-null and throws `ArgumentNullException` for either null argument. `ThemeProvider` does not raise `ThemeChanged` when the `Theme` setter receives the same theme instance.

`UIRoot.SetThemeProvider` subscribes to `ThemeProvider.ThemeChanged`; when the event is raised, the root invalidates aspect state for the subtree so theme-backed values can be resolved again.

## Constructors

| Name | Description |
| --- | --- |
| `ThemeChangedEventArgs(Theme oldTheme, Theme newTheme)` | Initializes the event arguments with the previous and replacement themes. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OldTheme` | `Theme` | Gets the active theme before the change. |
| `NewTheme` | `Theme` | Gets the active theme after the change. |

## Applies to

Cerneala UI theming and root aspect invalidation triggered by theme replacement.

## See also

- `Cerneala.UI.Theming.ThemeProvider`
- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Elements.UIRoot`
