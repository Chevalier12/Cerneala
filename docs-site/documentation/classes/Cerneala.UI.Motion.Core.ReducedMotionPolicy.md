# ReducedMotionPolicy Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/ReducedMotionPolicy.cs`

Stores the reduced-motion mode used by the motion system when creating samplers and layout motion.

```csharp
public sealed class ReducedMotionPolicy
```

Inheritance:
`object` -> `ReducedMotionPolicy`

## Examples

Create a root with reduced motion enabled, then update the policy later:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

ReducedMotionPolicy policy = new(ReducedMotionMode.Reduce);
UIRoot root = new(100, 100, reducedMotion: policy);

policy.SetMode(ReducedMotionMode.DisableNonEssential);
ReducedMotionMode currentMode = root.Motion.ReducedMotion.Mode;
```

## Remarks

`ReducedMotionPolicy` is a mutable holder for a `ReducedMotionMode`. `UIRoot` creates its `MotionSystem` with the supplied policy, or with `ReducedMotionPolicy.Default` when no policy is provided.

Motion specs read the policy through `MotionSpecContext`. `TweenSpec<T>` treats `ReducedMotionMode.Reduce` as a zero-duration tween and records a reduced-motion skip diagnostic when diagnostics are available. `RepeatSpec<T>` completes indefinite repeats immediately when the mode is not `ReducedMotionMode.NoPreference`.

Layout motion also reads the root policy. `LayoutMotionCoordinator` skips first-snapshot capture when the mode is `ReducedMotionMode.DisableNonEssential`, which prevents layout correction animations while keeping the final layout state.

Calling `UIRoot.SetPlatformServices` with a platform service that exposes an `IReducedMotionSource` copies that source's mode into the root's existing policy through `SetMode`.

## Constructors

| Name | Description |
| --- | --- |
| `ReducedMotionPolicy(ReducedMotionMode mode = ReducedMotionMode.NoPreference)` | Initializes a policy with the supplied mode, or `NoPreference` when no mode is supplied. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `ReducedMotionPolicy` | Gets a new policy initialized with `ReducedMotionMode.NoPreference`. |
| `Mode` | `ReducedMotionMode` | Gets the current reduced-motion mode. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetMode(ReducedMotionMode mode)` | `void` | Replaces the current reduced-motion mode. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/ReducedMotionPolicy.cs`
- `UI/Motion/Core/ReducedMotionMode.cs`
- `UI/Motion/Core/MotionSystem.cs`
- `UI/Motion/Specs/TweenSpec.cs`
- `UI/Motion/Specs/RepeatSpec.cs`
- `UI/Motion/Layout/LayoutMotionCoordinator.cs`
