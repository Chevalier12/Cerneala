# IInvalidationSink Interface

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/IInvalidationSink.cs`

Represents an object that accepts invalidation requests for elements.

```csharp
public interface IInvalidationSink
```

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
UIElement child = new();
root.VisualChildren.Add(child);
root.ProcessFrame();

IInvalidationSink sink = root;
sink.Invalidate(new InvalidationRequest(child, InvalidationFlags.Measure, "demo"));
Console.WriteLine($"{child.DirtyState.Flags} | {root.DirtyState.Flags}");
// Measure, Arrange, Render | Measure, Arrange
```

The child gets the expanded flags. Its parent, the root, gets measure and arrange work, because a child's size can change its parent's layout.

## Remarks

`UIRoot` is the only Cerneala implementation. `UIRoot.Invalidate(InvalidationRequest)` does the following:

1. Checks that the call runs on the root's thread (`Relay.VerifyAccess`), and records the request in the root's `InvalidationTrace`.
2. Computes the effective flags with `DirtyPropagation.Default.GetEffectiveFlags`.
3. If the effective flags contain `Semantics`, marks the semantics tree for rebuild. If they contain `Render`, invalidates the root's retained render cache. If they contain `HitTest` and the request comes from `IsEnabled`, `IsVisible`, `Visibility`, a presence change, or an added or removed input handler, invalidates the input route cache.
4. Marks the target and queues it with `DirtyPropagation.Default.Propagate`, including the measure ancestors and, for `Subtree`, the visual descendants.

The root does not check that the target is in its tree.

`UIElement.Invalidate` forwards to `Root.Invalidate` when the element is attached. A detached element marks its own `DirtyState` instead; for measure work it also marks its visual ancestors with `Measure` and `Arrange`, up to the first layout boundary.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Invalidate(InvalidationRequest request)` | `void` | Accepts an invalidation request. |

## Applies to

`UIRoot`.

## See also

- `Cerneala.UI.Invalidation.InvalidationRequest`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Elements.UIRoot`
