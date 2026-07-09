# UIRoot.ThemeChangedSubscription Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/UIRoot.cs`

Maintains the private `ThemeProvider.ThemeChanged` event subscription used by `UIRoot` to invalidate aspect state when the active theme changes.

```csharp
private sealed class ThemeChangedSubscription : IDisposable
```

Containing type:
`UIRoot`

Inheritance:
`object` -> `UIRoot.ThemeChangedSubscription`

Implements:
`IDisposable`

## Examples

`ThemeChangedSubscription` is a private nested implementation detail. Application code uses it indirectly by assigning a theme provider to the root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

UIRoot root = new();
ThemeProvider provider = new(DefaultTheme.Create());

root.SetThemeProvider(provider);

// Replacing the provider's theme raises ThemeProvider.ThemeChanged.
// The root-owned subscription invalidates aspect state for the subtree.
provider.Theme = DefaultTheme.Create();
```

## Remarks

`UIRoot.SetThemeProvider` creates a `ThemeChangedSubscription` when a non-null `ThemeProvider` is assigned. Before replacing the provider, the root disposes the previous subscription and clears its stored reference.

The subscription stores the root as a `WeakReference<UIRoot>` and stores the subscribed provider strongly. When `ThemeProvider.ThemeChanged` is raised, the handler tries to recover the root. If the root is still alive, it calls the root's theme invalidation path, which invalidates `InvalidationFlags.Aspect | InvalidationFlags.Subtree` with the reason `Theme changed`.

If the weak root reference can no longer be resolved, the event handler disposes the subscription. Disposal unsubscribes from `ThemeProvider.ThemeChanged` and is idempotent.

Because the class is private, callers outside `UIRoot` cannot construct it or call `Dispose` directly. Public theme-provider changes should go through `UIRoot.SetThemeProvider`.

## Constructors

| Name | Description |
| --- | --- |
| `ThemeChangedSubscription(UIRoot root, ThemeProvider provider)` | Stores a weak reference to `root`, stores `provider`, and subscribes to `provider.ThemeChanged`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Unsubscribes from `ThemeProvider.ThemeChanged` once; later calls return without changing state. |

## Applies to

Cerneala retained UI runtime.

## See Also

- `UI/Elements/UIRoot.cs`
- `UI/Theming/ThemeProvider.cs`
- `Cerneala.UI.Elements.UIRoot`
