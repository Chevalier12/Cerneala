# ITimbreOutput Interface

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/ITimbreOutput.cs`

Runtime-scoped destination for the final Timbre mix, implemented by a platform backend or a test sink.

```csharp
public interface ITimbreOutput
```

## Remarks

The output receives already mixed PCM and does not create voices, run DSP, decode, or decide playback lifetime. One [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md) uses one output, shared by all its scopes and windows.

- **Format.** `Submit` receives interleaved stereo float32 samples at 48 kHz in complete frames (two samples). The output owns any conversion or resampling toward the device and must copy the data before returning.
- **Producer.** Only the runtime's mixer thread calls `Open`, `Submit`, and `Close`.
- **Lifetime.** `Open` is called lazily before the first block; throwing means the device is unavailable. `Close` is called when the runtime is disposed or after a device failure, never because one playback was canceled, paused, sought, or replaced. The runtime never asks the output to clear or pause its queue.
- **Backpressure.** `QueuedFrames` reports frames submitted and not yet consumed by the device side. The runtime submits 480-frame blocks only while `QueuedFrames + 480 ≤ 1920` (40 ms) and treats `submitted − QueuedFrames` as consumed frames for completion.
- **Notifications.** After consuming frames, the output calls [ITimbreOutputClient.NotifyCapacityAvailable](Cerneala.Timbre.ITimbreOutputClient.md); on device loss, `NotifyDeviceLost`. Calls may come from any thread, including a native audio callback.

Consumed counts describe the software queue, not the moment a sample is heard at the DAC.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `QueuedFrames` | `int` | Submitted frames not yet consumed. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Open(ITimbreOutputClient client)` | `void` | Opens the device and registers the runtime's client. |
| `Submit(ReadOnlySpan<float> samples)` | `void` | Queues a block of mixed PCM. |
| `Close()` | `void` | Releases the device. |
