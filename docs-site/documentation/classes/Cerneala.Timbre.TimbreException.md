# TimbreException Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreException.cs`

Structured Timbre failure carrying a [TimbreErrorKind](Cerneala.Timbre.TimbreErrorKind.md).

```csharp
public sealed class TimbreException : Exception
```

Inheritance:
`object` -> `Exception` -> `TimbreException`

## Examples

```csharp
using Cerneala.Timbre;

try
{
    await runtime.PrepareAsync(clip);
}
catch (TimbreException failure) when (failure.Kind == TimbreErrorKind.ResourceLimitExceeded)
{
    // The decoded payload does not fit the configured preload or cache limit.
}
```

## Remarks

Asynchronous failures are reported through [TimbrePlaybackResult.Error](Cerneala.Timbre.TimbrePlaybackResult.md) and never thrown into the caller or an audio callback. Synchronous throws are limited to `PrepareAsync` failures and voice admission (`VoiceLimitExceeded`) in `TimbreScope.Play`. Invalid API usage throws standard argument and state exceptions instead. A custom [TimbreReader](Cerneala.Timbre.TimbreReader.md) may throw a `TimbreException` to report a specific kind.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreException(TimbreErrorKind kind, string message, Exception? innerException = null)` | Creates an exception of the given kind. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `TimbreErrorKind` | Failure category. |
