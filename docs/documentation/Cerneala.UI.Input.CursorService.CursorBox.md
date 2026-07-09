# CursorService.CursorBox Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CursorService.cs`

Stores a `Cursor` value as the reference-type payload used by `CursorService` for element-specific cursor overrides.

```csharp
private sealed class CursorBox
```

Inheritance:
`object` -> `CursorService.CursorBox`

Containing type:
`Cerneala.UI.Input.CursorService`

## Examples

`CursorBox` is private to `CursorService`; callers set cursor overrides through `CursorService.SetCursor` instead of constructing the nested type directly.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

static Cursor ResolveElementCursor(
    CursorService service,
    UIRoot root,
    UIElement element,
    float pointerX,
    float pointerY)
{
    service.SetCursor(element, Cursor.Hand);

    return service.Resolve(root, pointerX, pointerY);
}
```

## Remarks

`CursorBox` is an implementation detail. `CursorService` stores cursor overrides in a `ConditionalWeakTable<UIElement, CursorBox>`, and this nested class wraps the `Cursor` struct in a reference type suitable for that table.

The stored cursor is immutable after construction. `CursorService.SetCursor` replaces any previous box for the element, and `CursorService.Resolve` reads the box while walking from the hit element up through its visual parents.

Because the type is private, application code should use `CursorService`, `UIElement.Cursor`, and `Cursor` rather than depending on `CursorBox`.

## Constructors

| Name | Description |
| --- | --- |
| `CursorBox(Cursor)` | Initializes a cursor box with the cursor value stored by the `Cursor` property. |

## Properties

| Name | Description |
| --- | --- |
| `Cursor` | Gets the cursor value captured when the box was constructed. |

## Applies to

Cerneala retained UI input cursor resolution.

## See also

- `Cerneala.UI.Input.CursorService`
- `Cerneala.UI.Input.Cursor`
- `Cerneala.UI.Elements.UIElement`
