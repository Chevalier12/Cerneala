using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismColdStartWarmupTests
{
    [Fact]
    public void RepeatedBeginAndCompleteWarmTheGraphAndRetainedPipelineWithoutFailure()
    {
        Exception? failure = Record.Exception(() =>
        {
            PrismColdStartWarmup.Begin();
            PrismColdStartWarmup.Complete();
            PrismColdStartWarmup.Begin();
            PrismColdStartWarmup.Complete();
        });

        Assert.Null(failure);
    }
}
