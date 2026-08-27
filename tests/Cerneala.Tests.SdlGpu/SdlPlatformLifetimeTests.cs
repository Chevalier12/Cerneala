using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlPlatformLifetimeTests
{
    [Fact]
    public void InitializationAndShutdownOccurExactlyOnceOnTheUiThread()
    {
        FakeSdlApi api = new();
        SdlPlatformLifetime lifetime = new(api);

        lifetime.Dispose();
        lifetime.Dispose();

        Assert.Equal(1, api.InitializeCount);
        Assert.Equal(1, api.QuitCount);
        Assert.Equal(lifetime.UiThreadId, api.InitializeThreadId);
        Assert.Equal(lifetime.UiThreadId, api.QuitThreadId);
        Assert.True(lifetime.IsDisposed);
    }

    [Fact]
    public void InitializationFailureIncludesTheSdlErrorAndUnwindsSdl()
    {
        FakeSdlApi api = new()
        {
            InitializeResult = false,
            Error = "video driver unavailable"
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new SdlPlatformLifetime(api));

        Assert.Contains("SDL video initialization", exception.Message, StringComparison.Ordinal);
        Assert.Contains("video driver unavailable", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, api.InitializeCount);
        Assert.Equal(1, api.QuitCount);
    }

    [Fact]
    public void ShutdownFromAnotherThreadIsRejectedWithoutDestroyingSdl()
    {
        FakeSdlApi api = new();
        SdlPlatformLifetime lifetime = new(api);
        Exception? captured = null;
        Thread worker = new(() => captured = Record.Exception(lifetime.Dispose));

        worker.Start();
        worker.Join();

        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(captured);
        Assert.Contains("UI thread", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, api.QuitCount);
        lifetime.Dispose();
        Assert.Equal(1, api.QuitCount);
    }
}
