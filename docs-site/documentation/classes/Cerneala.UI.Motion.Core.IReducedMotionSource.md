# IReducedMotionSource Interface

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/IReducedMotionSource.cs`

Supplies the platform's reduced-motion preference to a UI root.

```csharp
public interface IReducedMotionSource
```

## Examples

Report a "reduce motion" preference through platform services:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;

UIRoot root = new(800, 600);
Console.WriteLine(root.Motion.ReducedMotion.Mode); // NoPreference

root.SetPlatformServices(new PlatformServices(ReducedMotion: new AlwaysReduce()));
Console.WriteLine(root.Motion.ReducedMotion.Mode); // Reduce

sealed class AlwaysReduce : IReducedMotionSource
{
    public ReducedMotionMode Mode => ReducedMotionMode.Reduce;
}
```

## Remarks

An `IReducedMotionSource` reaches the UI through `IPlatformServices.ReducedMotion`. `PlatformServices` takes it as the `ReducedMotion` constructor parameter, which defaults to `null`. `UiHostOptions.PlatformServices` passes the services to the root that a `UiHost` manages.

`UIRoot.SetPlatformServices` reads `Mode` once and copies it into `root.Motion.ReducedMotion` with `ReducedMotionPolicy.SetMode`. The root does not subscribe to the source and does not read it again. To apply a changed preference, call `SetPlatformServices` again.

When the new services have no reduced-motion source, the policy keeps its current mode. In the example above, calling `root.SetPlatformServices(null)` afterwards leaves `root.Motion.ReducedMotion.Mode` at `Reduce`.

The framework does not ship an implementation of this interface. Platform integrations and applications provide one.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Mode` | `ReducedMotionMode` | Gets the current reduced-motion preference. |

## Applies to

Cerneala motion and platform services.

## See also

- `Cerneala.UI.Motion.Core.ReducedMotionMode`
- `Cerneala.UI.Motion.Core.ReducedMotionPolicy`
- `Cerneala.UI.Platform.PlatformServices`
- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Elements.UIRoot`
