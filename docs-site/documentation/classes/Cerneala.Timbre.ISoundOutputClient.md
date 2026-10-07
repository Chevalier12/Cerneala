# ISoundOutputClient Interface

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/ISoundOutput.cs`

Notification sink implemented by the [SoundRuntime](Cerneala.Timbre.SoundRuntime.md) and passed to [ISoundOutput.Open](Cerneala.Timbre.ISoundOutput.md).

```csharp
public interface ISoundOutputClient
```

## Remarks

Both methods only signal the runtime's mixer thread; they never block, perform I/O, run DSP, touch UI, or throw back into the caller, so an output may call them from a native audio callback on any thread.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `NotifyCapacityAvailable()` | `void` | Reports that queued frames were consumed. |
| `NotifyDeviceLost(Exception? error)` | `void` | Reports device loss; affected playbacks fail with `DeviceUnavailable` and the output is closed. |
