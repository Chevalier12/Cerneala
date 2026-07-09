# MonoGameUiHost.MonoGameUiBackend Class

## Definition
Namespace: `Cerneala.UI.Hosting.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/MonoGame/MonoGameUiHost.cs`

Provides the private `IUiBackend` adapter used by `MonoGameUiHost` to expose input and drawing services to the core UI host.

```csharp
private sealed class MonoGameUiBackend : IUiBackend
```

Containing type:
`MonoGameUiHost`

Inheritance:
`object` -> `MonoGameUiHost.MonoGameUiBackend`

Implements:
`IUiBackend`

## Examples

```csharp
IInputSource inputSource = options.InputSource ?? new MonoGameInputSource();
IDrawingBackend drawingBackend = new MonoGameDrawingBackend(
    spriteBatch,
    options.WhitePixel,
    contentServices.TextRasterizer);

IUiBackend backend = new MonoGameUiBackend(inputSource, drawingBackend);
```

## Remarks

`MonoGameUiBackend` is an implementation detail of `MonoGameUiHost`. It stores the `IInputSource` and `IDrawingBackend` instances that are passed to the core `UiHost` through `UiHostOptions.Backend`.

The constructor requires both services. Passing `null` for `inputSource` throws `ArgumentNullException` for `inputSource`; passing `null` for `drawingBackend` throws `ArgumentNullException` for `drawingBackend`.

Although `IUiBackend` allows nullable `InputSource` and `DrawingBackend` values, this implementation always exposes non-null instances after construction.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameUiBackend(IInputSource, IDrawingBackend)` | Initializes the backend adapter with the input source and drawing backend used by `UiHost`. |

## Properties

| Name | Description |
| --- | --- |
| `InputSource` | Gets the input source used to produce UI input frames. |
| `DrawingBackend` | Gets the drawing backend used to render retained draw command lists. |

## Applies to

Cerneala MonoGame UI hosting internals.

## See also

- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
- `Cerneala.UI.Hosting.IUiBackend`
- `Cerneala.UI.Input.IInputSource`
- `Cerneala.Drawing.IDrawingBackend`
