using Cerneala.UI.Controls;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal enum RenderSurface2DRetainedMissReason
{
    None,
    FirstFrame,
    ClearColorChanged,
    CommandCountChanged,
    CommandPayloadChanged,
    ContextSensitiveCommand
}

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
        renderTarget = CreateRenderTarget(
            graphicsDevice,
            pixelWidth,
            pixelHeight);
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

    internal RenderSurface2DRetainedMissReason LastRetainedMissReason { get; private set; }

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
        DrawCommandStateAnalysis recordingAnalysis =
            new DrawCommandStateAnalyzer().Analyze(recordingCommands);
        DrawCommandStateAnalysis? retainedAnalysis = hasRetainedFrame
            ? new DrawCommandStateAnalyzer().Analyze(retainedCommands)
            : null;
        Rectangle? damage = ResolveDamage(
            surfaceBounds,
            nextClearColor,
            retainedAnalysis,
            recordingAnalysis);
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
        XnaColor nextClearColor,
        DrawCommandStateAnalysis? retainedAnalysis,
        DrawCommandStateAnalysis recordingAnalysis)
    {
        if (!hasRetainedFrame || retainedClearColor != nextClearColor)
        {
            LastRetainedMissReason = !hasRetainedFrame
                ? RenderSurface2DRetainedMissReason.FirstFrame
                : RenderSurface2DRetainedMissReason.ClearColorChanged;
            return surfaceBounds;
        }

        int prefixLength = 0;
        int sharedLength = Math.Min(
            retainedCommands.Count,
            recordingCommands.Count);
        while (prefixLength < sharedLength &&
            RetainedEquals(
                retainedAnalysis!.Entries[prefixLength],
                recordingAnalysis.Entries[prefixLength]))
        {
            prefixLength++;
        }

        if (prefixLength == retainedCommands.Count &&
            prefixLength == recordingCommands.Count)
        {
            LastRetainedMissReason = RenderSurface2DRetainedMissReason.None;
            return null;
        }

        int retainedSuffix = retainedCommands.Count - 1;
        int recordingSuffix = recordingCommands.Count - 1;
        while (retainedSuffix >= prefixLength &&
            recordingSuffix >= prefixLength &&
            RetainedEquals(
                retainedAnalysis!.Entries[retainedSuffix],
                recordingAnalysis.Entries[recordingSuffix]))
        {
            retainedSuffix--;
            recordingSuffix--;
        }

        if (ContainsContextSensitiveCommand(retainedAnalysis!) ||
            ContainsContextSensitiveCommand(recordingAnalysis))
        {
            LastRetainedMissReason =
                RenderSurface2DRetainedMissReason.ContextSensitiveCommand;
            return surfaceBounds;
        }

        LastRetainedMissReason = retainedCommands.Count != recordingCommands.Count
            ? RenderSurface2DRetainedMissReason.CommandCountChanged
            : RenderSurface2DRetainedMissReason.CommandPayloadChanged;

        Rectangle? damage = null;
        for (int index = prefixLength; index <= retainedSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                ResolveDamageBounds(retainedAnalysis!.Entries[index], surfaceBounds),
                surfaceBounds);
        }
        for (int index = prefixLength; index <= recordingSuffix; index++)
        {
            damage = UnionDamage(
                damage,
                ResolveDamageBounds(recordingAnalysis.Entries[index], surfaceBounds),
                surfaceBounds);
        }

        return damage;
    }

    private void ReplayDamage(
        Rectangle surfaceBounds,
        Rectangle damageBounds,
        XnaColor clearColor)
    {
        RenderTargetBinding[] hostTargets = GraphicsDevice.GetRenderTargets();
        Viewport hostViewport = GraphicsDevice.Viewport;
        Rectangle hostScissor = GraphicsDevice.ScissorRectangle;
        try
        {
            ReplayDamageToSurface(surfaceBounds, damageBounds, clearColor);
        }
        finally
        {
            if (hostTargets.Length == 0)
            {
                GraphicsDevice.SetRenderTarget(null);
            }
            else
            {
                GraphicsDevice.SetRenderTargets(hostTargets);
            }

            GraphicsDevice.Viewport = hostViewport;
            GraphicsDevice.ScissorRectangle = hostScissor;
        }
    }

    private void ReplayDamageToSurface(
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
        DrawCommandStateAnalysis recordingAnalysis =
            new DrawCommandStateAnalyzer().Analyze(recordingCommands);
        for (int index = 0; index < recordingCommands.Count; index++)
        {
            if (!ResolveDamageBounds(
                    recordingAnalysis.Entries[index],
                    surfaceBounds)
                .Intersects(damageBounds))
            {
                continue;
            }

            replayCommands.Add(recordingCommands[index]);
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

    private static bool ContainsContextSensitiveCommand(
        DrawCommandStateAnalysis analysis)
    {
        foreach (DrawCommandStateEntry entry in analysis.Entries)
        {
            if (entry.IsContextSensitive)
            {
                return true;
            }
        }

        return false;
    }

    private static bool RetainedEquals(
        DrawCommandStateEntry left,
        DrawCommandStateEntry right) =>
        left.Metadata?.RetainedIdentity.Equals(
            right.Metadata?.RetainedIdentity) == true;

    private static Rectangle ResolveDamageBounds(
        DrawCommandStateEntry entry,
        Rectangle surfaceBounds)
    {
        if (entry.Bounds is not DrawRect bounds)
        {
            return surfaceBounds;
        }

        int left = (int)MathF.Floor(bounds.X);
        int top = (int)MathF.Floor(bounds.Y);
        int right = (int)MathF.Ceiling(bounds.Right);
        int bottom = (int)MathF.Ceiling(bounds.Bottom);
        return new Rectangle(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private static DrawRect ToDrawRect(Rectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    private static RenderTarget2D CreateRenderTarget(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        foreach (int sampleCount in new[] { 16, 8, 4, 2, 0 })
        {
            try
            {
                return new RenderTarget2D(
                    graphicsDevice,
                    pixelWidth,
                    pixelHeight,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Color,
                    preferredDepthFormat: DepthFormat.None,
                    preferredMultiSampleCount: sampleCount,
                    usage: RenderTargetUsage.PreserveContents);
            }
            catch when (sampleCount > 0)
            {
            }
        }

        throw new InvalidOperationException(
            "Could not create a RenderSurface2D render target.");
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

        drawingBackend.Dispose();
        scissorRasterizerState.Dispose();
        spriteBatch.Dispose();
        whitePixel.Dispose();
        renderTarget.Dispose();
        disposed = true;
    }
}
