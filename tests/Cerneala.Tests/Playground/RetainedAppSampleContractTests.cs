using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Playground;

public sealed class RetainedAppSampleContractTests
{
    [Fact]
    public void RetainedAppSampleBuildsRetainedTree()
    {
        RetainedAppSample sample = new();

        UIElement root = sample.Build();

        Assert.NotNull(root);
        Assert.NotEmpty(root.VisualChildren);
        Assert.NotNull(sample.StatusText);
        Assert.NotNull(sample.PrimaryButton);
    }

    [Fact]
    public void DefaultSampleSelectorIncludesRetainedAppSample()
    {
        SampleSelector selector = SampleSelector.CreateDefault();

        Assert.Contains(selector.Samples, sample => sample.Name == "Retained App");
    }

    [Fact]
    public void BuildCreatesANewRetainedTreeWithoutMonoGameObjects()
    {
        RetainedAppSample sample = new();

        UIElement first = sample.Build();
        UIElement second = sample.Build();

        Assert.NotSame(first, second);
        Assert.Null(first.Root);
        Assert.Null(second.Root);
    }

    [Fact]
    public void RetainedAppSampleDoesNotExposePerFrameRebuildHook()
    {
        Assert.DoesNotContain(
            typeof(RetainedAppSample).GetMethods(),
            method => method.Name is "Update" or "Draw");
    }
}
