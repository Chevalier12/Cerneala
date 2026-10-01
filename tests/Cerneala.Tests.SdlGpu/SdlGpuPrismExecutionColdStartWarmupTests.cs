using Cerneala.Backends.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuPrismExecutionColdStartWarmupTests
{
    [Fact]
    public void RepeatedBeginAndCompletePrepareExecutionMethodsWithoutFailure()
    {
        Exception? failure = Record.Exception(() =>
        {
            SdlGpuPrismExecutionColdStartWarmup.Begin();
            SdlGpuPrismExecutionColdStartWarmup.Complete();
            SdlGpuPrismExecutionColdStartWarmup.Begin();
            SdlGpuPrismExecutionColdStartWarmup.Complete();
        });

        Assert.Null(failure);
    }
}
