# UIElementVisibility Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/UIElementVisibility.cs`

Provides static predicates that centralize how a `UIElement` participates in layout, rendering, input routing, and hit testing.

```csharp
public static class UIElementVisibility
```

## Examples
Check the visibility participation mode before doing work that should match the retained UI pipeline.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIElement element = new()
{
    IsVisible = true,
    Visibility = Visibility.Hidden
};

bool inLayout = UIElementVisibility.ParticipatesInLayout(element);
bool inRendering = UIElementVisibility.ParticipatesInRendering(element);
bool inInput = UIElementVisibility.ParticipatesInInput(element);
bool inHitTesting = UIElementVisibility.ParticipatesInHitTest(element);
```

For the element above, `inLayout` is `true`, while `inRendering`, `inInput`, and `inHitTesting` are `false`.

## Remarks
`UIElementVisibility` is the shared visibility gate used by layout, rendering, input route construction, focus policy, semantics, and hit testing code. It keeps the participation rules consistent instead of duplicating the same `UIElement` property checks throughout the UI pipeline.

Layout participation depends only on `UIElement.Visibility`. Elements with `Visibility.Collapsed` do not participate in layout; elements with `Visibility.Visible` or `Visibility.Hidden` do.

Rendering participation requires both `UIElement.IsVisible` and `UIElement.Visibility == Visibility.Visible`, and that no `VirtualizingStackPanel` currently skips the element. A hidden element still takes layout space, but it is not rendered.

A `VirtualizingStackPanel` with a `VirtualizationContext` or an automatic viewport skips each child whose item index lies outside its `RealizationWindow`. The skipped child stays in `VisualChildren` and is arranged into zero-sized bounds, but it does not participate in rendering, input, or hit testing until an arrange of the panel realizes it again. For example, after `panel.UpdateViewport(new ItemsVirtualizationViewport(10, 100, 0, 0))` with ten children of `Height = 20`, `ParticipatesInRendering(panel.VisualChildren[7])` returns `false`; after the viewport moves to offset 100 and the panel is arranged again, it returns `true`, and `panel.VisualChildren[0]` returns `false`. Removing the child from the panel ends the skipped state. A skipped child that sets `UIElement.Presence` is detached at once when it is removed, without an exit animation, so it is never drawn at its zero-sized bounds.

Input participation uses the same visibility checks as rendering and also requires that the element is not playing a presence exit whose `PresenceOptions.ExcludeInputWhileExiting` is `true` (the default). An element that exits with `ExcludeInputWhileExiting = false` keeps its input participation. Hit-test participation additionally requires `IsHitTestVisible`. Disabled elements are not filtered by this class; callers such as hit testing and focus policy apply `IsEnabled` separately when that matters.

All methods validate the `element` argument with `ArgumentNullException.ThrowIfNull`.

## Methods
| Name | Description |
| --- | --- |
| `ParticipatesInLayout(UIElement)` | Returns `true` when `element.Visibility` is not `Visibility.Collapsed`. |
| `ParticipatesInRendering(UIElement)` | Returns `true` when `element.IsVisible` is `true`, `element.Visibility` is `Visibility.Visible`, and no `VirtualizingStackPanel` skips the element. |
| `ParticipatesInInput(UIElement)` | Returns `true` when the element is not exiting with input excluded and `ParticipatesInRendering` returns `true`. |
| `ParticipatesInHitTest(UIElement)` | Returns `true` when the element is not exiting with input excluded, `IsHitTestVisible` is `true`, and `ParticipatesInRendering` returns `true`. |

## Participation Rules
| Element state | Layout | Rendering | Input route | Hit testing |
| --- | --- | --- | --- | --- |
| `IsVisible = true`, `Visibility = Visible` | Yes | Yes | Yes | Yes |
| `IsVisible = true`, `Visibility = Hidden` | Yes | No | No | No |
| `IsVisible = true`, `Visibility = Collapsed` | No | No | No | No |
| `IsVisible = false`, `Visibility = Visible` | Yes | No | No | No |
| `IsHitTestVisible = false`, otherwise visible | Yes | Yes | Yes | No |
| Presence-exiting element, `ExcludeInputWhileExiting = true` (default) | Based on `Visibility` | Based on `IsVisible` and `Visibility` | No | No |
| Presence-exiting element, `ExcludeInputWhileExiting = false` | Based on `Visibility` | Based on `IsVisible` and `Visibility` | Based on `IsVisible` and `Visibility` | Based on `IsVisible`, `Visibility` and `IsHitTestVisible` |
| Child skipped by a `VirtualizingStackPanel`, otherwise visible | Yes, arranged into zero-sized bounds | No | No | No |

## Exceptions
| Method | Exception | Condition |
| --- | --- | --- |
| `ParticipatesInLayout` | `ArgumentNullException` | `element` is `null`. |
| `ParticipatesInRendering` | `ArgumentNullException` | `element` is `null`. |
| `ParticipatesInInput` | `ArgumentNullException` | `element` is `null`. |
| `ParticipatesInHitTest` | `ArgumentNullException` | `element` is `null`. |

## Applies to
Cerneala retained UI elements in the `Cerneala` project.

## See also
- `UI/Elements/UIElement.cs`
- `UI/Layout/Visibility.cs`
- `UI/Rendering/DrawCommandListBuilder.cs`
- `UI/Input/HitTestService.cs`
- `UI/Layout/Panels/VirtualizingStackPanel.cs`
- `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`
- `tests/Cerneala.Tests/UI/Layout/VirtualizingStackPanelTests.cs`
