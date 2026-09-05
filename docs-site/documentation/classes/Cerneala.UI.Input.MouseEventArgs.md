# MouseEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/MouseEventArgs.cs`

Provides routed mouse event data and converts the unrounded root pointer position to coordinates relative to any element on the active input route.

```csharp
public class MouseEventArgs : RoutedEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `MouseEventArgs`

## Examples

Use `GetPosition` when scene or transformed element coordinates matter. The legacy `X` and `Y` values remain available for integer root coordinates.

```csharp
using System.Numerics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

void OnMouseMove(UIElement sceneNode, MouseEventArgs args)
{
    Vector2 localPosition = args.GetPosition(sceneNode);
    int legacyRootX = args.X;
    int legacyRootY = args.Y;
}
```

## Remarks

`MouseEventArgs` retains the raw floating-point root position supplied by the input dispatcher. `GetPosition` converts that position through the same visual, `RenderSurface2D` ViewBox, scene, and node transforms used by rendering and geometric hit testing. This avoids losing subpixel precision before a handler asks for surface-, scene-, or node-local coordinates.

`X` and `Y` are compatibility properties. Each is the corresponding raw root coordinate rounded to the nearest integer with `MathF.Round`; use `GetPosition` for geometry-sensitive work.

`GetPosition` throws `ArgumentNullException` when `relativeTo` is `null`. It throws `InvalidOperationException` when the requested element is detached from the active root or when any required transform is non-invertible. The method does not fabricate coordinates for either case.

Derived mouse event argument types can add more event-specific data while keeping the same coordinate properties.

## Constructors

| Name | Description |
| --- | --- |
| `MouseEventArgs(RoutedEvent, object, int, int)` | Initializes routed mouse event data for an event, original source, and coordinates. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `int` | Gets the raw root X coordinate rounded to the nearest integer. |
| `Y` | `int` | Gets the raw root Y coordinate rounded to the nearest integer. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `GetPosition(UIElement)` | `Vector2` | Returns the unrounded pointer position relative to the requested element, or throws when that conversion is unavailable. |

## Applies to

- `Cerneala.UI.Input.MouseEventArgs`

## See also

- `Cerneala.UI.Input.MouseButtonEventArgs`
- `Cerneala.UI.Input.MouseWheelEventArgs`
- `Cerneala.UI.Input.RoutedEventArgs`
