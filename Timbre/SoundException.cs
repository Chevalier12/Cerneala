namespace Cerneala.Timbre;

public enum SoundErrorKind
{
    SourceUnavailable,
    UnsupportedFormat,
    InvalidData,
    ResourceLimitExceeded,
    VoiceLimitExceeded,
    DeviceUnavailable
}

public sealed class SoundException : Exception
{
    public SoundException(SoundErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public SoundErrorKind Kind { get; }
}
