using Cerneala.UI;
using Cerneala.Tests.UI.Hosting;

namespace Cerneala.Tests.Presentation;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PresentationTestApplicationTests : IDisposable
{
    public PresentationTestApplicationTests()
    {
        PresentationTestApplication.Install();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void ResetOnAnotherThreadLeavesNoRuntimeInstalled()
    {
        // Issue #738: xUnit runs Dispose on the thread where an awaiting test resumed.
        Exception? failure = null;
        Thread other = new(() =>
        {
            try
            {
                WindowApplicationRuntime.ResetForTesting();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        other.Start();
        other.Join();

        Assert.Null(failure);
        Assert.Null(WindowApplicationRuntime.Current);
    }
}
