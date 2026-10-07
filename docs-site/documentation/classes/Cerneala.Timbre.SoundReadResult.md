# SoundReadResult Struct

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundReader.cs`

Result of one [SoundReader.ReadAsync](Cerneala.Timbre.SoundReader.md) call.

```csharp
public readonly struct SoundReadResult : IEquatable<SoundReadResult>
```

## Examples

```csharp
using Cerneala.Timbre;

var last = new SoundReadResult(frames: 120, endOfSource: true);
var waiting = new SoundReadResult(frames: 0, endOfSource: false);
```

## Remarks

`Frames` counts stereo frames written to the destination, not samples or bytes. A negative frame count is rejected. A result of zero frames without end of source obliges the next read to wait asynchronously; see [SoundReader](Cerneala.Timbre.SoundReader.md).

## Constructors

| Name | Description |
| --- | --- |
| `SoundReadResult(int frames, bool endOfSource)` | Creates a result; throws `ArgumentOutOfRangeException` for negative `frames`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Frames` | `int` | Frames written. |
| `EndOfSource` | `bool` | Whether the source ended after these frames. |

## Methods

| Name | Description |
| --- | --- |
| `Equals`, `==`, `!=`, `GetHashCode`, `ToString` | Value equality over both properties. |
