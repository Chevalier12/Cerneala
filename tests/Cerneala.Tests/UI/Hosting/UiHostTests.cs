using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

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
    public void OptionsCanProvideInputBridge()
    {
        ElementInputBridge inputBridge = new();
        UiHost host = new(new UiHostOptions
        {
            Root = new UIRoot(),
            InputBridge = inputBridge
        });

        Assert.Same(inputBridge, host.InputBridge);
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

    [Fact]
    public void UpdatePassesExplicitElapsedTimeToRepeatInputOnce()
    {
        UIRoot root = new(100, 100);
        RepeatButton button = new() { Delay = 10 };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(100, 100)
        });

        host.Update(PointerFrame(previousDown: false, currentDown: true), elapsedTime: TimeSpan.Zero);
        host.Update(PointerFrame(previousDown: true, currentDown: true), elapsedTime: TimeSpan.FromMilliseconds(9));
        Assert.Equal(1, clickCount);

        host.Update(PointerFrame(previousDown: true, currentDown: true), elapsedTime: TimeSpan.FromMilliseconds(1));

        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void DrawSubmitsOneCurrentFrameAnalysisWithTheCommittedCommands()
    {
        UIRoot root = new(100, 100);
        FakeDrawingBackend backend = new();
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(100, 100)
        });
        host.Update(
            FakeInputSource.CreateFrame(),
            elapsedTime: TimeSpan.Zero);

        host.Draw(backend);

        Assert.Equal(1, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        DrawingFrameContext frameContext =
            Assert.IsType<DrawingFrameContext>(backend.LastFrameContext);
        Assert.Equal(
            backend.LastCommands!.Version,
            frameContext.PrismAnalysis.CommandListVersion);
        frameContext.EnsureCurrent(backend.LastCommands);
    }

    private static InputFrame PointerFrame(bool previousDown, bool currentDown)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(10, 10);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
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
