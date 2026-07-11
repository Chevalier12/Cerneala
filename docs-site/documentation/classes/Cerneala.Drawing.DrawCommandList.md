# DrawCommandList Class

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawCommandList.cs`

Stores an ordered mutable list of drawing commands.

```csharp
public sealed class DrawCommandList : IReadOnlyList<DrawCommand>
```

Inheritance:
`object` -> `DrawCommandList`

Implements:
`IReadOnlyList<DrawCommand>`, `IReadOnlyCollection<DrawCommand>`, `IEnumerable<DrawCommand>`, `IEnumerable`

## Examples

```csharp
using Cerneala.Drawing;

DrawCommandList commands = new();

commands.Add(DrawCommand.FillRectangle(
    new DrawRect(0, 0, 100, 40),
    Color.White));

commands.Add(DrawCommand.DrawRectangle(
    new DrawRect(0, 0, 100, 40),
    Color.Black,
    thickness: 1));

DrawCommand first = commands[0];
int count = commands.Count;

commands.Clear();
```

## Remarks

`DrawCommandList` preserves commands in insertion order. `DrawingContext` appends drawing operations to a `DrawCommandList`, and render backends receive the list through `IDrawingBackend.Render`.

The list is mutable through `Add` and `Clear`, but consumers that receive it as an `IReadOnlyList<DrawCommand>` can inspect the command count, index commands, and enumerate commands without additional copying.

Render backends should treat submitted command lists as read-only while rendering. Retained rendering may reuse the same command-list instance across unchanged draw frames.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandList()` | Initializes an empty command list. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of stored commands. |
| `this[int index]` | Gets the command at the specified zero-based index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(DrawCommand)` | Appends a command to the end of the list. |
| `Clear()` | Removes all commands from the list. |
| `GetEnumerator()` | Returns an enumerator over the stored commands in insertion order. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns the non-generic enumerator for the command list. |

## Applies to

Cerneala retained rendering and drawing command submission.

## See also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawingContext`
- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.UI.Rendering.RetainedRenderer`
