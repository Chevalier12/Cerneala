namespace Cerneala.Timbre;

// Probe-local copies of the core error contract, so the prototypes compile
// unchanged against the production types after the stage-0 selection.
public enum SoundErrorKind
{
    SourceUnavailable,
    UnsupportedFormat,
    InvalidData,
    ResourceLimitExceeded,
    VoiceLimitExceeded,
    DeviceUnavailable
}

public sealed class SoundException(SoundErrorKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public SoundErrorKind Kind { get; } = kind;
}
