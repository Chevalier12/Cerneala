using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class ManipulationProcessorTests
{
    [Fact]
    public void OnePointComputesTranslation()
    {
        ManipulationProcessor processor = new();

        processor.Process([new ManipulationPoint(1, 0, 0)]);
        ManipulationDelta delta = processor.Process([new ManipulationPoint(1, 5, 3)]);

        Assert.Equal(5, delta.TranslationX);
        Assert.Equal(3, delta.TranslationY);
        Assert.Equal(1, delta.Scale);
    }

    [Fact]
    public void TwoPointsComputeScale()
    {
        ManipulationProcessor processor = new();

        processor.Process([new ManipulationPoint(1, 0, 0), new ManipulationPoint(2, 10, 0)]);
        ManipulationDelta delta = processor.Process([new ManipulationPoint(1, -5, 0), new ManipulationPoint(2, 15, 0)]);

        Assert.Equal(2, delta.Scale);
    }
}
