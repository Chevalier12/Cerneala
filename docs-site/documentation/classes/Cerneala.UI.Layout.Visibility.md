# Visibility Enum

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Visibility.cs`

Specifies whether an element is shown and whether it keeps its layout space.

```csharp
public enum Visibility
```

## Examples

A vertical stack panel with a 40 x 20 child above a 60 x 30 child:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

UIRoot root = new(300, 300);
StackPanel panel = new()
{
    HorizontalAlignment = HorizontalAlignment.Left,
    VerticalAlignment = VerticalAlignment.Top
};
UIElement first = new() { Width = 40, Height = 20 };
UIElement second = new() { Width = 60, Height = 30 };
panel.VisualChildren.Add(first);
panel.VisualChildren.Add(second);
root.VisualChildren.Add(panel);

first.Visibility = Visibility.Hidden;
root.ProcessFrame();
Console.WriteLine($"{panel.DesiredSize.Height} {second.ArrangedBounds.Y}");
// 50 20

first.Visibility = Visibility.Collapsed;
root.ProcessFrame();
Console.WriteLine($"{panel.DesiredSize.Height} {second.ArrangedBounds.Y}");
// 30 0
```

`Hidden` keeps the 20 pixels of space; `Collapsed` removes them.

## Remarks

`UIElement.Visibility` defaults to `Visible`. A change affects measure, arrange, render, hit testing, and semantics.

| Value | Layout | Render | Input and hit testing | Accessibility |
| --- | --- | --- | --- | --- |
| `Visible` | Measured and arranged. | Drawn, if `IsVisible` is also `true`. | Included, if `IsVisible` is `true`. | Included. |
| `Hidden` | Measured and arranged normally; keeps its space. | Not drawn, and neither are its descendants. | Excluded, with its descendants. | Excluded, with its descendants. |
| `Collapsed` | Measures as zero size and is arranged in a zero-size rectangle; takes no space. | Not drawn, and neither are its descendants. | Excluded, with its descendants. | Excluded, with its descendants. |

`UIElementVisibility` exposes these rules: `ParticipatesInLayout` is `false` only for `Collapsed`; `ParticipatesInRendering` and `ParticipatesInInput` need `Visible` and `IsVisible`; `ParticipatesInHitTest` also needs `IsHitTestVisible`.

When an element stops being visible, the motion running on its subtree is cancelled. Entering or leaving `Collapsed` also invalidates the parent's layout.

The property has no validator. A value outside the enum is accepted; it takes part in layout but is not drawn, like `Hidden`.

In `.crn` markup the value is the member name, matched without regard to case, for example `Visibility="Collapsed"`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Visible` | `0` | Shown, and takes part in layout. |
| `Hidden` | `1` | Not shown, but keeps its layout space. |
| `Collapsed` | `2` | Not shown, and takes no layout space. |

## Applies to

`UIElement.Visibility`.

## See also

- `Cerneala.UI.Elements.UIElementVisibility`
- `Cerneala.UI.Elements.UIElement`
