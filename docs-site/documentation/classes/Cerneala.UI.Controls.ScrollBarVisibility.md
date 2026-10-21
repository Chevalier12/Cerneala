# ScrollBarVisibility Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ScrollBarVisibility.cs`

Specifies whether a `ScrollViewer` scroll bar is shown and whether the viewer can scroll in that direction.

```csharp
public enum ScrollBarVisibility
```

## Examples

```csharp
using Cerneala.UI.Controls;

ScrollViewer viewer = new()
{
    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
    Content = new StackPanel()
};
```

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
    <StackPanel />
</ScrollViewer>
```

## Remarks

`ScrollViewer` uses this enum for `HorizontalScrollBarVisibility` (default `Disabled`) and `VerticalScrollBarVisibility` (default `Auto`). Changing either property invalidates measure, arrange, and render.

`Disabled` is the only value that turns scrolling off. With `Auto`, `Hidden`, or `Visible`, the content is measured with infinite size in that direction and can scroll. With `Disabled`, the content is measured with the available size, the offset in that direction stays `0`, and for the vertical direction the mouse wheel does nothing.

For `Auto`, "content is larger" means the extent is strictly greater than the viewport. For example, content `300` high in a `300` high viewport does not show the vertical bar; content `301` high does.

`Hidden` collapses the bar (`Visibility.Collapsed`), as `Disabled` does, and as WPF's `ComputedHorizontalScrollBarVisibility` and `ComputedVerticalScrollBarVisibility` do: the bar is not drawn and takes no layout space, and the presenter still scrolls on that axis. For example, a `ScrollViewer` with `HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden`, `VerticalScrollBarVisibility = ScrollBarVisibility.Visible`, and a `100` x `100` slot arranges the presenter at `(0, 0, 88, 100)` and the vertical bar at `(88, 0, 12, 100)`: the hidden horizontal bar takes none of the `100` height, and `Presenter.CanHorizontallyScroll` is `true`. Only `Visible`, and `Auto` when the content is larger than the viewport, show a bar.

`TextBox` uses `Hidden` as the default of its `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility`.

## Values

| Name | Value | Scroll bar | Scrolling |
| --- | --- | --- | --- |
| `Disabled` | `0` | Never shown (`Visibility.Collapsed`). | Off. The offset stays `0`. |
| `Auto` | `1` | Shown only when the content is larger than the viewport; otherwise collapsed. | On. |
| `Hidden` | `2` | Never shown and takes no layout space (`Visibility.Collapsed`). | On. |
| `Visible` | `3` | Always shown. | On. |

## Applies to

`Cerneala.UI.Controls.ScrollViewer`.

## See also

- `Cerneala.UI.Controls.ScrollViewer`
- `Cerneala.UI.Controls.IScrollInfo`
- `Cerneala.UI.Controls.ScrollContentPresenter`
- `Cerneala.UI.Controls.TextBox`
