# MotionStateTargetBuilder Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionStateTargetBuilder.cs`

Registers a typed property target for the state selected by `MotionStateBuilder.When`.

```csharp
public sealed class MotionStateTargetBuilder
```

Inheritance:
`object` -> `MotionStateTargetBuilder`

## Examples

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
UIElement element = new();
root.VisualChildren.Add(element);

MotionStateBuilder states = element.Motion().States();
states.When(AspectState.Focus).Set(
    UIElement.ScaleProperty,
    1.05f,
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));
```

## Remarks

Instances are created by `MotionStateBuilder.When`; there is no public constructor. `Set` captures the element's baseline the first time the property is registered and returns the owning `MotionStateBuilder`, allowing another `When` call to continue the chain.

Registering the same state and property again replaces that target. Registering another state for the same property appends a candidate; the most recently registered matching state wins.

## Constructors

| Name | Description |
| --- | --- |
| None | Use `MotionStateBuilder.When(AspectState)`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(UiProperty<T> property, T value, MotionSpec<T> spec)` | `MotionStateBuilder` | Registers the value and specification used while the selected state matches. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Set` | `ArgumentNullException` | `property` or `spec` is `null`. |
| `Set` | `InvalidOperationException` | The target element is detached and is not a `UIRoot`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionStateBuilder`
- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Aspect.AspectState`
- `Cerneala.UI.Core.UiProperty<T>`
