# PanelChildCollection Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/PanelChildCollection.cs`

Represents the children of a `Panel`: one call adds or removes a child in the visual tree and the logical tree together.

```csharp
public sealed class PanelChildCollection : IReadOnlyList<UIElement>
```

Implements:
`IReadOnlyList<UIElement>`, `IReadOnlyCollection<UIElement>`, `IEnumerable<UIElement>`, `IEnumerable`

## Examples

Build a `StackPanel` in code, as WPF code does with `stackPanel.Children.Add(child)`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

StackPanel panel = new();
Border child = new() { Width = 50, Height = 20 };

panel.Children.Add(child);
panel.Measure(new MeasureContext(new LayoutSize(200, 200), default));
panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200), default));

// panel.DesiredSize is 50 x 20
// child.LogicalParent is panel, child.VisualParent is panel
// child.ArrangedBounds is new LayoutRect(75, 0, 50, 20)

bool removed = panel.Children.Remove(child);

// removed is true
// child.LogicalParent is null, child.VisualParent is null
```

## Remarks

`Panel.Children` is the API application code uses to add panel children. It follows WPF `Panel.Children`, whose `UIElementCollection` makes an added child a visual child and a logical child of the panel in one call. Cerneala's `UIElementCollection` stores one role only, so a panel exposes this separate collection over its two role collections.

`Add` and `Insert` first validate the visual insertion (index range, an element added to itself, an ancestor, a child that already has a visual parent, a different root), so a rejected child gets neither parent. Then the child is inserted into `LogicalChildren` and into `VisualChildren`. If either insertion throws after the child became a logical child of the panel, for example because a `LogicalChildren.Changed` handler throws, the child is removed from `LogicalChildren` again before the exception propagates, so the panel never keeps a logical-only child and a later `Add` can add the same child. The logical position follows the visual position: the child is placed after the logical children that come before it in `VisualChildren`.

`Remove` returns `false` when the child is not a visual child of the panel. Otherwise it removes the child from `VisualChildren`, and from `LogicalChildren` when the panel is also its logical parent, and returns `true`. A visual-only child that was added with `VisualChildren.Add` can also be removed with `Remove`.

`Count`, the indexer, and enumeration read `VisualChildren`, the children the panel measures, arranges, and draws.

A panel rejects `LogicalChildren.Add` and `LogicalChildren.Insert` for a child that is not already one of its visual children: that child would be a logical child the panel never lays out or draws. The `InvalidOperationException` message names `Panel.Children.Add`.

The same tree rules as `UIElementCollection` apply, including the Relay access checks and the attach and detach lifecycle of the child subtree.

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of visual children of the panel. |
| `this[int index]` | Gets the visual child at the specified index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(UIElement)` | Adds the child at the end of `VisualChildren`, and to `LogicalChildren`. |
| `Insert(int index, UIElement child)` | Inserts the child at `index` in `VisualChildren`, and into `LogicalChildren` at the matching position. Throws `ArgumentOutOfRangeException` when `index` is greater than `Count`. |
| `Remove(UIElement)` | Removes a visual child of the panel from both collections and returns whether it was a visual child. |
| `GetEnumerator()` | Returns an enumerator over `VisualChildren`. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator over `VisualChildren`. |

## Applies to

Cerneala retained UI layout panels: `Panel`, `StackPanel`, `Grid`, `Canvas`, `VirtualizingStackPanel`, and their `Cerneala.UI.Controls` counterparts.

## See also

- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Elements.UIElement`
