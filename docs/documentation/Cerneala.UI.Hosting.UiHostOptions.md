# UiHostOptions Class

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/UiHostOptions.cs`

Provides construction options for `UiHost`.

```csharp
public sealed class UiHostOptions
```

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;

UIRoot root = new();

UiHost host = new(new UiHostOptions
{
    Root = root,
    Viewport = new UiViewport(800, 600)
});
```

## Remarks

`UiHostOptions` is a simple option bag used by the `UiHost` constructor. If no options instance is supplied to `UiHost`, the host creates a default `UiHostOptions` instance internally.

The options set the initial retained root, viewport, input source, drawing/input backend, clock, input bridge, and platform services for a host. `UiHost` copies these values during construction. When `InputBridge` is not supplied, `UiHost` creates a new `ElementInputBridge`.

When a root is supplied at construction time, `UiHost` applies the configured platform services and viewport to that root. The default viewport is `new UiViewport(0, 0)`, which uses the `UiViewport` default scale of `1`.

## Constructors

| Name | Description |
| --- | --- |
| `UiHostOptions()` | Initializes a new options instance with default values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Root` | `UIRoot?` | Gets or sets the optional initial retained UI root. |
| `Viewport` | `UiViewport` | Gets or sets the initial viewport. Defaults to `new UiViewport(0, 0)`. |
| `InputSource` | `IInputSource?` | Gets or sets the optional input source used by `UiHost.Update` when no explicit input frame is supplied. |
| `Backend` | `IUiBackend?` | Gets or sets the optional backend that can provide an input source for update and a drawing backend for draw. |
| `Clock` | `IUiClock?` | Gets or sets the optional clock used to provide elapsed frame time. |
| `InputBridge` | `ElementInputBridge?` | Gets or sets the optional input bridge. If omitted, `UiHost` creates a new `ElementInputBridge`. |
| `PlatformServices` | `IPlatformServices?` | Gets or sets optional platform services applied to the hosted root. |

## Applies to

Cerneala retained UI hosting.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.UiViewport`
- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions`
