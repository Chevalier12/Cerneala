# CursorService Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/CursorService.cs`

Resolves the cursor requested for the retained UI element under a pointer position.

```csharp
public sealed class CursorService
```

Inheritance:
`Object` -> `CursorService`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

UIRoot root = new(100, 100);
UIElement target = new();
target.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
root.VisualChildren.Add(target);

CursorService cursors = new();
cursors.SetCursor(target, Cursor.Hand);

Cursor overTarget = cursors.Resolve(root, 5, 5);   // Cursor.Hand
Cursor outsideTarget = cursors.Resolve(root, 50, 50); // Cursor.Arrow
```

## Remarks

`CursorService` performs hit testing against the root's current retained input route map, then walks from the hit element up through `VisualParent`. For each element in that chain, it first checks a cursor assigned through `SetCursor`, then checks the element's `UIElement.Cursor` property.

If no hit element or ancestor provides a cursor, `Resolve` returns `Cursor.Arrow`. The resolver calls `UIRoot.InputCache.EnsureCurrent(root)` before hit testing, so cursor lookup uses the current retained input cache and can rebuild it when it is dirty.

`UiHost` uses this service after input dispatch to publish a platform cursor through `Cerneala.UI.Platform.ICursorService` when platform cursor services are available.

## Constructors

| Name | Description |
| --- | --- |
| `CursorService(HitTestService? hitTestService = null)` | Initializes a cursor resolver. When `hitTestService` is `null`, a new `HitTestService` is created. |

## Methods

| Name | Description |
| --- | --- |
| `SetCursor(UIElement element, Cursor cursor)` | Associates `cursor` with `element` for this service instance. Throws `ArgumentNullException` when `element` is `null`. |
| `Resolve(UIRoot root, float x, float y)` | Returns the cursor for the element hit at logical coordinates `x`, `y`, using ancestor fallback and `Cursor.Arrow` as the default. Throws `ArgumentNullException` when `root` is `null`. |

## Applies to

Cerneala retained UI input, hit testing, and platform cursor publishing.

## See also

- `Cerneala.UI.Input.Cursor`
- `Cerneala.UI.Input.HitTestService`
- `Cerneala.UI.Elements.UIElement.Cursor`
- `Cerneala.UI.Platform.ICursorService`
