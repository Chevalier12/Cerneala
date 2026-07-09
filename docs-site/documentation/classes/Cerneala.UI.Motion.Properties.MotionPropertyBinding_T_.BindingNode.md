# MotionPropertyBinding<T>.BindingNode Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyBinding{T}.cs`

Adapts a `MotionPropertyBinding<T>` instance to the `MotionNode` contract used by `MotionGraph`.

```csharp
private sealed class BindingNode(MotionPropertyBinding<T> owner) : MotionNode
```

Containing type:
`MotionPropertyBinding<T>`

Inheritance:
`object` -> `MotionNode` -> `MotionPropertyBinding<T>.BindingNode`

## Examples

`BindingNode` is a private nested implementation detail. It is created by `MotionPropertyBinding<T>` and registered with the owning graph when an animation starts:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

using MotionPropertyBinding<DrawColor> binding =
    root.Motion.Properties.GetOrCreateBinding(
        root.Motion,
        control,
        Control.BackgroundProperty);

binding.AnimateTo(
    DrawColor.White,
    MotionFactory.Tween<DrawColor>(TimeSpan.FromMilliseconds(100)));

root.ProcessFrame();
```

## Remarks

`BindingNode` lets `MotionGraph` tick a property binding without exposing the binding itself as a graph node. `MotionPropertyBinding<T>` constructs one node for the lifetime of the binding and registers it after `AnimateTo` stages the current value.

The node's `Tick(MotionFrame)` override delegates directly to the owning binding's private tick routine. That routine stages pending `MotionValue<T>` samples into `MotionPropertyStore`, clears the animation contribution after natural completion when `HoldOnComplete` is not enabled, cancels and clears the binding when an attached `UIElement` target is detached, and reports completion when the binding no longer has active motion.

Because this class is private, callers cannot construct, register, or subclass it. Use `MotionPropertyBinding<T>.AnimateTo`, `Clear`, and `Dispose` to control the binding; those members handle graph registration and removal.

## Constructors

| Name | Description |
| --- | --- |
| `BindingNode(MotionPropertyBinding<T> owner)` | Captures the owning binding whose tick routine is delegated to the motion graph. |

## Public Properties

| Name | Type | Description |
| --- | --- | --- |
| None |  | `BindingNode` exposes no public properties. |

## Public Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None |  | `BindingNode` exposes no public methods. |

## Protected Internal Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Tick(MotionFrame frame)` | `MotionNodeTickResult` | Delegates graph sampling to the owning `MotionPropertyBinding<T>` and returns that binding's completion result. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Core.MotionNode`
- `Cerneala.UI.Motion.Core.MotionGraph`
