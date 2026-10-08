namespace Cerneala.Timbre;

// Probe-local copies of the core error contract, so the prototypes compile
// unchanged against the production types after the stage-0 selection.
public enum TimbreErrorKind
{
    SourceUnavailable,
    UnsupportedFormat,
    InvalidData,
    ResourceLimitExceeded,
    VoiceLimitExceeded,
    DeviceUnavailable
}

public sealed class TimbreException(TimbreErrorKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public TimbreErrorKind Kind { get; } = kind;
}
