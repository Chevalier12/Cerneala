# InvalidationFlags Enum

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/InvalidationFlags.cs`

Specifies which kinds of work an element needs before the next frame. The values can be combined.

```csharp
[Flags]
public enum InvalidationFlags
```

## Examples

Some flags add other flags. `DirtyState.Flags` shows the result:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIElement measured = new();
measured.Invalidate(InvalidationFlags.Measure, "demo");
Console.WriteLine(measured.DirtyState.Flags);
// Measure, Arrange, Render

UIElement text = new();
text.Invalidate(InvalidationFlags.Text, "demo");
Console.WriteLine(text.DirtyState.Flags);
// Measure, Arrange, Render, Text

UIElement resource = new();
resource.Invalidate(InvalidationFlags.Resource, "demo");
Console.WriteLine(resource.DirtyState.Flags);
// Render
```

`Subtree` passes the other flags to every visual descendant:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
UIElement parent = new();
UIElement child = new();
root.VisualChildren.Add(parent);
parent.VisualChildren.Add(child);
root.ProcessFrame();

parent.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "demo");
Console.WriteLine($"{parent.DirtyState.Flags} | {child.DirtyState.Flags}");
// Render | Render
```

## Remarks

`InvalidationRequest.Flags` holds the requested flags. `DirtyPropagation.GetEffectiveFlags` turns them into the effective flags, in this order:

| Rule | Effect |
| --- | --- |
| `SourceProperty` has `UiPropertyOptions.Inherits` | Adds `Inherited` and `Subtree`. |
| `SourceProperty` has `UiPropertyOptions.AffectsAspect` | Adds `Aspect`; removes `Render` unless the property also has `AffectsRender`. |
| `Measure` requested | Adds `Arrange` and `Render`. |
| `Arrange` requested | Adds `Render`. |
| `Text` requested | Adds `Measure`, `Arrange`, and `Render`. |
| `Image` requested | Adds `Measure`, `Arrange`, and `Render` when `AffectsIntrinsicSize` is `true` (the default); otherwise adds only `Render`. |
| `Resource` requested | Removes `Resource` and adds `ResourceEffects`, or `Render` when `ResourceEffects` is `null`. |
| `InputVisual` requested | Adds `Render`. |

The later rules can add back the `Render` that the aspect rule removed.

The effective flags (without `Subtree`) are stored in the target's `DirtyState`. Each of these flags also puts the element in a queue that the next frame processes:

| Flag | Queue |
| --- | --- |
| `Measure`, `Arrange` | Layout queue. |
| `Inherited` | Inherited property queue. |
| `Aspect` | Aspect queue. |
| `Render` | Render queue. |
| `HitTest` | Hit-test queue. |

`Text`, `Image`, `InputVisual`, and `Semantics` are stored in `DirtyState` but have no queue. `UIRoot` also reacts to `Semantics` by marking its semantics tree for rebuild, and to `Render` by invalidating its retained render cache.

When the effective flags contain `Measure`, every visual ancestor gets `Measure` and `Arrange`, up to and including the first layout boundary. `Subtree` sends the effective flags to all visual descendants only when it is in the requested flags; a `Subtree` added by the `Inherits` rule is not used that way.

A detached element (one with no `UIRoot`) stores the effective flags in its own `DirtyState`, marks its visual ancestors for measure work in the same way, and queues nothing.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | No work. |
| `Measure` | `1` | The element must be measured again. |
| `Arrange` | `2` | The element must be arranged again. |
| `Render` | `4` | The element's render cache must be rebuilt. |
| `Text` | `8` | The element's text changed. |
| `Image` | `16` | The element's image changed. |
| `Resource` | `32` | A resource that the element uses changed. Replaced by `ResourceEffects` or `Render`. |
| `Aspect` | `64` | The element's aspects must be applied again. |
| `InputVisual` | `128` | An input state that changes the element's look changed, for example `IsEnabled`. |
| `HitTest` | `256` | The element's hit-testing data must be updated. |
| `Subtree` | `512` | Applies the other flags to all visual descendants as well. |
| `Inherited` | `1024` | Inherited property values must be propagated again. |
| `Semantics` | `2048` | The accessibility (semantics) tree must be rebuilt. |

## Applies to

`InvalidationRequest`, `DirtyState`, `DirtyPropagation`, and `UIElement.Invalidate`.

## See also

- `Cerneala.UI.Invalidation.InvalidationRequest`
- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.DirtyState`
- `Cerneala.UI.Invalidation.IInvalidationSink`
