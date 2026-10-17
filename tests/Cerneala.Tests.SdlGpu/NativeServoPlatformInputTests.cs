using System.Diagnostics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeServoPlatformInputTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void RealWindowReceivesAutoRepeatAndTextPushedThroughSdl()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        TextBox editor = new() { Text = "ABCDEFG", Width = 200, Height = 40 };
        ServoApi.SetId(editor, "platform-input-editor");
        Window window = new() { Content = editor, Width = 320, Height = 160 };
        try
        {
            runtime.Show(window, modal: false);
            ServoApi servo = new(window, new ServoOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(5)
            });
            Task focus = servo.ClickAsync(ServoTarget.ById("platform-input-editor"));
            PumpUntil(() => focus.IsCompleted);
            focus.GetAwaiter().GetResult();
            Assert.True(editor.IsKeyboardFocused);

            List<(InputKey Key, bool IsRepeat)> backDowns = [];
            window.AddHandler(UIElement.KeyDownEvent, (_, args) =>
            {
                KeyEventArgs key = (KeyEventArgs)args;
                if (key.Key == InputKey.Back) backDowns.Add((key.Key, key.IsRepeat));
            }, handledEventsToo: true);

            Send(ServoPlatformInput.KeyDown(InputKey.End));
            Send(ServoPlatformInput.KeyUp(InputKey.End));
            Send(ServoPlatformInput.KeyDown(InputKey.Back));
            Send(ServoPlatformInput.KeyDown(InputKey.Back, repeat: true));
            Send(ServoPlatformInput.KeyDown(InputKey.Back, repeat: true));
            Send(ServoPlatformInput.KeyUp(InputKey.Back));
            Send(ServoPlatformInput.Text("xyé€"));

            Assert.Equal(3, backDowns.Count);
            Assert.Equal([false, true, true], backDowns.Select(entry => entry.IsRepeat));
            Assert.Equal("ABCDxyé€", editor.Text);
            Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.Back));
        }
        finally
        {
            runtime.Close(window, force: true);
        }

        void Send(params ServoPlatformInput[] events)
        {
            Task send = ServoApiFor(window).SendPlatformInputAsync(events);
            PumpUntil(() => send.IsCompleted);
            send.GetAwaiter().GetResult();
        }

        void PumpUntil(Func<bool> done)
        {
            Stopwatch deadline = Stopwatch.StartNew();
            while (!done() && deadline.Elapsed < TimeSpan.FromSeconds(15))
            {
                runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
                Thread.Sleep(1);
            }

            Assert.True(done(), "The native Window did not complete the platform input.");
        }
    }

    private static ServoApi ServoApiFor(Window window) =>
        new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(5) });
}
