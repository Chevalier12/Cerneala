# SceneSpatialLease2D&lt;T&gt; Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSpatialSource2D.cs`

Owns one acquisition of a reference-type payload and optionally releases that acquisition when disposed.

```csharp
public sealed class SceneSpatialLease2D<T> : IDisposable where T : class
```

## Examples

```csharp
using var acquisition = new SceneSpatialLease2D<MemoryStream>(
    new MemoryStream(), static leasedStream => leasedStream.Dispose());
MemoryStream stream = acquisition.Value;
```

## Remarks

The payload cannot be null. Disposal atomically removes the lease's payload and callback references, invokes the optional release callback at most once, and makes subsequent `Value` access throw `ObjectDisposedException`. Repeated or concurrent disposal cannot duplicate the callback. A throwing callback is propagated, but does not make the acquisition available for release a second time.

The payload is **not** automatically disposed merely because it implements `IDisposable`. The release callback defines ownership of this acquisition. Other references held by the application remain its responsibility. Do not dispose or use the same acquired payload concurrently unless that payload's own contract permits it.

The callback runs on the thread that disposes the lease; no UI dispatch is performed.

## Constructors

| Name | Description |
| --- | --- |
| `SceneSpatialLease2D(T value, Action<T>? release = null)` | Acquires a non-null value with an optional release callback. |

## Properties

| Name | Description |
| --- | --- |
| `Value` | Returns the payload while the acquisition is alive. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Removes the acquisition and invokes its callback at most once. |
