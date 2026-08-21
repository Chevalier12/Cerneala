using Cerneala.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal sealed class MonoGameRenderSurface2DSession : IDisposable
{
    private readonly RenderTarget2D renderTarget;
    private readonly SpriteBatch spriteBatch;
    private readonly Texture2D whitePixel;
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
        whitePixel = new Texture2D(graphicsDevice, 1, 1);
        whitePixel.SetData([XnaColor.White]);
    }

    public GraphicsDevice GraphicsDevice { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public bool IsDisposed => disposed;

    public Texture2D Surface => renderTarget;

    public void Render(
        Action<RenderSurface2DFrame> draw,
        CernealaColor clearColor,
        TimeSpan frameTime)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ObjectDisposedException.ThrowIf(disposed, this);

        GraphicsDevice.SetRenderTarget(renderTarget);
        GraphicsDevice.Viewport = new Viewport(0, 0, PixelWidth, PixelHeight);
        GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, PixelWidth, PixelHeight);
        GraphicsDevice.Clear(new XnaColor(
            clearColor.R,
            clearColor.G,
            clearColor.B,
            clearColor.A));

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        RenderSurface2DFrame frame = new(
            spriteBatch,
            whitePixel,
            new Rectangle(0, 0, PixelWidth, PixelHeight),
            frameTime);
        try
        {
            draw(frame);
        }
        finally
        {
            frame.Complete();
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        spriteBatch.Dispose();
        whitePixel.Dispose();
        renderTarget.Dispose();
        disposed = true;
    }
}
