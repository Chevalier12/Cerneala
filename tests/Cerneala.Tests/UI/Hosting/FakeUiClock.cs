using Cerneala.UI.Hosting;

namespace Cerneala.Tests.UI.Hosting;

internal sealed class FakeUiClock : IUiClock
{
    public TimeSpan ElapsedTime { get; set; }

    public TimeSpan GetElapsedTime()
    {
        return ElapsedTime;
    }
}
