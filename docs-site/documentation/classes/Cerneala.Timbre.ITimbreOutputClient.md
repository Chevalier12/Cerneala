# ITimbreOutputClient Interface

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/ITimbreOutput.cs`

Notification sink that a [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md) creates (a private implementation; `TimbreRuntime` itself does not implement this interface) and passes to [ITimbreOutput.Open](Cerneala.Timbre.ITimbreOutput.md).

```csharp
public interface ITimbreOutputClient
```

## Remarks

Both methods only signal the runtime's mixer thread; they never block, perform I/O, run DSP, touch UI, or throw back into the caller, so an output may call them from a native audio callback on any thread.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `NotifyCapacityAvailable()` | `void` | Reports that queued frames were consumed. |
| `NotifyDeviceLost(Exception? error)` | `void` | Reports device loss; affected playbacks fail with `DeviceUnavailable` and the output is closed. |
