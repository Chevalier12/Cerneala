using Cerneala.UI.Hosting;

namespace Cerneala.UI.Servo;

internal sealed class ServoInputState
{
    internal ServoInputState(
        UiHost host,
        Func<ServoInputSequence, CancellationToken, Task>? dispatchSequence = null)
    {
        Driver = dispatchSequence is null
            ? new RetainedServoInputDriver(host)
            : new RetainedServoInputDriver(host, dispatchSequence);
    }

    internal SemaphoreSlim Gate { get; } = new(1, 1);

    internal RetainedServoInputDriver Driver { get; }
}
