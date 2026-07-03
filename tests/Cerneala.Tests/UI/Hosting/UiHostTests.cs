using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostTests
{
    [Fact]
    public void UpdateReadsOneInputFrameFromSource()
    {
        FakeInputSource inputSource = new();
        UiHost host = new(new UiHostOptions
        {
            Root = new UIRoot(),
            InputSource = inputSource
        });

        host.Update(new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, inputSource.GetFrameCalls);
        Assert.Same(inputSource.NextFrame, host.LastFrame!.Input);
    }

    [Fact]
    public void ExplicitInputFrameDoesNotReadInputSource()
    {
        FakeInputSource inputSource = new();
        UiHost host = new(new UiHostOptions
        {
            Root = new UIRoot(),
            InputSource = inputSource
        });
        var explicitFrame = FakeInputSource.CreateFrame(5, 6);

        UiFrame frame = host.Update(explicitFrame, new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(0, inputSource.GetFrameCalls);
        Assert.Same(explicitFrame, frame.Input);
    }

    [Fact]
    public void UpdateUsesCurrentBackendInputSourceWhenBackendChanges()
    {
        FakeInputSource firstInputSource = new();
        FakeInputSource secondInputSource = new();
        UiHost host = new(new UiHostOptions
        {
            Root = new UIRoot(),
            Backend = new BackendInputSource(firstInputSource)
        });

        host.Backend = new BackendInputSource(secondInputSource);

        UiFrame frame = host.Update(new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(0, firstInputSource.GetFrameCalls);
        Assert.Equal(1, secondInputSource.GetFrameCalls);
        Assert.Same(secondInputSource.NextFrame, frame.Input);
    }

    [Fact]
    public void ClockSuppliesElapsedTimeWhenUpdateDoesNotReceiveIt()
    {
        FakeUiClock clock = new() { ElapsedTime = TimeSpan.FromSeconds(1) };
        UiHost host = new(new UiHostOptions
        {
            Root = new UIRoot(),
            Clock = clock
        });

        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100));

        Assert.Equal(TimeSpan.FromSeconds(1), frame.ElapsedTime);
    }

    [Fact]
    public void MissingRootFailsBeforeUpdateWork()
    {
        UiHost host = new(new UiHostOptions { InputSource = new FakeInputSource() });

        Assert.Throws<InvalidOperationException>(() => host.Update(new UiViewport(100, 100), TimeSpan.Zero));
    }

    [Fact]
    public void MissingInputSourceFailsWhenNoExplicitFrameIsSupplied()
    {
        UiHost host = new(new UiHostOptions { Root = new UIRoot() });

        Assert.Throws<InvalidOperationException>(() => host.Update(new UiViewport(100, 100), TimeSpan.Zero));
    }

    [Fact]
    public void MissingBackendFailsForImplicitDraw()
    {
        UiHost host = new(new UiHostOptions { Root = new UIRoot() });

        Assert.Throws<InvalidOperationException>(() => host.Draw());
    }

    [Fact]
    public void SetRootReplacesRootAndPrimesNextFrame()
    {
        UiHost host = new(new UiHostOptions { Root = new UIRoot() });
        UIRoot nextRoot = new();

        host.SetRoot(nextRoot);
        UiFrame frame = host.Update(FakeInputSource.CreateFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Same(nextRoot, host.Root);
        Assert.True(frame.Stats.HasWork);
    }

    private sealed class BackendInputSource : IUiBackend
    {
        public BackendInputSource(IInputSource inputSource)
        {
            InputSource = inputSource;
        }

        public IInputSource? InputSource { get; }

        public IDrawingBackend? DrawingBackend => null;
    }
}
