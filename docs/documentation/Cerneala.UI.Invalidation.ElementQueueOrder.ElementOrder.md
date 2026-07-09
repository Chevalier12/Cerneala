# ElementQueueOrder.ElementOrder Struct

## Definition
Namespace: `Cerneala.UI.Invalidation`  
Assembly/Project: `Cerneala`  
Source: `UI/Invalidation/ElementQueueOrder.cs`

Stores a queued `UIElement` together with its original queue position while `ElementQueueOrder.Sort` builds a stable visual-tree order.

```csharp
private readonly record struct ElementOrder(UIElement Element, int Index);
```

Containing type: `ElementQueueOrder`  
Accessibility: `private`  
Implements: value equality generated for a C# `record struct`

## Examples

The type is created only inside `ElementQueueOrder.Sort`, where each input element is paired with the order in which it appeared in the incoming enumerable:

```csharp
return elements
    .Select((Element, Index) => new ElementOrder(Element, Index))
    .OrderBy(item => order.TryGetValue(item.Element, out int treeIndex) ? treeIndex : int.MaxValue)
    .ThenBy(item => item.Index)
    .Select(item => item.Element)
    .ToArray();
```

## Remarks

`ElementOrder` is an internal implementation detail of the invalidation queue ordering code. `Element` is the queued UI element being sorted. `Index` preserves the element's original input position so elements with the same visual-tree priority, including elements not found under the supplied root, keep deterministic relative order.

Because the struct is declared as `readonly`, instances are immutable after construction. Because it is declared as a `record struct`, the compiler supplies value-based equality, `GetHashCode`, `ToString`, deconstruction, and equality operators. The source does not use those generated members directly; the sort reads only `Element` and `Index`.

## Constructors

| Name | Description |
| --- | --- |
| `ElementOrder(UIElement Element, int Index)` | Creates a queue-order value for one element and its original enumerable index. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | The queued element being sorted. |
| `Index` | `int` | The zero-based position assigned when the input enumerable is projected. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out UIElement Element, out int Index)` | Deconstructs the record struct into its positional values. |
| `Equals(ElementOrder other)` | Determines whether another `ElementOrder` has the same positional values. |
| `Equals(object? obj)` | Determines whether an object is an equivalent `ElementOrder`. |
| `GetHashCode()` | Returns the value-based hash code generated for the record struct. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==(ElementOrder left, ElementOrder right)` | Compares two values using generated record-struct equality. |
| `operator !=(ElementOrder left, ElementOrder right)` | Compares two values using generated record-struct inequality. |

## Applies to

`Cerneala.UI.Invalidation.ElementQueueOrder.Sort(UIElement root, IEnumerable<UIElement> elements)`

## See also

- `Cerneala.UI.Invalidation.ElementQueueOrder`
- `Cerneala.UI.Elements.UIElement`
