using Cerneala.UI.Controls;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
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
    private readonly MonoGameDrawingBackend drawingBackend;
    private readonly PrismCacheInvalidationQueue prismCacheInvalidations = new();
    private readonly DrawCommandList replayCommands = new();
    private DrawCommandList retainedCommands = new();
    private DrawCommandList recordingCommands = new();
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
        drawingBackend = new MonoGameDrawingBackend(
            spriteBatch,
            whitePixel,
            new SkiaTextRasterizer(),
            new PrismRendererOptions(),
            retainedCacheEnabled: true,
            prismEnabled: false);
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

    internal PrismRendererDiagnostics PrismDiagnostics =>
        drawingBackend.RendererDiagnostics;

    public void Render(
        Action<RenderSurface2DFrame> draw,
        CernealaColor clearColor,
        TimeSpan frameTime,
        Action<IDrawImage>? trackImageDependency = null)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ObjectDisposedException.ThrowIf(disposed, this);

        recordingCommands.Clear();
        RenderSurface2DFrame frame = new(
            recordingCommands,
            new DrawRect(0, 0, PixelWidth, PixelHeight),
            frameTime,
            trackImageDependency);
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
            retainedCommands[prefixLength].Equals(
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
            retainedCommands[retainedSuffix].Equals(
                recordingCommands[recordingSuffix]))
        {
            retainedSuffix--;
            recordingSuffix--;
        }

        if (ContainsContextSensitiveCommand(retainedCommands) ||
            ContainsContextSensitiveCommand(recordingCommands))
        {
            return surfaceBounds;
        }

        Rectangle? damage = null;
        for (int index = prefixLength; index <= retainedSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                ResolveDamageBounds(retainedCommands[index], surfaceBounds),
                surfaceBounds);
        }
        for (int index = prefixLength; index <= recordingSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                ResolveDamageBounds(recordingCommands[index], surfaceBounds),
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

        replayCommands.Clear();
        replayCommands.Add(DrawCommand.PushClip(ToDrawRect(damageBounds)));
        foreach (DrawCommand command in recordingCommands)
        {
            if (!ResolveDamageBounds(command, surfaceBounds)
                .Intersects(damageBounds))
            {
                continue;
            }

            replayCommands.Add(command);
            LastReplayedCommandCount++;
        }
        replayCommands.Add(DrawCommand.PopClip());

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(replayCommands);
        if (!analysis.Scopes.IsEmpty)
        {
            drawingBackend.EnablePrism();
        }
        DrawingFrameContext frameContext = new(
            analysis,
            backdropLease: null,
            backdropSourceToken: default,
            prismCacheInvalidations);
        drawingBackend.Render(replayCommands, in frameContext);
    }

    private static bool ContainsContextSensitiveCommand(DrawCommandList commands)
    {
        foreach (DrawCommand command in commands)
        {
            if (command.Kind is DrawCommandKind.PushClip or
                DrawCommandKind.PopClip or
                DrawCommandKind.BeginPrism or
                DrawCommandKind.EndPrism)
            {
                return true;
            }
        }

        return false;
    }

    private static Rectangle ResolveDamageBounds(
        DrawCommand command,
        Rectangle surfaceBounds)
    {
        MonoGameDrawMapper mapper = new(1);
        return command.Kind switch
        {
            DrawCommandKind.FillRectangle or
            DrawCommandKind.DrawRectangle or
            DrawCommandKind.FillEllipse or
            DrawCommandKind.DrawEllipse or
            DrawCommandKind.FillPath => mapper.MapRectangle(command.Rect),
            DrawCommandKind.DrawImage
                when command.ImageRotation == 0 &&
                    command.ImageOrigin == default =>
                mapper.MapRectangle(command.Rect),
            DrawCommandKind.DrawLine => ResolveLineBounds(command),
            _ => surfaceBounds
        };
    }

    private static Rectangle ResolveLineBounds(DrawCommand command)
    {
        float radius = command.Thickness / 2;
        int left = (int)MathF.Floor(
            MathF.Min(command.Position.X, command.EndPoint.X) - radius);
        int top = (int)MathF.Floor(
            MathF.Min(command.Position.Y, command.EndPoint.Y) - radius);
        int right = (int)MathF.Ceiling(
            MathF.Max(command.Position.X, command.EndPoint.X) + radius);
        int bottom = (int)MathF.Ceiling(
            MathF.Max(command.Position.Y, command.EndPoint.Y) + radius);
        return new Rectangle(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private static DrawRect ToDrawRect(Rectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

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

        drawingBackend.Dispose();
        scissorRasterizerState.Dispose();
        spriteBatch.Dispose();
        whitePixel.Dispose();
        renderTarget.Dispose();
        disposed = true;
    }
}
