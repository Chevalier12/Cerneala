namespace Cerneala.UI.Servo;

public class ServoException : Exception
{
    public ServoException(string message)
        : base(message)
    {
    }

    public ServoException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

public sealed class ServoTargetNotFoundException : ServoException
{
    public ServoTargetNotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ServoTargetAmbiguousException : ServoException
{
    public ServoTargetAmbiguousException(string message)
        : base(message)
    {
    }
}

public sealed class ServoTargetNotActionableException : ServoException
{
    public ServoTargetNotActionableException(string message)
        : base(message)
    {
    }
}

public sealed class ServoTimeoutException : ServoException
{
    public ServoTimeoutException(string message)
        : base(message)
    {
    }
}
