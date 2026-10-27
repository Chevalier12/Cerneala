using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Playground;

public sealed class InvestorReelCaptureHarnessTests
{
    // Playground/Cerneala.InvestorReel/ReelWindow.Capture.cs retries the poster click while the poster is not actionable.
    // Servo target actions never throw ServoTargetNotActionableException to the caller: they retry after each frame and,
    // when the timeout expires, throw ServoTimeoutException whose InnerException is the last ServoTargetNotActionableException
    // (ServoTargetNotActionableException.md). The "harness-catch" row copies the harness's call and catch (with a 100 ms
    // attempt timeout instead of 1 s); a button that stays disabled is never actionable, so the loop must retry instead of
    // letting the exception escape. "timeout-catch" is the control.
    [Theory]
    [InlineData("harness-catch")]
    [InlineData("timeout-catch")]
    public async Task RetryLoopAroundClickAsyncRetriesWhileTheTargetIsDisabled(string catchKind)
    {
        Button button = new() { Content = "Poster", Width = 160, Height = 48, IsEnabled = false };
        ServoApi.SetId(button, "theme-ink");
        UiHost host = CreateHost(button);
        ServoApi servo = new(host, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(240) });
        ServoTarget poster = ServoTarget.ById("theme-ink");
        int retries = 0;
        Exception? escaped = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await servo.ClickAsync(poster, TimeSpan.FromMilliseconds(100));
                break;
            }
            catch (ServoTimeoutException timeout) when (catchKind == "harness-catch" && timeout.InnerException is ServoTargetNotActionableException && attempt < 60)
            {
                retries++;
            }
            catch (ServoTimeoutException timeout) when (catchKind == "timeout-catch" && timeout.InnerException is ServoTargetNotActionableException)
            {
                retries++;
            }
            catch (Exception exception)
            {
                escaped = exception;
                break;
            }
        }

        Assert.True(
            retries >= 1,
            $"{catchKind}: the loop retried {retries} time(s); the exception that left the loop was {escaped?.GetType().Name ?? "none"} (InnerException {escaped?.InnerException?.GetType().Name ?? "none"}): {escaped?.Message}");
    }

    private static UiHost CreateHost(UIElement content, float width = 320, float height = 160)
    {
        UIRoot root = new(width, height);
        root.VisualChildren.Add(content);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(width, height)
        });
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }
}
