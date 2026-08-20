using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal sealed class MonoGameRenderSurface2DSession : IDisposable
{
    private readonly RenderTarget2D renderTarget;
    private readonly SpriteBatch spriteBatch;
    private bool disposed;

    public MonoGameRenderSurface2DSession(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        GraphicsDevice = graphicsDevice ??
            throw new ArgumentNullException(nameof(graphicsDevice));
        PixelWidth = pixelWidth > 0
            ? pixelWidth
            : throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        PixelHeight = pixelHeight > 0
            ? pixelHeight
            : throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        renderTarget = new RenderTarget2D(
            graphicsDevice,
            pixelWidth,
            pixelHeight,
            mipMap: false,
            preferredFormat: SurfaceFormat.Color,
            preferredDepthFormat: DepthFormat.None);
        spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public GraphicsDevice GraphicsDevice { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public bool IsDisposed => disposed;

    public Texture2D Surface => renderTarget;

    public void Render(Action<SpriteBatch, Rectangle> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ObjectDisposedException.ThrowIf(disposed, this);

        GraphicsDevice.SetRenderTarget(renderTarget);
        GraphicsDevice.Viewport = new Viewport(0, 0, PixelWidth, PixelHeight);
        GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, PixelWidth, PixelHeight);
        GraphicsDevice.Clear(XnaColor.Transparent);
        draw(spriteBatch, new Rectangle(0, 0, PixelWidth, PixelHeight));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        spriteBatch.Dispose();
        renderTarget.Dispose();
        disposed = true;
    }
}
