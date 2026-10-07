# SoundException Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundException.cs`

Structured Timbre failure carrying a [SoundErrorKind](Cerneala.Timbre.SoundErrorKind.md).

```csharp
public sealed class SoundException : Exception
```

Inheritance:
`object` -> `Exception` -> `SoundException`

## Examples

```csharp
using Cerneala.Timbre;

try
{
    await runtime.PrepareAsync(clip);
}
catch (SoundException failure) when (failure.Kind == SoundErrorKind.ResourceLimitExceeded)
{
    // The decoded payload does not fit the configured preload or cache limit.
}
```

## Remarks

Asynchronous failures are reported through [SoundPlaybackResult.Error](Cerneala.Timbre.SoundPlaybackResult.md) and never thrown into the caller or an audio callback. Synchronous throws are limited to `PrepareAsync` failures and voice admission (`VoiceLimitExceeded`) in `SoundScope.Play`. Invalid API usage throws standard argument and state exceptions instead. A custom [SoundReader](Cerneala.Timbre.SoundReader.md) may throw a `SoundException` to report a specific kind.

## Constructors

| Name | Description |
| --- | --- |
| `SoundException(SoundErrorKind kind, string message, Exception? innerException = null)` | Creates an exception of the given kind. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `SoundErrorKind` | Failure category. |
