# ItemContainerGenerator.ItemContainerInfo Class

## Definition
Namespace: `Cerneala.UI.Controls.Items`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Items/ItemContainerGenerator.cs`

Stores the item index, source item, and selection state associated with a generated item container.

```csharp
private sealed class ItemContainerInfo
```

Inheritance:
`object` -> `ItemContainerInfo`

Containing type:
`ItemContainerGenerator`

Access:
`private`; this nested helper is an implementation detail of `ItemContainerGenerator` and is not available to callers outside the containing type.

## Examples
Callers do not create `ItemContainerInfo` directly. The containing generator writes and reads this metadata through its public static helper methods.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIElement container = new();

ItemContainerGenerator.SetInfo(container, index: 2, item: "Ink", isSelected: true);

int index = ItemContainerGenerator.GetItemIndex(container);
object? item = ItemContainerGenerator.GetItem(container);
bool isSelected = ItemContainerGenerator.GetIsSelected(container);

ItemContainerGenerator.ClearInfo(container);
```

## Remarks
`ItemContainerInfo` is the value stored in the generator's static `ConditionalWeakTable<UIElement, ItemContainerInfo>`. Each entry attaches container metadata without adding fields to `UIElement`; the table key is the generated container.

`SetInfo` creates or retrieves an `ItemContainerInfo` entry and assigns all three stored values. `ClearInfo` keeps or creates the entry, then resets it to the detached defaults: `Index` is `-1`, `Item` is `null`, and `IsSelected` is `false`.

The containing `ItemContainerGenerator` uses this metadata to answer `GetItemIndex`, `GetItem`, and `GetIsSelected`. When a container has no table entry, those methods return the same detached-style defaults: `-1`, `null`, and `false`.

## Properties
| Name | Type | Default value | Description |
| --- | --- | --- | --- |
| `Index` | `int` | `-1` | Stores the item index associated with the container. `-1` represents no recorded item index. |
| `Item` | `object?` | `null` | Stores the source item associated with the container. |
| `IsSelected` | `bool` | `false` | Stores the selection state associated with the container. |

## Applies to
Internal container metadata used by `ItemContainerGenerator` in `Cerneala.UI.Controls`.

## See also
- `ItemContainerGenerator`
- `ItemsControl`
- `UIElement`
