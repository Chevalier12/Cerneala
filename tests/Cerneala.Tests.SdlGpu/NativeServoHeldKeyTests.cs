using System.Diagnostics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeServoHeldKeyTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void RealWindowRoutesHeldKeyAcrossCompletedFramesAndReleasesAfterCompletionAndCancellation()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "held-key-editor");
        Window window = new() { Content = editor, Width = 320, Height = 160 };
        try
        {
            runtime.Show(window, modal: false);
            ServoApi servo = new(window, new ServoOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(5)
            });
            Task focus = servo.ClickAsync(ServoTarget.ById("held-key-editor"));
            PumpUntil(() => focus.IsCompleted);
            focus.GetAwaiter().GetResult();
            Assert.True(editor.IsKeyboardFocused);

            int heldFrames = 0;
            long firstHeldCallbackAt = 0;
            long firstReleasedCallbackAt = 0;
            window.FrameRendered += (_, _) =>
            {
                if (window.LastFrame!.Input.Keyboard.IsDown(InputKey.Right))
                {
                    heldFrames++;
                    if (firstHeldCallbackAt == 0) firstHeldCallbackAt = Stopwatch.GetTimestamp();
                }
                else if (firstHeldCallbackAt != 0 && firstReleasedCallbackAt == 0)
                {
                    firstReleasedCallbackAt = Stopwatch.GetTimestamp();
                }
            };

            TimeSpan duration = TimeSpan.FromMilliseconds(400);
            Task hold = servo.HoldKeyAsync(InputKey.Right, duration);
            PumpUntil(() => hold.IsCompleted);
            hold.GetAwaiter().GetResult();

            Assert.True(heldFrames >= 2, $"Expected multiple completed native frame callbacks with Right down, got {heldFrames}.");
            Assert.True(firstReleasedCallbackAt != 0);
            Assert.True(Stopwatch.GetElapsedTime(firstHeldCallbackAt, firstReleasedCallbackAt) >= duration);
            Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.Right));

            using CancellationTokenSource cancellation = new();
            Task canceledHold = servo.HoldKeyAsync(InputKey.Right, TimeSpan.FromSeconds(2),
                cancellationToken: cancellation.Token);
            PumpUntil(() => window.LastFrame!.Input.Keyboard.IsDown(InputKey.Right));
            cancellation.Cancel();
            Assert.False(canceledHold.IsCompleted);
            PumpUntil(() => canceledHold.IsCompleted);
            Assert.ThrowsAny<OperationCanceledException>(() => canceledHold.GetAwaiter().GetResult());
            Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.Right));
        }
        finally
        {
            runtime.Close(window, force: true);
        }

        void PumpUntil(Func<bool> done)
        {
            Stopwatch deadline = Stopwatch.StartNew();
            while (!done() && deadline.Elapsed < TimeSpan.FromSeconds(15))
            {
                runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
                Thread.Sleep(1);
            }

            Assert.True(done(), "The native Window did not reach the required held-key state.");
        }
    }
}
