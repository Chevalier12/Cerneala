# LayoutMotionOptions Class

## Definition
Namespace: `Cerneala.UI.Motion.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Layout/LayoutMotionOptions.cs`

Stores the motion specification used to animate layout correction transforms for an element.

```csharp
public sealed class LayoutMotionOptions
```

Inheritance:
`object` -> `LayoutMotionOptions`

## Examples
Assign layout motion to an element that also has a stable layout motion identity.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Layout;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

Border item = new()
{
    LayoutMotionId = "card",
    LayoutMotion = LayoutMotionOptions.Spring(
        MotionFactory.Spring<Transform>())
};
```

## Remarks
`LayoutMotionOptions` is assigned through `UIElement.LayoutMotion`. Layout motion participates only when an attached element has both `LayoutMotion` and `LayoutMotionId` set.

When the layout coordinator detects that a participating element's visual bounds changed between the first and last layout snapshots, it creates an inverse correction transform and starts a `LayoutMotionBinding`. The binding jumps the correction to that inverse transform and animates it back to `Transform.Identity` using `CorrectionSpec`.

The `Spring` factory name creates a `LayoutMotionOptions` instance from the supplied `MotionSpec<Transform>`. The supplied spec can be any transform motion specification accepted by the motion system, such as a tween or spring spec from `Cerneala.UI.Motion.Specs.Motion`.

With the built-in transform mixer, `Motion.Spring<Transform>()` springs the decomposed translation, scale, rotation, and skew components independently. A layout correction settles to the exact `Transform.Identity` when every component meets the supplied spring's rest speed and rest delta thresholds. No custom vector mixer is required. Tween specifications remain supported by the factory.

Layout motion corrections are render-only. Tests for layout motion assert that ticking an active correction advances motion frames without enqueueing additional measure or arrange work.

Scroll-offset changes inside `ScrollContentPresenter` content are not layout-motion moves. Scrolling alone does not start or restart a correction, while genuine layout changes within the scrolling content still use `CorrectionSpec`.

Reduced motion can disable layout corrections. `LayoutMotionCoordinator` skips first-snapshot capture when the motion system's reduced-motion mode is `DisableNonEssential`.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `CorrectionSpec` | `MotionSpec<Transform>` | Gets the transform motion specification used to animate the layout correction back to `Transform.Identity`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Spring(MotionSpec<Transform> correctionSpec)` | `LayoutMotionOptions` | Creates layout motion options with the supplied correction specification. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Spring(MotionSpec<Transform>)` | `ArgumentNullException` | `correctionSpec` is `null`. |

## Applies to
Cerneala retained UI layout motion in the `Cerneala` project.

Target framework: `net8.0`

## See also
- `UI/Motion/Layout/LayoutMotionOptions.cs`
- `UI/Motion/Layout/LayoutMotionCoordinator.cs`
- `UI/Motion/Layout/LayoutMotionBinding.cs`
- `UI/Elements/UIElement.cs`
- `UI/Motion/Specs/Motion.cs`
