# SceneSpatialRegion2D&lt;T&gt; Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialResidency2D.cs`

Represents a prepared catalog selection and pins its payload acquisitions until released or its residency owner is disposed.

```csharp
public sealed class SceneSpatialRegion2D<T> : IDisposable where T : class
```

## Remarks

Regions are created by `SceneSpatialResidency2D<T>.AcquireAsync`; there is no public constructor. A successfully acquired region has a value for every selected entry, in catalog order. `GetValue` throws `KeyNotFoundException` for an unselected ID and `ObjectDisposedException` after the region or its residency owner has been disposed.

`IsCurrent` is true only while both lifetimes are alive and the source still exposes the same catalog snapshot. A replacement catalog makes it false even when the already acquired payloads remain valid. The property is a snapshot check, not a lock against concurrent source publication. It does not certify scene attachment, collision readiness, image upload, or rendering completion.

Disposal releases this region's interests once. Other regions can keep shared acquisitions alive. The immutable `Entries` metadata remains readable after disposal, but cannot be used to extend payload lifetime. Do not concurrently use a payload while its owner or region is being disposed.

## Properties

| Name | Description |
| --- | --- |
| `Entries` | Read-only selected metadata in original catalog order. |
| `IsCurrent` | Whether the region and owner are alive and its catalog is still current. |

## Methods

| Name | Description |
| --- | --- |
| `GetValue(string id)` | Gets the prepared payload for a selected ordinal ID. |
| `Dispose()` | Releases this region's payload interests. |

## See also

- [SceneSpatialResidency2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialResidency2D_T_.md)
