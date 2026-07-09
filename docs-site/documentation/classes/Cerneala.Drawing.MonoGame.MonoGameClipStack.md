# MonoGameClipStack Class

## Definition

Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/MonoGameClipStack.cs`

Tracks nested MonoGame scissor rectangles by intersecting pushed clips with the current clip and restoring previous clips on pop.

```csharp
internal sealed class MonoGameClipStack
```

Inheritance:
`object` -> `MonoGameClipStack`

## Examples

```csharp
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;

Rectangle viewport = new(0, 0, 100, 100);
MonoGameClipStack stack = new(viewport);

stack.Push(new Rectangle(10, 10, 80, 80));
stack.Push(new Rectangle(20, 20, 10, 10));

Rectangle activeClip = stack.CurrentClip; // (20, 20, 10, 10)

stack.Pop();
Rectangle restoredClip = stack.CurrentClip; // (10, 10, 80, 80)

stack.Reset();
Rectangle viewportClip = stack.CurrentClip; // (0, 0, 100, 100)
```

## Remarks

`MonoGameClipStack` is an internal helper used by `MonoGameDrawingBackend` to manage `GraphicsDevice.ScissorRectangle` while rendering `PushClip` and `PopClip` draw commands.

The stack starts with an initial clip, normally the current MonoGame viewport bounds. Each `Push` stores the current clip and replaces it with the intersection of the current clip and the requested rectangle. If the rectangles do not overlap, the active clip becomes an empty rectangle at `(0, 0, 0, 0)`.

`Pop` restores the previous clip when one exists. Calling `Pop` on an empty stack leaves `CurrentClip` unchanged and returns it. `Reset` clears all pushed clips and restores the initial clip.

`MonoGameDrawingBackend.Render` creates a fresh clip stack from the graphics viewport before rendering commands and resets it in a `finally` block while restoring the previous MonoGame scissor rectangle.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameClipStack(Rectangle initialClip)` | Creates a clip stack whose initial and current clip are `initialClip`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CurrentClip` | `Rectangle` | Gets the active clip rectangle. It starts as the initial clip and changes after `Push`, `Pop`, or `Reset`. |
| `Depth` | `int` | Gets the number of previous clips stored on the stack. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Push(Rectangle requestedClip)` | `void` | Saves the current clip and makes `CurrentClip` the intersection of the current clip and `requestedClip`. |
| `Pop()` | `Rectangle` | Restores and returns the previous clip. If there is no previous clip, returns the current clip without changing the stack. |
| `Reset()` | `void` | Clears stored clips and restores `CurrentClip` to the initial clip passed to the constructor. |

## Internal Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Intersect(Rectangle first, Rectangle second)` | `Rectangle` | Returns the overlapping rectangle between `first` and `second`, or `(0, 0, 0, 0)` when they do not overlap. |

## Applies to

Cerneala MonoGame drawing backend clipping.

## See also

- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawCommandList`
