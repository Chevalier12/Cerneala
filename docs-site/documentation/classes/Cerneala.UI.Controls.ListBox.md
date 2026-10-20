# ListBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBox.cs`

Displays an item list with single selection behavior and `ListBoxItem` containers.

```csharp
public class ListBox : Selector
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ListBox`

## Examples

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new();
listBox.SetItems(new[] { "Draft", "Review", "Published" });

listBox.SelectedIndex = 1;

object? selected = listBox.SelectedItem;
```

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new()
{
    ItemsSource = new[] { "One", "Two", "Three" }
};
```

Each row shows its item through `ItemTemplate`. Here the rows read `[Alpha]` and `[Beta]`, and each row's `Content` stays the item string.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ListBox listBox = new()
{
    ItemsSource = new List<string> { "Alpha", "Beta" },
    ItemTemplate = new ContentTemplate<string>("Bracketed", key: null, priority: 0, context =>
        new TextBlock { Text = "[" + context.Data + "]" })
};
```

`Background` fills the whole list area behind the rows, and `BorderBrush` draws a border around it. Here a list arranged at `(3, 4, 40, 30)` draws one red `FillRectangle` at `(3, 4, 40, 30)` first, then one blue `DrawRectangle` (thickness 2) at `(3, 4, 40, 30)`, then the row of `One` on top of them. The row is arranged at `x = 15`, `y = 16` with width 16: 2 + 10 = 12 pixels inside each edge.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

ListBox listBox = new()
{
    Background = new SolidColorBrush(Color.Red),
    BorderBrush = new SolidColorBrush(Color.Blue),
    BorderThickness = new Thickness(2),
    Padding = new Thickness(10)
};
listBox.Items.Add(new ListBoxItem { Content = "One" });
```

## Remarks

`ListBox` is a concrete `Selector` that configures item presentation for a vertical list. Its constructor assigns a retained vertical `StackPanel` directly to `ItemsPanel`.

Each data item is wrapped in a `ListBoxItem` container. If an item is already a `ListBoxItem`, that same element is used as its own container. Selection is inherited from `Selector`: setting `SelectedIndex` updates the selection model, and a left mouse button release on a prepared item container selects that item. A release on a button in the row, such as a `CheckBox` from the `ItemTemplate`, that completes the button's click only clicks that button and does not select the row. A release on the button after a press elsewhere in the row completes no click, so it selects the row and leaves the button unchanged (see `Selector`).

A click selects a row only when the row can take the keyboard focus, as WPF's `ListBoxItem.OnMouseLeftButtonDown` notifies the `ListBox` only when the item's `Focus()` succeeds. A row can take the focus when it is attached, `Focusable`, enabled, and visible. This is decided at the left press, before the press moves the focus, and the release of that press uses that answer: a row that a `GotFocus` handler sets to `Focusable = false` during the press is still selected by the release. A release without a press on the row decides at the release. A click on a row whose `Focusable` is `false` leaves `SelectedIndex`, `SelectedItem`, and every row's `IsSelected` unchanged and raises no `SelectionChanged`. The release is still handled, so an enclosing `Button` is not clicked by it. For example, with rows `a` (`Focusable = false`) and `b`, and `SelectedIndex = 1`, a click on `a` leaves `SelectedIndex` at `1` and `a.IsSelected` at `false`; a click on `b` (focusable, the default) selects and focuses `b`. `ComboBox` does not follow this rule: a click on a `ComboBoxItem`, which is not focusable, still selects it and closes the drop-down, as WPF's `ComboBoxItem` does.

