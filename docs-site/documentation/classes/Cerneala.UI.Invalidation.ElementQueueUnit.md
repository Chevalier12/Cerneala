# ElementQueueUnit Struct

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/ElementWorkQueue.cs`

Provides a zero-data metadata value for element work queues that only track membership.

```csharp
internal readonly struct ElementQueueUnit
```

## Examples

Queue wrappers use the shared value when an element needs no associated metadata:

```csharp
ElementWorkQueue<ElementQueueUnit> queue = new(root);
queue.Enqueue(element, ElementQueueUnit.Value);
```

## Remarks

`ElementQueueUnit` lets queue wrappers share `ElementWorkQueue<TMetadata>` without allocating or inventing meaningful metadata. Its single `Value` member returns the default zero-sized value.

The type is internal and is used by `AspectQueue`, `CommandStateQueue`, `HitTestQueue`, `InheritedPropertyQueue`, and `RenderQueue`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `ElementQueueUnit` | Gets the shared default unit value. |

## Applies to

Cerneala retained UI invalidation queues without entry metadata.

## See also

- `Cerneala.UI.Invalidation.ElementWorkQueue<TMetadata>`
- `Cerneala.UI.Invalidation.RenderQueue`
- `Cerneala.UI.Invalidation.AspectQueue`
