# TimbreErrorKind Enum

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreException.cs`

Category of a [TimbreException](Cerneala.Timbre.TimbreException.md).

```csharp
public enum TimbreErrorKind
```

## Remarks

The kinds are distinct: a resource refusal is never reported as corrupt data or as end of source, and a failure is never reported as a successful completion.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `SourceUnavailable` | 0 | The source could not be opened or read (missing file, I/O error, reader exception, non-seekable stream). |
| `UnsupportedFormat` | 1 | No decoder accepts the content, the variant is not supported, or the decoder cannot be loaded. |
| `InvalidData` | 2 | Corrupt or truncated data, non-finite samples from the source or from a playback's modifier chain, or a reader contract violation. |
| `ResourceLimitExceeded` | 3 | A configured preload, cache, or streaming budget cannot admit the source. |
| `VoiceLimitExceeded` | 4 | The runtime already has `MaxVoices` non-terminal playbacks. |
| `DeviceUnavailable` | 5 | No output is configured, it failed to open, or the device was lost. |
