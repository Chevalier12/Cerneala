using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Text;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Input.MonoGame;
using Cerneala.UI.Resources.MonoGame;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Relay;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Hosting.MonoGame;

public sealed class MonoGameUiHost : IDisposable
{
    private readonly MonoGameDrawingBackend drawingBackend;
    private readonly MonoGameUiBackend backend;
    private readonly UiHost host;
    private bool disposed;

    public MonoGameUiHost(MonoGameUiHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        SpriteBatch spriteBatch = options.SpriteBatch;
        InputSource = options.InputSource ?? new MonoGameInputSource();
        ContentServices = options.ContentServices ?? new MonoGameContentServices(
            textRasterizer: options.TextRasterizer,
            imageLoader: options.ImageLoader ?? new MonoGameImageLoader(spriteBatch.GraphicsDevice));
        drawingBackend = new MonoGameDrawingBackend(spriteBatch, options.WhitePixel, ContentServices.TextRasterizer);
        backend = new MonoGameUiBackend(InputSource, drawingBackend);
        host = new UiHost(new UiHostOptions
        {
            Root = options.Root,
            Viewport = options.Viewport,
            Backend = backend,
            Clock = options.Clock,
            PlatformServices = options.PlatformServices
        });
        AttachContentServices(host.Root);
    }

    public MonoGameInputSource InputSource { get; }

    public MonoGameContentServices ContentServices { get; }

    public UIRoot? Root => host.Root;

    public UiRelay? Relay => host.Relay;

    public UiFrame? LastFrame => host.LastFrame;

    public void SetRoot(UIRoot root)
    {
        host.SetRoot(root);
        AttachContentServices(root);
    }

    public UiFrame Update(UiViewport viewport, TimeSpan elapsedTime)
    {
        RequireRelay().VerifyAccess();
        GeneratedWindowApplication.PumpHosted(elapsedTime);
        InputSource.CoordinateScale = viewport.Scale;
        return host.Update(viewport, elapsedTime);
    }

    public UiFrame Update(InputFrame inputFrame, UiViewport viewport, TimeSpan elapsedTime)
    {
        RequireRelay().VerifyAccess();
        GeneratedWindowApplication.PumpHosted(elapsedTime);
        return host.Update(inputFrame, viewport, elapsedTime);
    }

    public void QueueTextInput(string text)
    {
        InputSource.QueueTextInput(text);
    }

    public void Draw()
    {
        RequireRelay().VerifyAccess();
        drawingBackend.CoordinateScale = host.Viewport.Scale;
        host.Draw(drawingBackend);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        drawingBackend.Dispose();
        ContentServices.Dispose();
        GeneratedWindowApplication.StopHosted();
        disposed = true;
    }

    private void AttachContentServices(UIRoot? root)
    {
        root?.SetImageResourceCache(ContentServices.ImageLoader, ContentServices.ImageResourceCache);
    }

    private UiRelay RequireRelay()
    {
        return Relay ?? throw new InvalidOperationException("MonoGameUiHost requires a retained root.");
    }

    private sealed class MonoGameUiBackend : IUiBackend
    {
        public MonoGameUiBackend(IInputSource inputSource, IDrawingBackend drawingBackend)
        {
            InputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
            DrawingBackend = drawingBackend ?? throw new ArgumentNullException(nameof(drawingBackend));
        }

        public IInputSource InputSource { get; }

        public IDrawingBackend DrawingBackend { get; }
    }
}
