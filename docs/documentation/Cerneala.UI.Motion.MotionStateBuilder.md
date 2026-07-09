# MotionStateBuilder Class

## Definition
Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionStateBuilder.cs`

Represents the fluent motion-state builder returned by `MotionElementFacade.States()`.

```csharp
public sealed class MotionStateBuilder
```

Inheritance:
`object` -> `MotionStateBuilder`

## Examples

Create a motion-state builder from a UI element motion facade:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;

UIElement element = new();

MotionStateBuilder states = element.Motion().States();
```

## Remarks

`MotionStateBuilder` is created by `MotionElementFacade.States()`. Its constructor is internal, so application code obtains an instance through `element.Motion().States()` rather than constructing it directly.

The current class stores the owning `MotionElementFacade` internally and does not expose public state-registration members yet. It is part of the motion fluent API surface, alongside property animation, gesture, drag, and scroll timeline entry points exposed by `MotionElementFacade`.

## Constructors

| Name | Description |
| --- | --- |
| None | `MotionStateBuilder` has no public constructors. Use `MotionElementFacade.States()` to create an instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| None | N/A | This class declares no public properties. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None | N/A | This class declares no public methods beyond inherited `object` members. |

## Events

| Name | Description |
| --- | --- |
| None | This class declares no public events. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.MotionExtensions`
- `Cerneala.UI.Elements.UIElement`
