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
    private readonly RasterizerState scissorRasterizerState;
    private List<RenderSurface2DCommand> retainedCommands = [];
    private List<RenderSurface2DCommand> recordingCommands = [];
    private XnaColor retainedClearColor;
    private bool hasRetainedFrame;
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
            preferredDepthFormat: DepthFormat.None,
            preferredMultiSampleCount: 0,
            usage: RenderTargetUsage.PreserveContents);
        spriteBatch = new SpriteBatch(graphicsDevice);
        whitePixel = new Texture2D(graphicsDevice, 1, 1);
        whitePixel.SetData([XnaColor.White]);
        scissorRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };
    }

    public GraphicsDevice GraphicsDevice { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public bool IsDisposed => disposed;

    public Texture2D Surface => renderTarget;

    internal int RasterizedFrameCount { get; private set; }

    internal Rectangle? LastDamageBounds { get; private set; }

    internal int LastReplayedCommandCount { get; private set; }

    internal int RetainedCommandCount => retainedCommands.Count;

    public void Render(
        Action<RenderSurface2DFrame> draw,
        CernealaColor clearColor,
        TimeSpan frameTime)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ObjectDisposedException.ThrowIf(disposed, this);

        recordingCommands.Clear();
        RenderSurface2DFrame frame = new(
            GraphicsDevice,
            recordingCommands,
            new Rectangle(0, 0, PixelWidth, PixelHeight),
            frameTime);
        try
        {
            draw(frame);
        }
        finally
        {
            frame.Complete();
        }

        XnaColor nextClearColor = new(
            clearColor.R,
            clearColor.G,
            clearColor.B,
            clearColor.A);
        Rectangle surfaceBounds = new(0, 0, PixelWidth, PixelHeight);
        Rectangle? damage = ResolveDamage(
            surfaceBounds,
            nextClearColor);
        LastDamageBounds = damage;
        LastReplayedCommandCount = 0;
        if (damage is Rectangle damageBounds)
        {
            ReplayDamage(
                surfaceBounds,
                damageBounds,
                nextClearColor);
            RasterizedFrameCount++;
        }

        (retainedCommands, recordingCommands) =
            (recordingCommands, retainedCommands);
        retainedClearColor = nextClearColor;
        hasRetainedFrame = true;
    }

    private Rectangle? ResolveDamage(
        Rectangle surfaceBounds,
        XnaColor nextClearColor)
    {
        if (!hasRetainedFrame || retainedClearColor != nextClearColor)
        {
            return surfaceBounds;
        }

        int prefixLength = 0;
        int sharedLength = Math.Min(
            retainedCommands.Count,
            recordingCommands.Count);
        while (prefixLength < sharedLength &&
            retainedCommands[prefixLength].VisuallyEquals(
                recordingCommands[prefixLength]))
        {
            prefixLength++;
        }

        if (prefixLength == retainedCommands.Count &&
            prefixLength == recordingCommands.Count)
        {
            return null;
        }

        int retainedSuffix = retainedCommands.Count - 1;
        int recordingSuffix = recordingCommands.Count - 1;
        while (retainedSuffix >= prefixLength &&
            recordingSuffix >= prefixLength &&
            retainedCommands[retainedSuffix].VisuallyEquals(
                recordingCommands[recordingSuffix]))
        {
            retainedSuffix--;
            recordingSuffix--;
        }

        Rectangle? damage = null;
        for (int index = prefixLength; index <= retainedSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                retainedCommands[index].ResolveDamageBounds(surfaceBounds),
                surfaceBounds);
        }
        for (int index = prefixLength; index <= recordingSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                recordingCommands[index].ResolveDamageBounds(surfaceBounds),
                surfaceBounds);
        }

        return damage;
    }

    private void ReplayDamage(
        Rectangle surfaceBounds,
        Rectangle damageBounds,
        XnaColor clearColor)
    {
        GraphicsDevice.SetRenderTarget(renderTarget);
        GraphicsDevice.Viewport = new Viewport(
            0,
            0,
            PixelWidth,
            PixelHeight);
        GraphicsDevice.ScissorRectangle = damageBounds;

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            scissorRasterizerState);
        try
        {
            spriteBatch.Draw(whitePixel, damageBounds, clearColor);
        }
        finally
        {
            spriteBatch.End();
        }

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            scissorRasterizerState);
        try
        {
            foreach (RenderSurface2DCommand command in recordingCommands)
            {
                if (!command.ResolveDamageBounds(surfaceBounds)
                    .Intersects(damageBounds))
                {
                    continue;
                }

                command.Replay(spriteBatch, whitePixel);
                LastReplayedCommandCount++;
            }
        }
        finally
        {
            spriteBatch.End();
        }
    }

    private static Rectangle? UnionDamage(
        Rectangle? current,
        Rectangle candidate,
        Rectangle surfaceBounds)
    {
        Rectangle clipped = Rectangle.Intersect(
            candidate,
            surfaceBounds);
        if (clipped.Width <= 0 || clipped.Height <= 0)
        {
            return current;
        }

        return current is Rectangle existing
            ? Rectangle.Union(existing, clipped)
            : clipped;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        scissorRasterizerState.Dispose();
        spriteBatch.Dispose();
        whitePixel.Dispose();
        renderTarget.Dispose();
        disposed = true;
    }
}
