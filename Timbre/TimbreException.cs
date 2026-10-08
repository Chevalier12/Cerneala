namespace Cerneala.Timbre;

public enum TimbreErrorKind
{
    SourceUnavailable,
    UnsupportedFormat,
    InvalidData,
    ResourceLimitExceeded,
    VoiceLimitExceeded,
    DeviceUnavailable
}

public sealed class TimbreException : Exception
{
    public TimbreException(TimbreErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public TimbreErrorKind Kind { get; }
}
