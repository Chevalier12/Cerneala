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

Rendering participation requires both `UIElement.IsVisible` and `UIElement.Visibility == Visibility.Visible`. A hidden element still takes layout space, but it is not rendered.

Input and hit-test participation use the same public visibility checks as rendering and also require that the element is not in the internal presence-exiting state. Disabled elements are not filtered by this class; callers such as hit testing and focus policy apply `IsEnabled` separately when that matters.

All methods validate the `element` argument with `ArgumentNullException.ThrowIfNull`.

## Methods
| Name | Description |
| --- | --- |
| `ParticipatesInLayout(UIElement)` | Returns `true` when `element.Visibility` is not `Visibility.Collapsed`. |
| `ParticipatesInRendering(UIElement)` | Returns `true` when `element.IsVisible` is `true` and `element.Visibility` is `Visibility.Visible`. |
| `ParticipatesInInput(UIElement)` | Returns `true` when the element is not presence-exiting, `IsVisible` is `true`, and `Visibility` is `Visible`. |
| `ParticipatesInHitTest(UIElement)` | Returns `true` when the element is not presence-exiting, `IsVisible` is `true`, and `Visibility` is `Visible`. |

## Participation Rules
| Element state | Layout | Rendering | Input route | Hit testing |
| --- | --- | --- | --- | --- |
| `IsVisible = true`, `Visibility = Visible` | Yes | Yes | Yes | Yes |
| `IsVisible = true`, `Visibility = Hidden` | Yes | No | No | No |
| `IsVisible = true`, `Visibility = Collapsed` | No | No | No | No |
| `IsVisible = false`, `Visibility = Visible` | Yes | No | No | No |
| Presence-exiting element | Based on `Visibility` | Based on `IsVisible` and `Visibility` | No | No |

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
- `tests/Cerneala.Tests/UI/Layout/VisibilityCombinationTests.cs`