A left press on a row moves keyboard focus to that row, because `ListBoxItem` is focusable, as in WPF. For example, a `Button` has the focus and the user clicks the first row: the row's `IsKeyboardFocused` becomes `true`, the `ListBox`'s `IsKeyboardFocusWithin` becomes `true`, and a Space press after the click does not click the `Button`. The `ListBox` itself does not take the focus from a press on a row, also when its `Focusable` is `true`: a row whose `Focusable` is `false` leaves the focus where it was. A press on a focusable element inside the row, such as a `TextBox` from the `ItemTemplate`, focuses that element. See [`ListBoxItem`](Cerneala.UI.Controls.ListBoxItem.md#keyboard-focus-on-a-click).

`ListBoxItem` receives `ItemIndex`, `Item`, and `IsSelected` during container preparation. When a selected realized container is invalidated, the container render and input visual state are refreshed.

Row content follows `ItemsControl`: without `ItemTemplate`, a row's `Content` is the item's `DisplayMemberPath` value (the item itself when the path is empty); with `ItemTemplate`, it is the item. The row shows a `string` value as text, and `ListBox` passes `ItemTemplate`, `ItemTemplateKey`, and `Templates` to the row, so a template creates the row's visual. For example, `SetItems(new[] { "Apple" })` creates a row whose `Content` is `"Apple"` and whose text reads `Apple`; the row is as tall as that text. See [`ListBoxItem`](Cerneala.UI.Controls.ListBoxItem.md#content-presentation) for every content kind. An item that is already a `ListBoxItem` is its own row and does not use the item template. Any other `UIElement` item is wrapped in a `ListBoxItem` row: with `ItemTemplate`, the row shows the element that the template creates for it (the item is `context.Data` and its index is `context.Index`), as in WPF; without `ItemTemplate`, the row shows the element that a template of `Templates` creates for it when one matches the element and `ItemTemplateKey`, and otherwise hosts the element directly.

As the `Border` of the WPF `ListBox` template, `Background` fills the list's arranged bounds, `BorderBrush` draws a border around them (as `Border` does, with the largest side of `BorderThickness` as its thickness), and the rows draw on top. Without `Background` (the default, `null`) the list draws no fill, so the area behind it shows through; without `BorderBrush` or with a zero `BorderThickness` it draws no border. The rows are laid out inside `BorderThickness + Padding`, as WPF gives `Padding` to the `ScrollViewer` inside that `Border`, and `DesiredSize` includes both. With the defaults (`Thickness.Zero`) the rows touch the list edges. A `ListBox` with a `ComponentTemplate` draws no chrome and does not inset its template; the template draws its own chrome. This behavior comes from `ItemsControl`. `Background` and `BorderBrush` are registered with `AffectsRender`, so changing them redraws the list without a new measure or arrange; `BorderThickness` and `Padding` also affect measure.

## Constructors

| Name | Description |
| --- | --- |
| `ListBox()` | Initializes a new list box and sets the default items panel to a `StackPanel`. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Inherited from `Selector`. Identifies the `SelectedIndex` UI property. |
| `ItemTemplateProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplate` UI property. |
| `ItemsPanelProperty` | Inherited from `ItemsControl`. Identifies the `ItemsPanel` UI property. |
| `ItemTemplateKeyProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplateKey` UI property. |
| `ItemsSourceProperty` | Inherited from `ItemsControl`. Identifies the `ItemsSource` UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndex` | `int` | Inherited from `Selector`. Gets or selects the current item index. The default is `-1`; values below `-1` are rejected by the selection model/property validation. |
| `SelectedItem` | `object?` | Inherited from `Selector`. Gets the item at `SelectedIndex`, or `null` when the selected index is outside the current item range. |
| `SelectionModel` | `SelectionModel` | Inherited from `Selector`. Gets the single-selection model used by the selector. |
| `Items` | `ItemCollection` | Inherited from `ItemsControl`. Gets the local item collection used when `ItemsSource` is not set. |
| `ItemsSource` | `IEnumerable?` | Inherited from `ItemsControl`. Gets or sets an external item source. |
| `ItemCount` | `int` | Inherited from `ItemsControl`. Gets the count of the `ItemsSource` snapshot when `ItemsSource` is set; otherwise the count of `Items`. |
| `ItemTemplate` | `ContentTemplate?` | Inherited from `ItemsControl`. Gets or sets the template used to display non-container items. `ListBox` applies it inside each `ListBoxItem` row. |
| `ItemTemplateKey` | `string?` | Inherited from `ItemsControl`. Gets or sets the template key used by content presentation. |
| `ItemsPanel` | `Panel?` | Inherited from `ItemsControl`. Gets or sets the retained panel used by the items presenter. `ListBox` initializes it to a vertical `StackPanel`. |
| `Background` | `Brush?` | Inherited from `Control`. Gets or sets the brush that fills the list's arranged bounds behind its rows. The default is `null` (no fill). Ignored when a `ComponentTemplate` is set. |
| `BorderBrush` | `Brush?` | Inherited from `Control`. Gets or sets the brush of the border around the list's arranged bounds. The default is `null` (no border). Ignored when a `ComponentTemplate` is set. |
| `BorderThickness` | `Thickness` | Inherited from `Control`. Gets or sets the border width; the rows are inset by it. The default is `Thickness.Zero`. Ignored when a `ComponentTemplate` is set. |
| `Padding` | `Thickness` | Inherited from `Control`. Gets or sets the space between the border and the rows. The default is `Thickness.Zero`. Ignored when a `ComponentTemplate` is set. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>` | Inherited from `Selector`. Bubbling routed event raised after the selected index changes. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable? items)` | Inherited from `ItemsControl`. Replaces the local `Items` collection. |
| `GetItemAt(int index)` | Inherited from `ItemsControl`. Gets an item from the `ItemsSource` snapshot when `ItemsSource` is set; otherwise from `Items`. |

## Protected Members

| Name | Description |
| --- | --- |
| `DefaultContainerType` | Returns `typeof(ListBoxItem)`. |
| `GetContainerTypeForItem(object? item)` | Returns the item's own type when the item is a `ListBoxItem`; otherwise `typeof(ListBoxItem)`. |
| `CreateItemContainer(int index, object? item)` | Returns the item itself when it is a `ListBoxItem`; otherwise a new `ListBoxItem`. |
| `PrepareItemContent(UIElement container, int index, object? item)` | Passes `ItemTemplate`, `ItemTemplateKey`, `Templates`, and the item index to a `ListBoxItem` row, then sets its `Content` as `ItemsControl` does. |
| `ClearItemContainer(UIElement container)` | Clears the container as `ItemsControl` does, then removes the item template from a `ListBoxItem` row. |
| `OnRender(RenderContext context)` | Inherited from `ItemsControl`. Without a `ComponentTemplate`, fills the arranged bounds with `Background` and draws `BorderBrush` around them when they are set and the bounds are not empty; with a `ComponentTemplate`, draws nothing. |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.ListBoxItem`
