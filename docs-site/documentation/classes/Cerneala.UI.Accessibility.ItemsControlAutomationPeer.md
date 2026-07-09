# ItemsControlAutomationPeer Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: [`UI/Accessibility/ItemsControlAutomationPeer.cs`](../../UI/Accessibility/ItemsControlAutomationPeer.cs)

Provides the automation peer used to expose an `ItemsControl` as a list in the semantics tree.

```csharp
public sealed class ItemsControlAutomationPeer : AutomationPeer
```

Inheritance:
`object` -> `AutomationPeer` -> `ItemsControlAutomationPeer`

## Examples

```csharp
using System.Collections.Generic;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;

ItemsControl itemsControl = new();
itemsControl.SetItems(new[] { "one", "two", "three" });

ItemsControlAutomationPeer peer = new(itemsControl);
IReadOnlyDictionary<SemanticsProperty, object?> properties = peer.GetProperties();

bool isList = peer.Role == SemanticsRole.List;
int itemCount = (int)properties[SemanticsProperty.ItemCount]!;
```

`isList` is `true`, and `itemCount` is `3`.

## Remarks

`ItemsControlAutomationPeer` specializes `AutomationPeer` for `ItemsControl` instances. Its `Role` is always `SemanticsRole.List`.

`GetProperties` starts with the base automation properties and adds `SemanticsProperty.ItemCount` from `ItemsControl.ItemCount`. The inherited base properties include `SemanticsProperty.IsEnabled` and `SemanticsProperty.IsFocused`.

`AutomationPeer.Create(UIElement)` constructs this peer automatically when the supplied element is an `ItemsControl`.

Passing `null` to the constructor throws `ArgumentNullException` through the base `AutomationPeer` constructor.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsControlAutomationPeer(ItemsControl itemsControl)` | Initializes a peer for the specified `ItemsControl`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Role` | `SemanticsRole` | Gets `SemanticsRole.List`. |
| `Name` | `string?` | Inherited from `AutomationPeer`; gets the accessible name for the owner element. |
| `Owner` | `UIElement` | Inherited from `AutomationPeer`; gets the element associated with the peer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetProperties()` | `IReadOnlyDictionary<SemanticsProperty, object?>` | Returns the base automation properties plus `SemanticsProperty.ItemCount`. |
| `CreateNode(IReadOnlyList<SemanticsNode> children)` | `SemanticsNode` | Inherited from `AutomationPeer`; creates a semantics node using the peer role, name, properties, and child nodes. |

## Applies To

Cerneala UI accessibility semantics for `Cerneala.UI.Controls.ItemsControl`.

## See Also

- [`AutomationPeer`](../../UI/Accessibility/AutomationPeer.cs)
- [`ItemsControl`](../../UI/Controls/ItemsControl.cs)
- [`SemanticsProperty`](../../UI/Accessibility/SemanticsProperty.cs)
- [`SemanticsRole`](../../UI/Accessibility/SemanticsRole.cs)
