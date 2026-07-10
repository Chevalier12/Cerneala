using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Microsoft.Xna.Framework;

namespace Cerneala.WindowsDxSmoke;

internal static class WindowsDxSmokeApplication
{
    public static int Main()
    {
        if (!OperatingSystem.IsWindows())
        {
            return 0;
        }

        try
        {
            using Win32WindowPlatform platform = new();
            CallbackSink callbacks = new();
            using IPlatformWindow first = platform.CreateWindow(
                new Window { Title = "WindowsDX Smoke A", Width = 240, Height = 160 },
                callbacks);
            using IPlatformWindow second = platform.CreateWindow(
                new Window { Title = "WindowsDX Smoke B", Width = 260, Height = 180 },
                callbacks);
            using IPlatformWindow third = platform.CreateWindow(
                new Window { Title = "WindowsDX Smoke C", Width = 220, Height = 150 },
                callbacks);

            first.Show();
            second.Show();
            third.Show();
            platform.PumpEvents();

            WindowsDxWindowGraphicsSession firstSession = RequireWindowsDx(first);
            WindowsDxWindowGraphicsSession secondSession = RequireWindowsDx(second);
            WindowsDxWindowGraphicsSession thirdSession = RequireWindowsDx(third);
            Assert(!ReferenceEquals(firstSession.GraphicsDevice, secondSession.GraphicsDevice), "Windows shared a GraphicsDevice.");
            Assert(!ReferenceEquals(firstSession.GraphicsDevice, thirdSession.GraphicsDevice), "Windows shared a GraphicsDevice.");
            Assert(!ReferenceEquals(secondSession.GraphicsDevice, thirdSession.GraphicsDevice), "Windows shared a GraphicsDevice.");
            Assert(firstSession.WindowHandle != secondSession.WindowHandle, "Windows shared an HWND.");
            Assert(secondSession.WindowHandle != thirdSession.WindowHandle, "Windows shared an HWND.");

            RenderAndCheck(firstSession, new DrawColor(210, 35, 45));
            RenderAndCheck(secondSession, new DrawColor(30, 150, 80));
            RenderAndCheck(thirdSession, new DrawColor(55, 95, 175));
            VerifyPerDeviceImages(firstSession, secondSession);
            RenderTextAndCheck(secondSession);

            firstSession.Resize(192, 128, 1.25f);
            secondSession.Resize(224, 144, 1.5f);
            RenderAndCheck(firstSession, new DrawColor(35, 90, 210));
            RenderAndCheck(secondSession, new DrawColor(220, 150, 30));
            RenderTextAndCheck(secondSession);

            first.Dispose();
            RenderAndCheck(secondSession, new DrawColor(120, 45, 190));
            second.Dispose();
            RenderAndCheck(thirdSession, new DrawColor(20, 170, 180));
            third.Dispose();
            platform.PumpEvents();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static WindowsDxWindowGraphicsSession RequireWindowsDx(IPlatformWindow window)
    {
        return window.GraphicsSession as WindowsDxWindowGraphicsSession
            ?? throw new InvalidOperationException("Window did not receive a WindowsDX graphics session.");
    }

    private static void RenderAndCheck(WindowsDxWindowGraphicsSession session, DrawColor color)
    {
        int width = session.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = session.GraphicsDevice.PresentationParameters.BackBufferHeight;
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, width, height),
            color));

        session.BeginFrame(DrawColor.Black);
        session.DrawingBackend.Render(commands);
        session.Present();

        Color[] pixels = new Color[width * height];
        session.GraphicsDevice.GetBackBufferData(pixels);
        Color actual = pixels[((height / 2) * width) + (width / 2)];
        Assert(
            Math.Abs(actual.R - color.R) <= 2 &&
            Math.Abs(actual.G - color.G) <= 2 &&
            Math.Abs(actual.B - color.B) <= 2,
            $"Unexpected center pixel. Expected {color}, received {actual}.");
    }

    private static void VerifyPerDeviceImages(
        WindowsDxWindowGraphicsSession firstSession,
        WindowsDxWindowGraphicsSession secondSession)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-windowsdx-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(
                path,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl2n0sAAAAASUVORK5CYII="));
            using MonoGameImage firstImage = AssertImage(firstSession.ImageLoader.Load(path));
            using MonoGameImage secondImage = AssertImage(secondSession.ImageLoader.Load(path));

            Assert(!ReferenceEquals(firstImage.Texture, secondImage.Texture), "Windows shared a Texture2D.");
            Assert(ReferenceEquals(firstImage.Texture.GraphicsDevice, firstSession.GraphicsDevice), "First image used the wrong device.");
            Assert(ReferenceEquals(secondImage.Texture.GraphicsDevice, secondSession.GraphicsDevice), "Second image used the wrong device.");

            DrawCommandList foreignImageCommands = new();
            foreignImageCommands.Add(DrawCommand.DrawImage(firstImage, new DrawRect(0, 0, 8, 8), DrawColor.White));
            secondSession.BeginFrame(DrawColor.Black);
            InvalidOperationException foreignImageError = ExpectInvalidOperation(
                () => secondSession.DrawingBackend.Render(foreignImageCommands));
            Assert(foreignImageError.Message.Contains("GraphicsDevice", StringComparison.Ordinal), "Cross-device image failure was not descriptive.");
            secondSession.Present();

            int width = firstSession.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = firstSession.GraphicsDevice.PresentationParameters.BackBufferHeight;
            DrawCommandList commands = new();
            commands.Add(DrawCommand.DrawImage(firstImage, new DrawRect(0, 0, width, height), DrawColor.White));
            firstSession.BeginFrame(DrawColor.Black);
            firstSession.DrawingBackend.Render(commands);
            firstSession.Present();

            Color center = ReadCenterPixel(firstSession);
            Assert(center != Color.Black, "Path-backed image did not reach the first window backbuffer.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void RenderTextAndCheck(WindowsDxWindowGraphicsSession session)
    {
        SkiaFont font = (SkiaFont)new SystemFontSource().LoadFont("Arial", 24);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(
            new DrawTextRun(font, "Cerneala", 24),
            new DrawPoint(12, 12),
            DrawColor.Black));
        session.BeginFrame(DrawColor.White);
        session.DrawingBackend.Render(commands);
        session.Present();

        int width = session.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = session.GraphicsDevice.PresentationParameters.BackBufferHeight;
        Color[] pixels = new Color[width * height];
        session.GraphicsDevice.GetBackBufferData(pixels);
        Assert(pixels.Any(pixel => pixel.R < 240 || pixel.G < 240 || pixel.B < 240), "Skia/HarfBuzz text texture was not visible.");
    }

    private static Color ReadCenterPixel(WindowsDxWindowGraphicsSession session)
    {
        int width = session.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = session.GraphicsDevice.PresentationParameters.BackBufferHeight;
        Color[] pixels = new Color[width * height];
        session.GraphicsDevice.GetBackBufferData(pixels);
        return pixels[((height / 2) * width) + (width / 2)];
    }

    private static MonoGameImage AssertImage(IDrawImage image)
    {
        return image as MonoGameImage
            ?? throw new InvalidOperationException("Window image loader did not create a MonoGameImage.");
    }

    private static InvalidOperationException ExpectInvalidOperation(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Expected an InvalidOperationException.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }

        public void RenderRequested() { }
    }
}
