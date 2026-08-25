using System.Buffers;
using System.Diagnostics;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Text;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend :
    IDrawingBackend,
    IDrawingBackendFrameTimingSource,
    IPrismCommandRenderer,
    IDisposable
{
    private const int TextSubpixelPhaseCount = 8;
    private const float EllipseCoverageCompensationPixels = 0.055f;
    private const int DefaultMaximumTextTextureEntries = 2_048;
    private const long DefaultMaximumTextTextureBytes = 256L * 1024 * 1024;

    private readonly SpriteBatch _spriteBatch;
    private readonly Dictionary<TextTextureKey, TextTexture> _textTextureCache = new();
    private readonly Dictionary<TextBrushTextureKey, Texture2D> textBrushTextureCache = new();
    private Dictionary<TextTextureKey, TextTextureCacheMetadata> textTextureCacheMetadata = new();
    private HashSet<TextTextureKey> activeTextTextureKeys = [];
    private List<TextTextureKey> textTextureEvictionCandidates = [];
    private readonly Dictionary<TextTextureKey, TextRasterizationRequest> textRasterizationRequests = new();
    private readonly Dictionary<TextTextureKey, RasterizedText[]> preparedTextRasterizations = new();
    private readonly HashSet<TextTextureKey> preparedTextTextureKeys = [];
    private readonly Dictionary<Texture2D, int> sharedTextTextureReferenceCounts =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<BrushTextureKey, Texture2D> brushTextureCache = new();
    private readonly HashSet<BrushTextureKey> activeBrushTextureKeys = [];
    private readonly List<BrushTextureKey> brushTextureEvictionCandidates = [];
    private readonly Dictionary<PathMeshKey, MonoGamePathMesh> pathMeshCache = new();
    private readonly Dictionary<StrokeMeshKey, MonoGameStrokeMesh> strokeMeshCache = new();
    private readonly HashSet<IDrawBrush> activeBrushes = new(ReferenceEqualityComparer.Instance);
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;
    private readonly RasterizerState? scissorRasterizerState;
    private readonly MonoGameGraphicsDeviceStateSnapshot? deviceStateSnapshot;
    private readonly PrismGraphBuilder prismGraphBuilder = new();
    private readonly PrismGraphOptimizer prismGraphOptimizer = new();
    private readonly PrismExecutionDiagnostics prismDiagnostics;
    private readonly PrismRendererOptions prismRendererOptions;
    private readonly bool prismRetainedCacheEnabled;
    private bool prismEnabled;
    private BasicEffect? pathEffect;
    private PrismGraphExecutor? prismExecutor;
    private RasterizerState? pathRasterizerState;
    private readonly BlendState redTextBlendState;
    private readonly BlendState greenTextBlendState;
    private readonly BlendState blueTextBlendState;
    private readonly BlendState textMaskBlendState;
    private readonly BlendState multiplyBlendState;
    private readonly BlendState screenBlendState;
    private readonly BlendState stencilWriteBlendState;
    private readonly DepthStencilState stencilWriteState;
    private readonly DepthStencilState stencilTestState;
    private readonly List<NumericsMatrix3x2> drawingTransforms =
        [NumericsMatrix3x2.Identity];
    private readonly List<DrawBlendMode> drawingBlends =
        [DrawBlendMode.Normal];
    private readonly Stack<bool> geometricClipScopes = new();
    private readonly List<DrawingLayerScope> drawingLayers = [];
    private readonly Stack<RenderTarget2D> drawingLayerPool = new();
    private long textTextureCacheHits;
    private long textTextureCacheMisses;
    private long textTextureCacheEvictions;
    private long textTextureCacheEstimatedBytes;
    private long textTextureCacheGeneration;
    private long textTextureCacheInsertionSequence;
    private float coordinateScale = 1;
    private bool disposed;
    private MonoGameClipStack? clipStack;
    private TimeSpan lastTextRequestCollectionTime;
    private TimeSpan lastTextRasterizationTime;
    private TimeSpan lastTextAtlasUploadTime;
    private int lastTextRequestCount;
    private long lastRasterizedPixelCount;
    private int lastAdvancedPrimitiveDrawCalls;
    private bool spriteBatchBegun;
    private SamplerState? activeSamplerState;
    private bool prismExecutorUnavailable;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
        : this(
            spriteBatch,
            whitePixel,
            textRasterizer,
            new PrismRendererOptions())
    {
    }

    public MonoGameDrawingBackend(
        SpriteBatch spriteBatch,
        Texture2D whitePixel,
        SkiaTextRasterizer? textRasterizer,
        PrismRendererOptions prismRendererOptions)
        : this(
            spriteBatch,
            whitePixel,
            textRasterizer,
            prismRendererOptions,
            retainedCacheEnabled: true)
    {
    }

    internal MonoGameDrawingBackend(
        SpriteBatch spriteBatch,
        Texture2D whitePixel,
        SkiaTextRasterizer? textRasterizer,
        PrismRendererOptions prismRendererOptions,
        bool retainedCacheEnabled)
        : this(
            spriteBatch,
            whitePixel,
            textRasterizer,
            prismRendererOptions,
            retainedCacheEnabled,
            prismEnabled: true)
    {
    }

    internal MonoGameDrawingBackend(
        SpriteBatch spriteBatch,
        Texture2D whitePixel,
        SkiaTextRasterizer? textRasterizer,
        PrismRendererOptions prismRendererOptions,
        bool retainedCacheEnabled,
        bool prismEnabled)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        this.prismRendererOptions = prismRendererOptions ??
            throw new ArgumentNullException(
                nameof(prismRendererOptions));
        prismRetainedCacheEnabled = retainedCacheEnabled;
        this.prismEnabled = prismEnabled;
        this.prismRendererOptions.Validate();
        prismDiagnostics = new PrismExecutionDiagnostics(
            this.prismRendererOptions.EnableDevelopmentDiagnostics);
        ValidateGraphicsResources(_spriteBatch, _whitePixel, nameof(whitePixel));
        _textRasterizer = textRasterizer;
        scissorRasterizerState = ScissorRasterizerState;
        deviceStateSnapshot = new MonoGameGraphicsDeviceStateSnapshot();
        redTextBlendState = CreateTextBlendState(ColorWriteChannels.Red);
        greenTextBlendState = CreateTextBlendState(ColorWriteChannels.Green);
        blueTextBlendState = CreateTextBlendState(ColorWriteChannels.Blue);
        textMaskBlendState = CreateTextMaskBlendState();
        multiplyBlendState = CreateMultiplyBlendState();
        screenBlendState = CreateScreenBlendState();
        stencilWriteBlendState = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None
        };
        stencilWriteState = new DepthStencilState
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1
        };
        stencilTestState = new DepthStencilState
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1
        };
        if (_spriteBatch.GraphicsDevice is GraphicsDevice graphicsDevice)
        {
            CreatePathResources(graphicsDevice);
            if (prismEnabled)
            {
                PrismColdStartWarmup.Begin();
                PrismExecutionColdStartWarmup.Begin();
                TryEnsurePrismExecutor(graphicsDevice);
                TryWarmUpPrism(graphicsDevice);
            }
            graphicsDevice.DeviceReset += OnDeviceReset;
        }
    }

    private void TryWarmUpPrism(GraphicsDevice graphicsDevice)
    {
        if (prismExecutor is null)
        {
            return;
        }

        Viewport originalViewport = graphicsDevice.Viewport;
        try
        {
            (DrawCommandList commands, PrismFrameAnalysis analysis) =
                PrismColdStartWarmup.CreateOuterGlowWorkload();
            graphicsDevice.Viewport = new Viewport(0, 0, 64, 64);
            DrawingFrameContext frameContext = new(analysis);
            Render(commands, frameContext);
            prismExecutor.InvalidateAll();
            prismDiagnostics.BeginFrame();
            LastFrameTiming = default;
        }
        catch
        {
            prismDiagnostics.BeginFrame();
        }
        finally
        {
            graphicsDevice.Viewport = originalViewport;
        }
    }

    public static RasterizerState ScissorRasterizerState => new() { ScissorTestEnable = true };

    public float CoordinateScale
    {
        get => coordinateScale;
        set
        {
            UiCoordinateMapper.ValidateScale(value);
            if (coordinateScale != value)
            {
                ClearTextTextureCaches();
                ClearBrushTextureCache();
                ClearPathMeshCache();
                prismExecutor?.InvalidateAll();
            }

            coordinateScale = value;
        }
    }

    public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);
        frameContext.EnsureCurrent(commands);
        frameContext.StateAnalysis.EnsureCurrent(commands);

        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice ??
            throw new InvalidOperationException("The SpriteBatch does not have a GraphicsDevice.");
        ConsumePrismCacheInvalidations(
            frameContext.PrismCacheInvalidations);
        MonoGameGraphicsDeviceStateSnapshot stateSnapshot = deviceStateSnapshot ??
            throw new InvalidOperationException("The backend graphics state snapshot is unavailable.");
        stateSnapshot.Capture(graphicsDevice);
        TimeSpan preparationTime = TimeSpan.Zero;
        TimeSpan commandRenderingTime = TimeSpan.Zero;
        long commandRenderingStarted = 0;
        bool commandRenderingStartedValid = false;

        try
        {
            Viewport viewport = graphicsDevice.Viewport;
            Rectangle viewportClip = new(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            graphicsDevice.ScissorRectangle = viewportClip;
            clipStack = new MonoGameClipStack(viewportClip);
            ResetDrawingState();
            activeTextTextureKeys.Clear();
            activeBrushTextureKeys.Clear();
            lastTextRequestCollectionTime = TimeSpan.Zero;
            lastTextRasterizationTime = TimeSpan.Zero;
            lastTextAtlasUploadTime = TimeSpan.Zero;
            lastTextRequestCount = 0;
            lastRasterizedPixelCount = 0;
            lastAdvancedPrimitiveDrawCalls = 0;

            long preparationStarted = Stopwatch.GetTimestamp();
            PrepareTextRasterizations(commands);
            preparationTime = Stopwatch.GetElapsedTime(preparationStarted);

            prismDiagnostics.BeginFrame();
            BeginUiSpriteBatch();
            commandRenderingStarted = Stopwatch.GetTimestamp();
            commandRenderingStartedValid = true;
            if (frameContext.PrismAnalysis.Scopes.IsEmpty)
            {
                for (int index = 0; index < commands.Count; index++)
                {
                    RenderCommand(commands[index]);
                }
            }
            else
            {
                var backdropLease = frameContext.BackdropLease;
                PrismGraph graph = backdropLease is null
                    ? prismGraphBuilder.Build(frameContext.PrismAnalysis)
                    : prismGraphBuilder.Build(
                        frameContext.PrismAnalysis,
                        backdropLease.Metadata,
                        frameContext.BackdropSourceToken);
                PrismGraphExecutionPlan executionPlan =
                    prismGraphOptimizer.Optimize(graph);
                if (TryEnsurePrismExecutor(graphicsDevice))
                {
                    prismExecutor!.Execute(
                        commands,
                        frameContext.PrismAnalysis,
                        executionPlan,
                        this,
                        viewport,
                        backdropLease);
                }
                else
                {
                    for (int index = 0; index < commands.Count; index++)
                    {
                        RenderCommand(commands[index]);
                    }
                }
            }

            commandRenderingTime = Stopwatch.GetElapsedTime(commandRenderingStarted);
            commandRenderingStartedValid = false;
        }
        finally
        {
            if (commandRenderingStartedValid)
            {
                commandRenderingTime = Stopwatch.GetElapsedTime(commandRenderingStarted);
            }

            try
            {
                try
                {
                    EndSpriteBatch();
                }
                finally
                {
                    long cleanupStarted = Stopwatch.GetTimestamp();
                    AbortDrawingLayers(graphicsDevice);
                    clipStack?.Reset();
                    foreach (RasterizedText[] layers in preparedTextRasterizations.Values)
                    {
                        ReturnRasterizedTextPixels(layers);
                    }
                    preparedTextRasterizations.Clear();
                    preparedTextTextureKeys.Clear();
                    CompleteTextTextureFrame(
                        DefaultMaximumTextTextureEntries,
                        DefaultMaximumTextTextureBytes);
                    CompleteBrushTextureFrame();
                    LastFrameTiming = new DrawingBackendFrameTiming(
                        preparationTime,
                        lastTextRequestCollectionTime,
                        lastTextRasterizationTime,
                        lastTextAtlasUploadTime,
                        commandRenderingTime,
                        Stopwatch.GetElapsedTime(cleanupStarted),
                        lastTextRequestCount,
                        lastRasterizedPixelCount);
                }
            }
            finally
            {
                stateSnapshot.Restore(graphicsDevice);
            }
        }
    }

    internal int LastAdvancedPrimitiveDrawCalls =>
        lastAdvancedPrimitiveDrawCalls;

    private void BeginSpriteBatch(
        SpriteSortMode sortMode,
        BlendState blendState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        RasterizerState rasterizerState,
        Effect? effect = null,
        Matrix? transformMatrix = null)
    {
        if (spriteBatchBegun)
        {
            throw new InvalidOperationException("The backend SpriteBatch is already active.");
        }

        _spriteBatch.Begin(
            sortMode,
            blendState,
            samplerState,
            depthStencilState,
            rasterizerState,
            effect,
            transformMatrix ?? CurrentSpriteTransform);
        activeSamplerState = samplerState;
        spriteBatchBegun = true;
    }

    private void EndSpriteBatch()
    {
        if (!spriteBatchBegun)
        {
            return;
        }

        try
        {
            _spriteBatch.End();
        }
        finally
        {
            spriteBatchBegun = false;
            activeSamplerState = null;
        }
    }

    private void BeginUiSpriteBatch()
    {
        BeginSpriteBatch(
            SpriteSortMode.Immediate,
            ResolveBlendState(drawingBlends[^1]),
            SamplerState.LinearClamp,
            CurrentDepthStencilState,
            scissorRasterizerState!);
    }

    private void RestartUiSpriteBatch()
    {
        EndSpriteBatch();
        BeginUiSpriteBatch();
    }

    private void RenderCommand(DrawCommand command)
    {
        EnsureCommandSampler(command);
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                if (command.Brush is null)
                {
                    FillRectangle(command.Rect, command.Color);
                }
                else
                {
                    FillRectangle(command.Rect, command.Brush, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.DrawRectangle:
                if (command.Pen is not null)
                {
                    DrawStroke(command);
                }
                else if (command.Brush is null)
                {
                    DrawRectangle(command.Rect, command.Color, command.Thickness);
                }
                else
                {
                    DrawRectangle(command.Rect, command.Brush, command.Thickness, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.FillRoundedRectangle:
                if (CanFillAsPath(command))
                {
                    FillPath(command);
                }
                else
                {
                    FillRoundedRectangle(command);
                }
                break;

            case DrawCommandKind.DrawRoundedRectangle:
                DrawStroke(command);
                break;

            case DrawCommandKind.FillEllipse:
                if (CanFillAsPath(command))
                {
                    FillEllipsePath(command);
                }
                else if (command.Brush is null)
                {
                    FillEllipse(command.Rect, command.Color);
                }
                else
                {
                    FillEllipse(command.Rect, command.Brush, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.DrawEllipse:
                if (command.Pen is not null)
                {
                    DrawStroke(command);
                }
                else if (command.Brush is null)
                {
                    DrawEllipse(command.Rect, command.Color, command.Thickness);
                }
                else
                {
                    DrawEllipse(command.Rect, command.Brush, command.Thickness, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.DrawLine:
                if (command.Pen is not null)
                {
                    DrawStroke(command);
                }
                else if (command.Brush is null)
                {
                    DrawLine(command.Position, command.EndPoint, command.Color, command.Thickness);
                }
                else
                {
                    DrawLine(command.Position, command.EndPoint, command.Brush, command.Thickness, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.FillPath:
                FillPath(command);
                break;

            case DrawCommandKind.DrawPath:
                DrawStroke(command);
                break;

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawImageQuad:
            case DrawCommandKind.DrawNineSlice:
            case DrawCommandKind.DrawMesh:
            case DrawCommandKind.DrawPointBatch:
            case DrawCommandKind.DrawLineBatch:
            case DrawCommandKind.DrawSpriteBatch:
                DrawAdvancedMesh(command);
                break;

            case DrawCommandKind.RenderSurface2D:
                DrawRenderSurface2D(command);
                break;

            case DrawCommandKind.DrawText:
                DrawText(command);
                break;

            case DrawCommandKind.DrawTextLayout:
                DrawTextLayout(command);
                break;

            case DrawCommandKind.PushClip:
                PushTransformedClip(command.Rect);
                break;

            case DrawCommandKind.PushPathClip:
                PushGeometricClip(command);
                break;

            case DrawCommandKind.PopClip:
                PopDrawingClip();
                break;

            case DrawCommandKind.PushTransform:
                drawingTransforms.Add(NumericsMatrix3x2.Multiply(
                    command.Transform,
                    drawingTransforms[^1]));
                RestartUiSpriteBatch();
                break;

            case DrawCommandKind.PopTransform:
                drawingTransforms.RemoveAt(drawingTransforms.Count - 1);
                RestartUiSpriteBatch();
                break;

            case DrawCommandKind.PushOpacity:
                BeginDrawingLayer(command.Opacity, DrawBlendMode.Normal, isGeometricClip: false);
                break;

            case DrawCommandKind.PopOpacity:
                EndDrawingLayer();
                break;

            case DrawCommandKind.PushBlend:
                drawingBlends.Add(command.BlendMode);
                RestartUiSpriteBatch();
                break;

            case DrawCommandKind.PopBlend:
                drawingBlends.RemoveAt(drawingBlends.Count - 1);
                RestartUiSpriteBatch();
                break;

            case DrawCommandKind.PushLayer:
                BeginDrawingLayer(
                    command.LayerOptions!.Opacity,
                    command.LayerOptions.BlendMode,
                    isGeometricClip: false);
                break;

            case DrawCommandKind.PopLayer:
                EndDrawingLayer();
                break;

            case DrawCommandKind.BeginPrism:
            case DrawCommandKind.EndPrism:
                break;

            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void FillRectangle(DrawRect rect, CernealaColor color)
    {
        _spriteBatch.Draw(_whitePixel, Mapper.MapRectangle(rect), Premultiply(ToColor(color)));
    }

    private void FillRectangle(DrawRect rect, IDrawBrush brush, float commandOpacity)
    {
        DrawBrushDescriptor descriptor = brush.CreateDescriptor();
        if (TryGetSolidColor(descriptor, commandOpacity, out XnaColor solid))
        {
            _spriteBatch.Draw(_whitePixel, Mapper.MapRectangle(rect), Premultiply(solid));
            return;
        }

        if (descriptor is ImageDrawBrushDescriptor image)
        {
            DrawImageBrush(rect, image, commandOpacity);
            return;
        }

        if (descriptor is DrawingDrawBrushDescriptor drawing)
        {
            ValidateBrushGraphForDiagnostics(brush);
            DrawCommandBrush(rect, brush, drawing.Commands, drawing.ContentBounds, drawing, commandOpacity);
            return;
        }

        if (descriptor is VisualDrawBrushDescriptor visual)
        {
            ValidateBrushGraphForDiagnostics(brush);
            DrawCommandBrush(rect, brush, visual.Commands, visual.ContentBounds, visual, commandOpacity);
            return;
        }

        Texture2D texture = GetOrCreateBrushTexture(brush, descriptor, rect);
        _spriteBatch.Draw(texture, Mapper.MapRectangle(rect), OpacityTint(commandOpacity));
    }

    private void DrawRectangle(DrawRect rect, CernealaColor color, float thickness)
    {
        int lineThickness = Mapper.MapThickness(thickness);
        Rectangle bounds = Mapper.MapRectangle(rect);
        XnaColor monoGameColor = Premultiply(ToColor(color));

        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Bottom - lineThickness, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, lineThickness, bounds.Height), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - lineThickness, bounds.Top, lineThickness, bounds.Height), monoGameColor);
    }

    private void DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness, float commandOpacity)
    {
        float safeThickness = MathF.Min(thickness, MathF.Min(rect.Width, rect.Height) / 2);
        if (safeThickness <= 0)
        {
            return;
        }

        FillRectangle(new DrawRect(rect.X, rect.Y, rect.Width, safeThickness), brush, commandOpacity);
        FillRectangle(new DrawRect(rect.X, rect.Bottom - safeThickness, rect.Width, safeThickness), brush, commandOpacity);
        FillRectangle(new DrawRect(rect.X, rect.Y + safeThickness, safeThickness, MathF.Max(0, rect.Height - (safeThickness * 2))), brush, commandOpacity);
        FillRectangle(new DrawRect(rect.Right - safeThickness, rect.Y + safeThickness, safeThickness, MathF.Max(0, rect.Height - (safeThickness * 2))), brush, commandOpacity);
    }

    private static bool CanFillAsPath(DrawCommand command) =>
        command.Path is not null &&
        (command.Brush is null ||
            command.Brush.CreateDescriptor() is SolidDrawBrushDescriptor);

    private void FillEllipsePath(DrawCommand command)
    {
        float compensation = EllipseCoverageCompensationPixels / coordinateScale;
        FillPath(
            command,
            new DrawRect(
                command.Rect.X - compensation,
                command.Rect.Y - compensation,
                command.Rect.Width + (compensation * 2),
                command.Rect.Height + (compensation * 2)));
    }

    private void FillRoundedRectangle(DrawCommand command)
    {
        Rectangle bounds = Mapper.MapRectangle(command.Rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawCornerRadius radii = command.CornerRadius.Normalize(command.Rect);
        if (command.Brush is null)
        {
            XnaColor color = Premultiply(ToColor(command.Color));
            DrawRoundedRows(
                bounds,
                radii,
                (row, source) => _spriteBatch.Draw(_whitePixel, row, color));
            return;
        }

        DrawBrushDescriptor descriptor = command.Brush.CreateDescriptor();
        if (TryGetSolidColor(descriptor, command.BrushOpacity, out XnaColor solid))
        {
            XnaColor color = Premultiply(solid);
            DrawRoundedRows(
                bounds,
                radii,
                (row, source) => _spriteBatch.Draw(_whitePixel, row, color));
            return;
        }

        Texture2D texture = GetOrCreateBrushTexture(command.Brush, descriptor, command.Rect);
        XnaColor tint = OpacityTint(command.BrushOpacity);
        DrawRoundedRows(
            bounds,
            radii,
            (row, source) => _spriteBatch.Draw(texture, row, source, tint));
    }

    private void DrawRoundedRows(
        Rectangle bounds,
        DrawCornerRadius radii,
        Action<Rectangle, Rectangle> drawRow)
    {
        float scale = coordinateScale;
        float topLeft = radii.TopLeft * scale;
        float topRight = radii.TopRight * scale;
        float bottomRight = radii.BottomRight * scale;
        float bottomLeft = radii.BottomLeft * scale;
        for (int y = 0; y < bounds.Height; y++)
        {
            float sampleY = y + 0.5f;
            int leftInset = RoundedCornerInset(
                sampleY,
                bounds.Height,
                topLeft,
                bottomLeft);
            int rightInset = RoundedCornerInset(
                sampleY,
                bounds.Height,
                topRight,
                bottomRight);
            int width = bounds.Width - leftInset - rightInset;
            if (width <= 0)
            {
                continue;
            }

            Rectangle source = new(leftInset, y, width, 1);
            drawRow(
                new Rectangle(bounds.Left + leftInset, bounds.Top + y, width, 1),
                source);
        }
    }

    private static int RoundedCornerInset(
        float sampleY,
        float height,
        float topRadius,
        float bottomRadius)
    {
        float radius;
        float distanceFromCenter;
        if (topRadius > 0 && sampleY < topRadius)
        {
            radius = topRadius;
            distanceFromCenter = sampleY - radius;
        }
        else if (bottomRadius > 0 && sampleY > height - bottomRadius)
        {
            radius = bottomRadius;
            distanceFromCenter = sampleY - (height - radius);
        }
        else
        {
            return 0;
        }

        float horizontalSpan = MathF.Sqrt(MathF.Max(
            0,
            (radius * radius) - (distanceFromCenter * distanceFromCenter)));
        return Math.Max(0, (int)MathF.Ceiling(radius - horizontalSpan));
    }

    private void FillEllipse(DrawRect rect, CernealaColor color)
    {
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        XnaColor monoGameColor = Premultiply(ToColor(color));
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerY = bounds.Top + radiusY;

        for (int y = 0; y < bounds.Height; y++)
        {
            float normalizedY = ((bounds.Top + y + 0.5f) - centerY) / radiusY;
            float span = MathF.Sqrt(MathF.Max(0, 1 - (normalizedY * normalizedY))) * radiusX;
            int left = (int)MathF.Round(bounds.Left + radiusX - span);
            int right = (int)MathF.Round(bounds.Left + radiusX + span);
            int width = Math.Max(1, right - left);
            _spriteBatch.Draw(_whitePixel, new Rectangle(left, bounds.Top + y, width, 1), monoGameColor);
        }
    }

    private void FillEllipse(DrawRect rect, IDrawBrush brush, float commandOpacity)
    {
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawBrushDescriptor descriptor = brush.CreateDescriptor();
        if (TryGetSolidColor(descriptor, commandOpacity, out XnaColor solid))
        {
            FillEllipse(rect, new CernealaColor(solid.R, solid.G, solid.B, solid.A));
            return;
        }

        Texture2D texture = GetOrCreateBrushTexture(brush, descriptor, rect);
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerY = bounds.Top + radiusY;
        XnaColor tint = OpacityTint(commandOpacity);
        for (int y = 0; y < bounds.Height; y++)
        {
            float normalizedY = ((bounds.Top + y + 0.5f) - centerY) / radiusY;
            float span = MathF.Sqrt(MathF.Max(0, 1 - (normalizedY * normalizedY))) * radiusX;
            int left = Math.Clamp((int)MathF.Round(radiusX - span), 0, bounds.Width - 1);
            int right = Math.Clamp((int)MathF.Round(radiusX + span), left + 1, bounds.Width);
            int width = right - left;
            _spriteBatch.Draw(
                texture,
                new Rectangle(bounds.Left + left, bounds.Top + y, width, 1),
                new Rectangle(left, y, width, 1),
                tint);
        }
    }

    private void DrawEllipse(DrawRect rect, CernealaColor color, float thickness)
    {
        int lineThickness = Mapper.MapThickness(thickness);
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawEllipseRing(bounds, ToColor(color), lineThickness);
    }

    private void DrawEllipse(DrawRect rect, IDrawBrush brush, float thickness, float commandOpacity)
    {
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float radiusX = rect.Width / 2f;
        float radiusY = rect.Height / 2f;
        DrawPoint center = new(rect.X + radiusX, rect.Y + radiusY);
        int segments = Math.Max(24, (int)MathF.Ceiling(MathF.PI * MathF.Max(bounds.Width, bounds.Height) / 2f));
        DrawPoint previous = new(center.X + radiusX, center.Y);
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            DrawPoint next = new(center.X + (MathF.Cos(angle) * radiusX), center.Y + (MathF.Sin(angle) * radiusY));
            DrawLine(previous, next, brush, thickness, commandOpacity);
            previous = next;
        }
    }

    private void DrawEllipseRing(Rectangle bounds, XnaColor color, int thickness)
    {
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerX = bounds.Left + radiusX;
        float centerY = bounds.Top + radiusY;
        int segments = Math.Max(24, (int)MathF.Ceiling(MathF.PI * MathF.Max(radiusX, radiusY) / 2f));
        Vector2 previous = new(centerX + radiusX, centerY);

        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = new(centerX + (MathF.Cos(angle) * radiusX), centerY + (MathF.Sin(angle) * radiusY));
            DrawLine(previous, next, color, thickness);
            previous = next;
        }
    }

    private void DrawLine(DrawPoint start, DrawPoint end, CernealaColor color, float thickness)
    {
        DrawLine(Mapper.MapVector(start), Mapper.MapVector(end), ToColor(color), Mapper.MapThickness(thickness));
    }

    private void DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness, float commandOpacity)
    {
        DrawBrushDescriptor descriptor = brush.CreateDescriptor();
        if (TryGetSolidColor(descriptor, commandOpacity, out XnaColor solid))
        {
            DrawLine(Mapper.MapVector(start), Mapper.MapVector(end), solid, Mapper.MapThickness(thickness));
            return;
        }

        Vector2 startPixels = Mapper.MapVector(start);
        Vector2 endPixels = Mapper.MapVector(end);
        float length = Vector2.Distance(startPixels, endPixels);
        int segments = Math.Clamp((int)MathF.Ceiling(length / 2), 1, 1024);
        Vector2 previous = startPixels;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 next = Vector2.Lerp(startPixels, endPixels, t);
            DrawPoint logical = new(start.X + ((end.X - start.X) * (t - (0.5f / segments))), start.Y + ((end.Y - start.Y) * (t - (0.5f / segments))));
            XnaColor color = ToColor(ApplyOpacity(Sample(descriptor, logical), commandOpacity));
            DrawLine(previous, next, color, Mapper.MapThickness(thickness));
            previous = next;
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, XnaColor color, int thickness)
    {
        color = Premultiply(color);
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0)
        {
            _spriteBatch.Draw(
                _whitePixel,
                new Rectangle(
                    (int)MathF.Round(start.X - (thickness / 2f)),
                    (int)MathF.Round(start.Y - (thickness / 2f)),
                    thickness,
                    thickness),
                color);
            return;
        }

        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _whitePixel,
            start,
            null,
            color,
            angle,
            new Vector2(0, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0);
    }

    private void DrawImage(DrawCommand command)
    {
        if (command.Image is not MonoGameImage image)
        {
            throw new InvalidOperationException("DrawImage requires a MonoGameImage when using MonoGameDrawingBackend.");
        }

        ObjectDisposedException.ThrowIf(image.Texture.IsDisposed, image);
        if (!ReferenceEquals(image.Texture.GraphicsDevice, _spriteBatch.GraphicsDevice))
        {
            throw new InvalidOperationException("A MonoGameImage can only be drawn by the GraphicsDevice that created it.");
        }

        DrawImageOptions options = command.ImageOptions ??
            new DrawImageOptions(
                command.ImageSource,
                command.Color,
                rotation: command.ImageRotation,
                origin: command.ImageOrigin,
                flip: command.ImageFlip,
                layerDepth: command.LayerDepth);
        DrawRect source = DrawImageGeometry.ResolveSource(image, options);

        _spriteBatch.Draw(
            image.Texture,
            Mapper.MapRectangle(command.Rect),
            MapImageSource(source),
            Premultiply(ToColor(DrawImageGeometry.EffectiveTint(options))),
            options.Rotation,
            new Vector2(options.Origin.X, options.Origin.Y),
            ToSpriteEffects(options.Flip),
            options.LayerDepth);
    }

    private void DrawAdvancedMesh(DrawCommand command)
    {
        DrawMesh2D mesh = command.Mesh ??
            throw new InvalidOperationException(
                $"{command.Kind} has no mesh payload.");
        Texture2D? texture = null;
        if (mesh.Image is not null)
        {
            if (mesh.Image is not MonoGameImage image)
            {
                throw new InvalidOperationException(
                    $"{command.Kind} requires a MonoGameImage when its mesh is textured.");
            }
            ObjectDisposedException.ThrowIf(image.Texture.IsDisposed, image);
            if (!ReferenceEquals(
                    image.Texture.GraphicsDevice,
                    _spriteBatch.GraphicsDevice))
            {
                throw new InvalidOperationException(
                    "A textured mesh can only be drawn by the GraphicsDevice that created its image.");
            }

            texture = image.Texture;
        }

        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        CreatePathResources(device);
        BlendState previousBlend = device.BlendState;
        SamplerState previousSampler = device.SamplerStates[0];
        DepthStencilState previousDepth = device.DepthStencilState;
        RasterizerState previousRasterizer = device.RasterizerState;
        VertexPositionColorTexture[] vertices =
            ArrayPool<VertexPositionColorTexture>.Shared.Rent(
                mesh.VertexArray.Length);

        EndSpriteBatch();
        try
        {
            for (int index = 0; index < mesh.VertexArray.Length; index++)
            {
                DrawVertex2D vertex = mesh.VertexArray[index];
                Vector2 position = Mapper.MapVector(vertex.Position);
                vertices[index] = new VertexPositionColorTexture(
                    new Vector3(position.X, position.Y, command.LayerDepth),
                    Premultiply(ToColor(vertex.Color)),
                    new Vector2(
                        vertex.TextureCoordinate.X,
                        vertex.TextureCoordinate.Y));
            }

            device.BlendState = previousBlend;
            device.SamplerStates[0] = ResolveSamplerState(
                command.ImageOptions?.Sampling ?? DrawSamplingMode.Linear,
                command.ImageOptions?.AddressMode ?? DrawAddressMode.Clamp);
            device.DepthStencilState = previousDepth;
            device.RasterizerState = pathRasterizerState!;
            pathEffect!.VertexColorEnabled = true;
            pathEffect.TextureEnabled = texture is not null;
            pathEffect.Texture = texture;
            pathEffect.World = CurrentSpriteTransform;
            pathEffect.View = Matrix.Identity;
            pathEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0,
                device.Viewport.Width,
                device.Viewport.Height,
                0,
                0,
                1);

            PrimitiveType primitiveType = mesh.Topology switch
            {
                DrawPrimitiveTopology.TriangleList => PrimitiveType.TriangleList,
                DrawPrimitiveTopology.TriangleStrip => PrimitiveType.TriangleStrip,
                _ => throw new ArgumentOutOfRangeException(nameof(mesh.Topology))
            };
            int primitiveCount = mesh.Topology == DrawPrimitiveTopology.TriangleList
                ? mesh.IndexArray.Length / 3
                : mesh.IndexArray.Length - 2;
            foreach (EffectPass pass in pathEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(
                    primitiveType,
                    vertices,
                    0,
                    mesh.VertexArray.Length,
                    mesh.IndexArray,
                    0,
                    primitiveCount);
                lastAdvancedPrimitiveDrawCalls++;
            }
        }
        finally
        {
            pathEffect!.TextureEnabled = false;
            pathEffect.Texture = null;
            ArrayPool<VertexPositionColorTexture>.Shared.Return(vertices);
            BeginSpriteBatch(
                SpriteSortMode.Immediate,
                previousBlend,
                previousSampler,
                previousDepth,
                previousRasterizer);
        }
    }

    private void EnsureCommandSampler(DrawCommand command)
    {
        SamplerState desired = command.ImageOptions is DrawImageOptions options &&
            command.Kind is DrawCommandKind.DrawImage or
                DrawCommandKind.DrawImageQuad or
                DrawCommandKind.DrawNineSlice or
                DrawCommandKind.DrawMesh or
                DrawCommandKind.DrawSpriteBatch
            ? ResolveSamplerState(options.Sampling, options.AddressMode)
            : SamplerState.LinearClamp;
        if (!spriteBatchBegun || ReferenceEquals(activeSamplerState, desired))
        {
            return;
        }

        EndSpriteBatch();
        BeginSpriteBatch(
            SpriteSortMode.Immediate,
            ResolveBlendState(drawingBlends[^1]),
            desired,
            CurrentDepthStencilState,
            scissorRasterizerState!);
    }

    private static SamplerState ResolveSamplerState(
        DrawSamplingMode sampling,
        DrawAddressMode addressMode) =>
        (sampling, addressMode) switch
        {
            (DrawSamplingMode.Point, DrawAddressMode.Clamp) =>
                SamplerState.PointClamp,
            (DrawSamplingMode.Point, DrawAddressMode.Wrap) =>
                SamplerState.PointWrap,
            (DrawSamplingMode.Linear, DrawAddressMode.Clamp) =>
                SamplerState.LinearClamp,
            (DrawSamplingMode.Linear, DrawAddressMode.Wrap) =>
                SamplerState.LinearWrap,
            _ => throw new ArgumentOutOfRangeException(nameof(sampling))
        };

    private static Rectangle MapImageSource(DrawRect source)
    {
        int left = (int)MathF.Floor(source.X);
        int top = (int)MathF.Floor(source.Y);
        int right = (int)MathF.Ceiling(source.Right);
        int bottom = (int)MathF.Ceiling(source.Bottom);
        return new Rectangle(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private void DrawRenderSurface2D(DrawCommand command)
    {
        if (command.RenderSurface is not IRenderSurface2DFrameSource surface)
        {
            throw new InvalidOperationException(
                "RenderSurface2D requires a frame-recording surface source.");
        }

        Rectangle destination = Mapper.MapRectangle(command.Rect);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        EndSpriteBatch();
        MonoGameGraphicsDeviceStateSnapshot snapshot = new();
        snapshot.Capture(graphicsDevice);
        Texture2D texture;
        try
        {
            MonoGameRenderSurface2DSession? session =
                surface.GetBackendState(graphicsDevice) as
                    MonoGameRenderSurface2DSession;
            if (session is null ||
                session.IsDisposed ||
                !ReferenceEquals(session.GraphicsDevice, graphicsDevice) ||
                session.PixelWidth != destination.Width ||
                session.PixelHeight != destination.Height)
            {
                session = new MonoGameRenderSurface2DSession(
                    graphicsDevice,
                    destination.Width,
                    destination.Height);
                surface.SetBackendState(graphicsDevice, session);
            }

            if (session.RenderedFrameVersion != surface.FrameVersion)
            {
                session.Render(
                    surface.RecordFrame,
                    surface.ClearColor,
                    surface.FrameVersion);
            }

            texture = session.Surface;
        }
        finally
        {
            snapshot.Restore(graphicsDevice);
            BeginUiSpriteBatch();
        }

        if (!ReferenceEquals(texture.GraphicsDevice, graphicsDevice))
        {
            throw new InvalidOperationException(
                "A RenderSurface2D texture can only be drawn by the GraphicsDevice that created it.");
        }

        _spriteBatch.Draw(
            texture,
            destination,
            Premultiply(ToColor(command.Color)));
    }

    private void FillPath(
        DrawCommand command,
        DrawRect? destinationOverride = null)
    {
        DrawRect destinationRect = destinationOverride ?? command.Rect;
        float physicalLeft = destinationRect.X * coordinateScale;
        float physicalTop = destinationRect.Y * coordinateScale;
        float physicalRight = destinationRect.Right * coordinateScale;
        float physicalBottom = destinationRect.Bottom * coordinateScale;
        int left = (int)MathF.Floor(physicalLeft);
        int top = (int)MathF.Floor(physicalTop);
        int right = (int)MathF.Ceiling(physicalRight);
        int bottom = (int)MathF.Ceiling(physicalBottom);
        Rectangle destination = new(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        if (destination.Width <= 0 || destination.Height <= 0 || command.Path is null)
        {
            return;
        }

        XnaColor color;
        if (command.Brush is null)
        {
            color = ToColor(command.Color);
        }
        else if (!TryGetSolidColor(command.Brush.CreateDescriptor(), command.BrushOpacity, out color))
        {
            throw new NotSupportedException("Filled paths currently require a solid brush.");
        }

        float phaseX = physicalLeft - left;
        float phaseY = physicalTop - top;
        float physicalWidth = physicalRight - physicalLeft;
        float physicalHeight = physicalBottom - physicalTop;
        PathMeshKey key = new(
            command.Path.StableId,
            command.FillRule,
            command.SourceRect,
            destination.Width,
            destination.Height,
            physicalWidth,
            physicalHeight,
            phaseX,
            phaseY,
            color);
        if (!pathMeshCache.TryGetValue(key, out MonoGamePathMesh? mesh))
        {
            mesh = MonoGamePathMeshBuilder.Build(
                command.Path,
                command.SourceRect,
                physicalWidth,
                physicalHeight,
                phaseX,
                phaseY,
                Premultiply(color),
                command.FillRule);
            pathMeshCache.Add(key, mesh);
        }

        if (!mesh.IsEmpty)
        {
            DrawPathMesh(mesh, left, top);
        }
    }

    private void DrawStroke(DrawCommand command)
    {
        DrawPen? pen = command.Pen;
        DrawStrokeStyle style = pen?.Style ?? DrawStrokeStyle.Default;
        float thickness = pen?.Thickness ?? command.Thickness;
        Func<DrawPoint, XnaColor> colorSelector;
        if (pen is null)
        {
            XnaColor color = Premultiply(ToColor(command.Color));
            colorSelector = _ => color;
        }
        else
        {
            DrawBrushDescriptor descriptor = pen.Brush.CreateDescriptor();
            if (descriptor is not SolidDrawBrushDescriptor and
                not LinearGradientDrawBrushDescriptor and
                not RadialGradientDrawBrushDescriptor)
            {
                throw new NotSupportedException(
                    "Native strokes currently support solid and gradient brushes.");
            }
            colorSelector = point => Premultiply(ToColor(ApplyOpacity(
                SampleInBounds(descriptor, command.Rect, point),
                command.BrushOpacity)));
        }

        StrokeMeshKey key = StrokeMeshKey.From(command, coordinateScale);
        if (!strokeMeshCache.TryGetValue(key, out MonoGameStrokeMesh stroke))
        {
            stroke = MonoGameStrokeMeshBuilder.Build(
                command,
                thickness,
                style,
                coordinateScale,
                colorSelector);
            strokeMeshCache.Add(key, stroke);
        }

        if (!stroke.Mesh.IsEmpty)
        {
            DrawPathMesh(stroke.Mesh, stroke.Left, stroke.Top);
        }
    }

    private void DrawPathMask(DrawCommand command)
    {
        if (command.Path is null)
        {
            throw new InvalidOperationException(
                "A geometric clip requires a path payload.");
        }

        float physicalLeft = command.Rect.X * coordinateScale;
        float physicalTop = command.Rect.Y * coordinateScale;
        int left = (int)MathF.Floor(physicalLeft);
        int top = (int)MathF.Floor(physicalTop);
        float physicalWidth = command.Rect.Width * coordinateScale;
        float physicalHeight = command.Rect.Height * coordinateScale;
        int width = Math.Max(1, (int)MathF.Ceiling(command.Rect.Right * coordinateScale) - left);
        int height = Math.Max(1, (int)MathF.Ceiling(command.Rect.Bottom * coordinateScale) - top);
        XnaColor color = XnaColor.White;
        PathMeshKey key = new(
            command.Path.StableId,
            command.FillRule,
            command.SourceRect,
            width,
            height,
            physicalWidth,
            physicalHeight,
            physicalLeft - left,
            physicalTop - top,
            color);
        if (!pathMeshCache.TryGetValue(key, out MonoGamePathMesh? mesh))
        {
            mesh = MonoGamePathMeshBuilder.Build(
                command.Path,
                command.SourceRect,
                physicalWidth,
                physicalHeight,
                physicalLeft - left,
                physicalTop - top,
                color,
                command.FillRule);
            pathMeshCache.Add(key, mesh);
        }

        if (!mesh.IsEmpty)
        {
            DrawPathMesh(
                mesh,
                left,
                top,
                stencilWriteBlendState,
                stencilWriteState);
        }
    }

    private void DrawPathMesh(
        MonoGamePathMesh mesh,
        int left,
        int top,
        BlendState? blendState = null,
        DepthStencilState? depthStencilState = null)
    {
        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        CreatePathResources(device);
        BlendState previousBlend = device.BlendState;
        SamplerState previousSampler = device.SamplerStates[0];
        DepthStencilState previousDepth = device.DepthStencilState;
        RasterizerState previousRasterizer = device.RasterizerState;

        EndSpriteBatch();
        try
        {
            device.BlendState = blendState ?? BlendState.AlphaBlend;
            device.DepthStencilState = depthStencilState ?? DepthStencilState.None;
            device.RasterizerState = pathRasterizerState!;
            pathEffect!.World =
                Matrix.CreateTranslation(left, top, 0) *
                CurrentSpriteTransform;
            pathEffect.View = Matrix.Identity;
            pathEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0,
                device.Viewport.Width,
                device.Viewport.Height,
                0,
                0,
                1);

            foreach (EffectPass pass in pathEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    mesh.Vertices,
                    0,
                    mesh.Vertices.Length,
                    mesh.Indices,
                    0,
                    mesh.Indices.Length / 3);
            }
        }
        finally
        {
            BeginSpriteBatch(
                SpriteSortMode.Immediate,
                previousBlend,
                previousSampler,
                previousDepth,
                previousRasterizer);
        }
    }

    private void CreatePathResources(GraphicsDevice device)
    {
        pathEffect ??= new BasicEffect(device)
        {
            VertexColorEnabled = true,
            TextureEnabled = false,
            FogEnabled = false,
            LightingEnabled = false
        };
        pathRasterizerState ??= new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None,
            MultiSampleAntiAlias = true
        };
    }

    private void DrawText(DrawCommand command)
    {
        if (_textRasterizer is null || command.TextRun is null)
        {
            return;
        }

        XnaColor? solidTextColor = null;
        if (command.Brush is not null &&
            TryGetSolidColor(command.Brush.CreateDescriptor(), command.BrushOpacity, out XnaColor resolvedSolid))
        {
            solidTextColor = resolvedSolid;
        }

        DrawPoint pixelPhase = GetCanonicalPixelPhase(command.Position, coordinateScale);
        TextTextureKey key = TextTextureKey.From(command.TextRun, coordinateScale, pixelPhase);
        bool needsMask = command.Brush is not null && solidTextColor is null;

        if (!_textTextureCache.TryGetValue(key, out TextTexture cachedText))
        {
            textTextureCacheMisses++;
            RasterizedText[] layers = preparedTextRasterizations.Remove(key, out RasterizedText[]? prepared)
                ? prepared
                : _textRasterizer.RasterizeSubpixelAtPhase(
                    command.TextRun,
                    CernealaColor.White,
                    coordinateScale,
                    pixelPhase);
            try
            {
                cachedText = CreatePackedTextTexture(
                    layers,
                    needsMask ? CreateTexture(CreateGrayscaleMask(layers)) : null);
            }
            finally
            {
                ReturnRasterizedTextPixels(layers);
            }
            _textTextureCache.Add(key, cachedText);
            textTextureCacheEstimatedBytes += EstimateBytes(cachedText);
            MarkTextTextureUsed(key);
        }
        else if (preparedTextTextureKeys.Remove(key))
        {
            textTextureCacheMisses++;
            MarkTextTextureUsed(key);
        }
        else
        {
            textTextureCacheHits++;
            MarkTextTextureUsed(key);
        }

        if (needsMask && cachedText.MaskTexture is null)
        {
            RasterizedText[] layers = _textRasterizer.RasterizeSubpixelAtPhase(
                command.TextRun,
                CernealaColor.White,
                coordinateScale,
                pixelPhase);
            Texture2D mask;
            try
            {
                mask = CreateTexture(CreateGrayscaleMask(layers));
            }
            finally
            {
                ReturnRasterizedTextPixels(layers);
            }
            cachedText = cachedText with { MaskTexture = mask };
            _textTextureCache[key] = cachedText;
            textTextureCacheEstimatedBytes += EstimateBytes(mask);
        }

        Vector2 origin = MapTextTexturePosition(command.Position, cachedText.OriginOffset, coordinateScale);
        if (command.Brush is not null)
        {
            if (solidTextColor is not XnaColor solid)
            {
                Texture2D texture = GetOrCreateTextBrushTexture(key, cachedText, command.Brush, command.BrushOpacity);
                _spriteBatch.Draw(texture, origin, XnaColor.White);
                return;
            }

            DrawSolidText(cachedText, origin, solid);
            return;
        }

        DrawSolidText(cachedText, origin, ToColor(command.Color));
    }

    private void DrawTextLayout(DrawCommand command)
    {
        if (command.TextLayout is null)
        {
            return;
        }

        foreach (DrawTextLayoutLine line in command.TextLayout.Lines)
        {
            foreach (DrawTextLayoutRun run in line.Runs)
            {
                DrawText(DrawCommand.DrawText(
                    run.TextRun,
                    new DrawPoint(
                        command.Position.X + run.Position.X,
                        command.Position.Y + run.Position.Y),
                    run.Brush,
                    run.Opacity * command.BrushOpacity));
            }
        }
    }

    private void PrepareTextRasterizations(DrawCommandList commands)
    {
        long requestCollectionStarted = Stopwatch.GetTimestamp();
        preparedTextRasterizations.Clear();
        textRasterizationRequests.Clear();
        if (_textRasterizer is null)
        {
            lastTextRequestCollectionTime = Stopwatch.GetElapsedTime(requestCollectionStarted);
            return;
        }

        for (int index = 0; index < commands.Count; index++)
        {
            DrawCommand command = commands[index];
            if (command.Kind == DrawCommandKind.DrawText && command.TextRun is not null)
            {
                CollectRequest(
                    command.TextRun,
                    command.Position,
                    command.Brush,
                    command.BrushOpacity);
            }
            else if (command.Kind == DrawCommandKind.DrawTextLayout && command.TextLayout is not null)
            {
                foreach (DrawTextLayoutLine line in command.TextLayout.Lines)
                {
                    foreach (DrawTextLayoutRun run in line.Runs)
                    {
                        CollectRequest(
                            run.TextRun,
                            new DrawPoint(
                                command.Position.X + run.Position.X,
                                command.Position.Y + run.Position.Y),
                            run.Brush,
                            run.Opacity * command.BrushOpacity);
                    }
                }
            }
        }

        void CollectRequest(
            DrawTextRun textRun,
            DrawPoint position,
            IDrawBrush? brush,
            float opacity)
        {
            DrawPoint pixelPhase = GetCanonicalPixelPhase(position, coordinateScale);
            TextTextureKey key = TextTextureKey.From(textRun, coordinateScale, pixelPhase);
            if (!_textTextureCache.ContainsKey(key))
            {
                bool needsMask = brush is not null &&
                    !TryGetSolidColor(brush.CreateDescriptor(), opacity, out _);
                textRasterizationRequests.TryAdd(
                    key,
                    new TextRasterizationRequest(
                        textRun,
                        pixelPhase,
                        needsMask));
            }
        }

        lastTextRequestCollectionTime = Stopwatch.GetElapsedTime(requestCollectionStarted);
        lastTextRequestCount = textRasterizationRequests.Count;
        if (textRasterizationRequests.Count == 0)
        {
            return;
        }

        KeyValuePair<TextTextureKey, TextRasterizationRequest>[] work = textRasterizationRequests.ToArray();
        RasterizedText[][] results = new RasterizedText[work.Length][];
        long rasterizationStarted = Stopwatch.GetTimestamp();
        try
        {
            if (work.Length == 1)
            {
                TextRasterizationRequest request = work[0].Value;
                results[0] = _textRasterizer.RasterizeSubpixelAtPhase(
                    request.TextRun,
                    CernealaColor.White,
                    coordinateScale,
                    request.PixelPhase);
            }
            else
            {
                Parallel.For(
                    0,
                    work.Length,
                    index =>
                    {
                        TextRasterizationRequest request = work[index].Value;
                        results[index] = _textRasterizer.RasterizeSubpixelAtPhase(
                            request.TextRun,
                            CernealaColor.White,
                            coordinateScale,
                            request.PixelPhase);
                    });
            }
        }
        catch
        {
            ReturnRasterizedTextPixels(results);
            throw;
        }
        lastTextRasterizationTime = Stopwatch.GetElapsedTime(rasterizationStarted);
        for (int index = 0; index < results.Length; index++)
        {
            RasterizedText[] layers = results[index];
            if (layers.Length > 0)
            {
                long pixelCount = (long)layers[0].Width * layers[0].Height * layers.Length;
                lastRasterizedPixelCount += pixelCount;
            }
        }

        bool atlasCreated;
        long atlasUploadStarted = Stopwatch.GetTimestamp();
        try
        {
            atlasCreated = TryCreateTextTextureAtlas(work, results);
        }
        catch
        {
            ReturnRasterizedTextPixels(results);
            throw;
        }
        lastTextAtlasUploadTime = Stopwatch.GetElapsedTime(atlasUploadStarted);

        if (atlasCreated)
        {
            ReturnRasterizedTextPixels(results);
            return;
        }

        for (int index = 0; index < work.Length; index++)
        {
            preparedTextRasterizations.Add(work[index].Key, results[index]);
        }
    }

    private bool TryCreateTextTextureAtlas(
        KeyValuePair<TextTextureKey, TextRasterizationRequest>[] work,
        RasterizedText[][] results)
    {
        if (work.Length < 2)
        {
            return false;
        }

        if (!TryPackTextAtlas(results, out int atlasWidth, out int atlasHeight, out Rectangle[] placements))
        {
            return false;
        }

        int atlasPixelBytes = checked(atlasWidth * atlasHeight * 4);
        byte[] atlasPixels = ArrayPool<byte>.Shared.Rent(atlasPixelBytes);
        Texture2D atlas;
        try
        {
            atlasPixels.AsSpan(0, atlasPixelBytes).Clear();
            for (int index = 0; index < results.Length; index++)
            {
                RasterizedText[] layers = results[index];
                Rectangle placement = placements[index];
                int sourceRowBytes = layers[0].Width * 4;
                for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                {
                    ReadOnlySpan<byte> source = layers[layerIndex].PixelSpan;
                    int layerY = placement.Y + (layerIndex * layers[0].Height);
                    for (int row = 0; row < layers[0].Height; row++)
                    {
                        source.Slice(row * sourceRowBytes, sourceRowBytes).CopyTo(
                            atlasPixels.AsSpan(
                                ((((layerY + row) * atlasWidth) + placement.X) * 4),
                                sourceRowBytes));
                    }
                }
            }

            atlas = new Texture2D(_spriteBatch.GraphicsDevice, atlasWidth, atlasHeight);
            try
            {
                atlas.SetData(
                    level: 0,
                    rect: null,
                    data: atlasPixels,
                    startIndex: 0,
                    elementCount: atlasPixelBytes);
            }
            catch
            {
                atlas.Dispose();
                throw;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(atlasPixels);
        }

        sharedTextTextureReferenceCounts.Add(atlas, work.Length);
        textTextureCacheEstimatedBytes += EstimateBytes(atlas);

        for (int index = 0; index < work.Length; index++)
        {
            RasterizedText[] layers = results[index];
            RasterizedText first = layers[0];
            Rectangle placement = placements[index];
            Texture2D? mask = work[index].Value.NeedsMask
                ? CreateTexture(CreateGrayscaleMask(layers))
                : null;
            TextTexture text = new(
                atlas,
                atlas,
                atlas,
                mask,
                first.OriginOffset)
            {
                RedSource = new Rectangle(placement.X, placement.Y, first.Width, first.Height),
                GreenSource = new Rectangle(placement.X, placement.Y + first.Height, first.Width, first.Height),
                BlueSource = new Rectangle(placement.X, placement.Y + (first.Height * 2), first.Width, first.Height)
            };
            _textTextureCache.Add(work[index].Key, text);
            preparedTextTextureKeys.Add(work[index].Key);
            if (mask is not null)
            {
                textTextureCacheEstimatedBytes += EstimateBytes(mask);
            }
        }

        return true;
    }

    private static bool TryPackTextAtlas(
        RasterizedText[][] results,
        out int atlasWidth,
        out int atlasHeight,
        out Rectangle[] placements)
    {
        const int maximumSafeAtlasDimension = 4_096;
        long totalArea = 0;
        int widestBlock = 0;
        int[] order = new int[results.Length];
        placements = new Rectangle[results.Length];
        for (int index = 0; index < results.Length; index++)
        {
            RasterizedText first = results[index][0];
            int blockHeight = checked(first.Height * 3);
            widestBlock = Math.Max(widestBlock, first.Width);
            totalArea = checked(totalArea + ((long)first.Width * blockHeight));
            order[index] = index;
        }

        if (widestBlock > maximumSafeAtlasDimension)
        {
            atlasWidth = 0;
            atlasHeight = 0;
            return false;
        }

        Array.Sort(
            order,
            (left, right) =>
                (results[right][0].Height * 3).CompareTo(results[left][0].Height * 3));
        atlasWidth = Math.Max(
            widestBlock,
            Math.Min(maximumSafeAtlasDimension, (int)Math.Ceiling(Math.Sqrt(totalArea))));

        while (!TryPackTextAtlasAtWidth(
            results,
            order,
            atlasWidth,
            maximumSafeAtlasDimension,
            placements,
            out atlasHeight))
        {
            if (atlasWidth == maximumSafeAtlasDimension)
            {
                atlasHeight = 0;
                return false;
            }

            atlasWidth = Math.Min(maximumSafeAtlasDimension, checked(atlasWidth * 2));
        }

        return true;
    }

    private static bool TryPackTextAtlasAtWidth(
        RasterizedText[][] results,
        int[] order,
        int atlasWidth,
        int maximumAtlasHeight,
        Rectangle[] placements,
        out int atlasHeight)
    {
        int x = 0;
        int y = 0;
        int shelfHeight = 0;
        foreach (int index in order)
        {
            RasterizedText first = results[index][0];
            int blockHeight = first.Height * 3;
            if (x > 0 && x + first.Width > atlasWidth)
            {
                y += shelfHeight;
                x = 0;
                shelfHeight = 0;
            }

            if (y + blockHeight > maximumAtlasHeight)
            {
                atlasHeight = 0;
                return false;
            }

            placements[index] = new Rectangle(x, y, first.Width, blockHeight);
            x += first.Width;
            shelfHeight = Math.Max(shelfHeight, blockHeight);
        }

        atlasHeight = y + shelfHeight;
        return true;
    }

    private void DrawSolidText(TextTexture cachedText, Vector2 origin, XnaColor color)
    {
        color = Premultiply(color);
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        BlendState previousBlendState = graphicsDevice.BlendState;
        try
        {
            DrawTextLayer(cachedText.RedTexture, cachedText.RedSource, origin, color, redTextBlendState);
            DrawTextLayer(cachedText.GreenTexture, cachedText.GreenSource, origin, color, greenTextBlendState);
            DrawTextLayer(cachedText.BlueTexture, cachedText.BlueSource, origin, color, blueTextBlendState);
        }
        finally
        {
            graphicsDevice.BlendState = previousBlendState;
        }
    }

    private TextTexture CreatePackedTextTexture(RasterizedText[] layers, Texture2D? mask)
    {
        RasterizedText first = layers[0];
        int layerByteCount = first.PixelLength;
        byte[] packedPixels;
        if (ReferenceEquals(first.PixelBuffer, layers[1].PixelBuffer) &&
            ReferenceEquals(first.PixelBuffer, layers[2].PixelBuffer) &&
            first.PixelOffset == 0 &&
            layers[1].PixelOffset == layerByteCount &&
            layers[2].PixelOffset == layerByteCount * 2)
        {
            packedPixels = first.PixelBuffer;
        }
        else
        {
            packedPixels = GC.AllocateUninitializedArray<byte>(layerByteCount * 3);
            for (int index = 0; index < layers.Length; index++)
            {
                layers[index].PixelSpan.CopyTo(packedPixels.AsSpan(index * layerByteCount, layerByteCount));
            }
        }

        Texture2D packedTexture = new(
            _spriteBatch.GraphicsDevice,
            first.Width,
            first.Height * 3);
        packedTexture.SetData(
            level: 0,
            rect: null,
            data: packedPixels,
            startIndex: 0,
            elementCount: checked(layerByteCount * 3));
        return new TextTexture(
            packedTexture,
            packedTexture,
            packedTexture,
            mask,
            first.OriginOffset)
        {
            RedSource = new Rectangle(0, 0, first.Width, first.Height),
            GreenSource = new Rectangle(0, first.Height, first.Width, first.Height),
            BlueSource = new Rectangle(0, first.Height * 2, first.Width, first.Height)
        };
    }

    private Texture2D CreateTexture(RasterizedText text)
    {
        Texture2D texture = new(_spriteBatch.GraphicsDevice, text.Width, text.Height);
        texture.SetData(
            level: 0,
            rect: null,
            data: text.PixelBuffer,
            startIndex: text.PixelOffset,
            elementCount: text.PixelLength);
        return texture;
    }

    private static void ReturnRasterizedTextPixels(RasterizedText[] layers)
    {
        if (layers.Length > 0)
        {
            layers[0].ReturnPixelBuffer();
        }
    }

    private static void ReturnRasterizedTextPixels(RasterizedText[][] results)
    {
        foreach (RasterizedText[]? layers in results)
        {
            if (layers is not null)
            {
                ReturnRasterizedTextPixels(layers);
            }
        }
    }

    private void DrawTextLayer(
        Texture2D texture,
        Rectangle? source,
        Vector2 origin,
        XnaColor color,
        BlendState blendState)
    {
        _spriteBatch.GraphicsDevice.BlendState = blendState;
        _spriteBatch.Draw(texture, origin, source, color);
    }

    private Texture2D GetOrCreateTextBrushTexture(
        TextTextureKey textKey,
        TextTexture text,
        IDrawBrush brush,
        float commandOpacity)
    {
        TextBrushTextureKey key = new(textKey, brush, commandOpacity);
        if (textBrushTextureCache.TryGetValue(key, out Texture2D? cached))
        {
            return cached;
        }

        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        Rectangle previousScissor = device.ScissorRectangle;
        BlendState previousBlend = device.BlendState;
        SamplerState previousSampler = device.SamplerStates[0];
        DepthStencilState previousDepth = device.DepthStencilState;
        RasterizerState previousRasterizer = device.RasterizerState;
        MonoGameClipStack? previousClipStack = clipStack;
        Texture2D mask = text.MaskTexture ??
            throw new InvalidOperationException("Non-solid text brushes require a grayscale text mask.");
        RenderTarget2D target = new(device, mask.Width, mask.Height, false, SurfaceFormat.Color, DepthFormat.None);
        DrawRect localBounds = new(0, 0, target.Width / coordinateScale, target.Height / coordinateScale);

        EndSpriteBatch();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(XnaColor.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);
            clipStack = new MonoGameClipStack(device.ScissorRectangle);
            BeginSpriteBatch(SpriteSortMode.Immediate, BlendState.AlphaBlend, previousSampler, DepthStencilState.None, previousRasterizer);
            try
            {
                FillRectangle(localBounds, brush, commandOpacity);
            }
            finally
            {
                EndSpriteBatch();
            }

            BeginSpriteBatch(SpriteSortMode.Immediate, textMaskBlendState, SamplerState.PointClamp, DepthStencilState.None, previousRasterizer);
            try
            {
                _spriteBatch.Draw(mask, Vector2.Zero, XnaColor.White);
            }
            finally
            {
                EndSpriteBatch();
            }
        }
        catch
        {
            target.Dispose();
            throw;
        }
        finally
        {
            EndSpriteBatch();
            if (previousTargets.Length == 0)
            {
                device.SetRenderTarget(null);
            }
            else
            {
                device.SetRenderTargets(previousTargets);
            }

            device.ScissorRectangle = previousScissor;
            clipStack = previousClipStack;
            BeginSpriteBatch(SpriteSortMode.Immediate, previousBlend, previousSampler, previousDepth, previousRasterizer);
        }

        textBrushTextureCache.Add(key, target);
        textTextureCacheEstimatedBytes += EstimateBytes(target);
        return target;
    }

    private static RasterizedText CreateGrayscaleMask(IReadOnlyList<RasterizedText> layers)
    {
        RasterizedText first = layers[0];
        ReadOnlySpan<byte> red = layers[0].PixelSpan;
        ReadOnlySpan<byte> green = layers[1].PixelSpan;
        ReadOnlySpan<byte> blue = layers[2].PixelSpan;
        byte[] mask = new byte[red.Length];
        for (int index = 0; index < mask.Length; index += 4)
        {
            byte coverage = (byte)((red[index + 3] + green[index + 3] + blue[index + 3] + 1) / 3);
            mask[index] = coverage;
            mask[index + 1] = coverage;
            mask[index + 2] = coverage;
            mask[index + 3] = coverage;
        }

        return RasterizedText.FromOwnedPixels(
            first.Width,
            first.Height,
            mask,
            first.ShapeResult,
            first.OriginOffset);
    }

    private void PushTransformedClip(DrawRect rect)
    {
        NumericsMatrix3x2 transform = drawingTransforms[^1];
        if (MathF.Abs(transform.M12) <= 0.00001f &&
            MathF.Abs(transform.M21) <= 0.00001f)
        {
            geometricClipScopes.Push(false);
            PushClip(DrawCommandStateAnalyzer.TransformBounds(rect, transform));
            return;
        }

        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(rect.X, rect.Y))
            .LineTo(new DrawPoint(rect.Right, rect.Y))
            .LineTo(new DrawPoint(rect.Right, rect.Bottom))
            .LineTo(new DrawPoint(rect.X, rect.Bottom))
            .Close()
            .Build();
        PushGeometricClip(DrawCommand.PushClip(path));
    }

    private void PushGeometricClip(DrawCommand command)
    {
        geometricClipScopes.Push(true);
        BeginDrawingLayer(1, DrawBlendMode.Normal, isGeometricClip: true);
        DrawPathMask(command);
    }

    private void PopDrawingClip()
    {
        if (geometricClipScopes.Pop())
        {
            EndDrawingLayer();
        }
        else
        {
            PopClip();
        }
    }

    private void PushClip(DrawRect rect)
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        MonoGameClipStack stack = clipStack ??= new MonoGameClipStack(new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height));

        stack.Push(Mapper.MapRectangle(rect));
        graphicsDevice.ScissorRectangle = stack.CurrentClip;
    }

    private void PopClip()
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        MonoGameClipStack stack = clipStack ??= new MonoGameClipStack(new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height));
        graphicsDevice.ScissorRectangle = stack.Pop();
    }

    private void BeginDrawingLayer(
        float opacity,
        DrawBlendMode blendMode,
        bool isGeometricClip)
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        EndSpriteBatch();
        Viewport hostViewport = graphicsDevice.Viewport;
        Rectangle hostScissor = graphicsDevice.ScissorRectangle;
        RenderTargetBinding[] hostTargets = graphicsDevice.GetRenderTargets();
        RenderTarget2D target = AcquireDrawingLayerTarget(
            graphicsDevice,
            hostViewport.Width,
            hostViewport.Height);
        drawingLayers.Add(new DrawingLayerScope(
            target,
            hostTargets,
            hostViewport,
            hostScissor,
            opacity,
            blendMode,
            isGeometricClip));
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Viewport = new Viewport(
            0,
            0,
            target.Width,
            target.Height);
        graphicsDevice.ScissorRectangle = new Rectangle(
            0,
            0,
            target.Width,
            target.Height);
        graphicsDevice.Clear(
            ClearOptions.Target | ClearOptions.Stencil,
            XnaColor.Transparent,
            depth: 1,
            stencil: 0);
        BeginUiSpriteBatch();
    }

    private void EndDrawingLayer()
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        EndSpriteBatch();
        DrawingLayerScope scope = drawingLayers[^1];
        drawingLayers.RemoveAt(drawingLayers.Count - 1);
        RestoreDrawingLayerHost(graphicsDevice, scope);

        BeginSpriteBatch(
            SpriteSortMode.Immediate,
            ResolveBlendState(scope.BlendMode),
            SamplerState.LinearClamp,
            CurrentDepthStencilState,
            scissorRasterizerState!,
            transformMatrix: Matrix.Identity);
        _spriteBatch.Draw(
            scope.Target,
            new Rectangle(0, 0, scope.Target.Width, scope.Target.Height),
            OpacityTint(scope.Opacity));
        EndSpriteBatch();
        ReturnDrawingLayerTarget(scope.Target);
        BeginUiSpriteBatch();
    }

    private static void RestoreDrawingLayerHost(
        GraphicsDevice graphicsDevice,
        DrawingLayerScope scope)
    {
        if (scope.HostTargets.Length == 0)
        {
            graphicsDevice.SetRenderTarget(null);
        }
        else
        {
            graphicsDevice.SetRenderTargets(scope.HostTargets);
        }
        graphicsDevice.Viewport = scope.HostViewport;
        graphicsDevice.ScissorRectangle = scope.HostScissor;
    }

    private RenderTarget2D AcquireDrawingLayerTarget(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        while (drawingLayerPool.TryPop(out RenderTarget2D? target))
        {
            if (!target.IsDisposed &&
                target.Width == width &&
                target.Height == height &&
                ReferenceEquals(target.GraphicsDevice, graphicsDevice))
            {
                return target;
            }
            target.Dispose();
        }

        return new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            mipMap: false,
            preferredFormat: SurfaceFormat.Color,
            preferredDepthFormat: DepthFormat.Depth24Stencil8,
            preferredMultiSampleCount: 0,
            usage: RenderTargetUsage.PreserveContents);
    }

    private void ReturnDrawingLayerTarget(RenderTarget2D target)
    {
        const int MaximumPooledTargets = 8;
        if (drawingLayerPool.Count < MaximumPooledTargets && !target.IsDisposed)
        {
            drawingLayerPool.Push(target);
        }
        else
        {
            target.Dispose();
        }
    }

    private void ResetDrawingState()
    {
        drawingTransforms.Clear();
        drawingTransforms.Add(NumericsMatrix3x2.Identity);
        drawingBlends.Clear();
        drawingBlends.Add(DrawBlendMode.Normal);
        geometricClipScopes.Clear();
        drawingLayers.Clear();
    }

    private void AbortDrawingLayers(GraphicsDevice graphicsDevice)
    {
        while (drawingLayers.Count > 0)
        {
            DrawingLayerScope scope = drawingLayers[^1];
            drawingLayers.RemoveAt(drawingLayers.Count - 1);
            RestoreDrawingLayerHost(graphicsDevice, scope);
            ReturnDrawingLayerTarget(scope.Target);
        }
        drawingTransforms.Clear();
        drawingTransforms.Add(NumericsMatrix3x2.Identity);
        drawingBlends.Clear();
        drawingBlends.Add(DrawBlendMode.Normal);
        geometricClipScopes.Clear();
    }

    private BlendState ResolveBlendState(DrawBlendMode mode) =>
        mode switch
        {
            DrawBlendMode.Normal => BlendState.AlphaBlend,
            DrawBlendMode.Opaque => BlendState.Opaque,
            DrawBlendMode.Additive => BlendState.Additive,
            DrawBlendMode.Multiply => multiplyBlendState,
            DrawBlendMode.Screen => screenBlendState,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

    private DepthStencilState CurrentDepthStencilState =>
        drawingLayers.Count > 0 && drawingLayers[^1].IsGeometricClip
            ? stencilTestState
            : DepthStencilState.None;

    private Matrix CurrentSpriteTransform
    {
        get
        {
            NumericsMatrix3x2 transform = drawingTransforms[^1];
            Matrix logical = new(
                transform.M11, transform.M12, 0, 0,
                transform.M21, transform.M22, 0, 0,
                0, 0, 1, 0,
                transform.M31, transform.M32, 0, 1);
            Matrix toPhysical = Matrix.CreateScale(coordinateScale, coordinateScale, 1);
            Matrix toLogical = Matrix.CreateScale(1 / coordinateScale, 1 / coordinateScale, 1);
            return toLogical * logical * toPhysical;
        }
    }

    private Texture2D GetOrCreateBrushTexture(IDrawBrush brush, DrawBrushDescriptor descriptor, DrawRect rect)
    {
        Rectangle pixelBounds = Mapper.MapRectangle(rect);
        (int width, int height) = BrushTextureSize(descriptor, pixelBounds);
        BrushTextureKey key = new(brush, rect, width, height, coordinateScale, 0);
        if (brushTextureCache.TryGetValue(key, out Texture2D? cached))
        {
            activeBrushTextureKeys.Add(key);
            return cached;
        }

        CernealaColor[] pixels = new CernealaColor[width * height];
        float logicalWidth = rect.Width <= 0 ? 1 : rect.Width;
        float logicalHeight = rect.Height <= 0 ? 1 : rect.Height;
        for (int y = 0; y < height; y++)
        {
            float logicalY = rect.Y + (((y + 0.5f) / height) * logicalHeight);
            for (int x = 0; x < width; x++)
            {
                float logicalX = rect.X + (((x + 0.5f) / width) * logicalWidth);
                pixels[(y * width) + x] = SampleInBounds(descriptor, rect, new DrawPoint(logicalX, logicalY));
            }
        }

        Texture2D texture = new(_spriteBatch.GraphicsDevice, width, height);
        texture.SetData(pixels.Select(color => Premultiply(ToColor(color))).ToArray());
        brushTextureCache.Add(key, texture);
        activeBrushTextureKeys.Add(key);
        return texture;
    }

    private static (int Width, int Height) BrushTextureSize(
        DrawBrushDescriptor descriptor,
        Rectangle pixelBounds)
    {
        int width = Math.Max(1, pixelBounds.Width);
        int height = Math.Max(1, pixelBounds.Height);
        if (descriptor is not LinearGradientDrawBrushDescriptor linear)
        {
            return (width, height);
        }

        if (linear.StartPoint.X == linear.EndPoint.X)
        {
            width = 1;
        }
        if (linear.StartPoint.Y == linear.EndPoint.Y)
        {
            height = 1;
        }

        return (width, height);
    }

    private void DrawImageBrush(DrawRect destination, ImageDrawBrushDescriptor descriptor, float commandOpacity)
    {
        if (descriptor.Image is null)
        {
            if (!string.IsNullOrWhiteSpace(descriptor.SourceIdentity))
            {
                throw new InvalidOperationException(
                    $"ImageBrush source '{descriptor.SourceIdentity}' was not resolved to a device-local image.");
            }

            return;
        }

        if (descriptor.Image is not MonoGameImage image)
        {
            throw new InvalidOperationException("ImageBrush requires a MonoGameImage when using MonoGameDrawingBackend.");
        }

        ObjectDisposedException.ThrowIf(image.Texture.IsDisposed, image);
        if (!ReferenceEquals(image.Texture.GraphicsDevice, _spriteBatch.GraphicsDevice))
        {
            throw new InvalidOperationException("An ImageBrush image can only be used by the GraphicsDevice that created it.");
        }

        Rectangle source = descriptor.Viewbox is DrawRect viewbox
            ? ClampSourceRectangle(ToSourceRectangle(viewbox), image.Texture.Width, image.Texture.Height)
            : new Rectangle(0, 0, image.Texture.Width, image.Texture.Height);
        DrawRect tile = descriptor.Viewport ?? destination;
        if (descriptor.TileMode == DrawTileMode.None)
        {
            DrawImageTile(image.Texture, source, destination, tile, descriptor, SpriteEffects.None, commandOpacity);
            return;
        }

        if (tile.Width <= 0 || tile.Height <= 0)
        {
            return;
        }

        int column = 0;
        for (float x = destination.X; x < destination.Right; x += tile.Width, column++)
        {
            int row = 0;
            for (float y = destination.Y; y < destination.Bottom; y += tile.Height, row++)
            {
                DrawRect clippedTile = new(x, y, MathF.Min(tile.Width, destination.Right - x), MathF.Min(tile.Height, destination.Bottom - y));
                SpriteEffects effects = GetTileEffects(descriptor.TileMode, column, row);
                DrawImageTile(image.Texture, source, destination, clippedTile, descriptor, effects, commandOpacity);
            }
        }
    }

    private void DrawImageTile(
        Texture2D texture,
        Rectangle source,
        DrawRect clipBounds,
        DrawRect tile,
        ImageDrawBrushDescriptor descriptor,
        SpriteEffects effects,
        float commandOpacity)
    {
        DrawRect fitted = FitTile(tile, source.Width, source.Height, descriptor.Stretch, descriptor.AlignmentX, descriptor.AlignmentY);
        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        Rectangle previous = device.ScissorRectangle;
        Rectangle clip = MonoGameClipStack.Intersect(previous, Mapper.MapRectangle(clipBounds));
        device.ScissorRectangle = clip;
        try
        {
            _spriteBatch.Draw(texture, Mapper.MapRectangle(fitted), source, OpacityTint(descriptor.Opacity * commandOpacity), 0, Vector2.Zero, effects, 0);
        }
        finally
        {
            device.ScissorRectangle = previous;
        }
    }

    private void DrawCommandBrush(
        DrawRect destination,
        IDrawBrush brush,
        IReadOnlyList<DrawCommand> commands,
        DrawRect contentBounds,
        TileDrawBrushDescriptor descriptor,
        float commandOpacity)
    {
        if (!activeBrushes.Add(brush))
        {
            throw new InvalidOperationException("Brush rendering cycle detected.");
        }

        try
        {
            DrawRect tile = descriptor.Viewport ?? destination;
            Texture2D texture = GetOrCreateCommandBrushTexture(brush, commands, contentBounds, tile);
            int column = 0;
            for (float x = destination.X; x < destination.Right; x += tile.Width, column++)
            {
                int row = 0;
                for (float y = destination.Y; y < destination.Bottom; y += tile.Height, row++)
                {
                    DrawRect current = descriptor.TileMode == DrawTileMode.None
                        ? destination
                        : new DrawRect(x, y, MathF.Min(tile.Width, destination.Right - x), MathF.Min(tile.Height, destination.Bottom - y));
                    SpriteEffects effects = GetTileEffects(descriptor.TileMode, column, row);
                    PushClip(current);
                    try
                    {
                        _spriteBatch.Draw(
                            texture,
                            Mapper.MapRectangle(current),
                            null,
                            OpacityTint(descriptor.Opacity * commandOpacity),
                            0,
                            Vector2.Zero,
                            effects,
                            0);
                    }
                    finally
                    {
                        PopClip();
                    }

                    if (descriptor.TileMode == DrawTileMode.None)
                    {
                        break;
                    }
                }

                if (descriptor.TileMode == DrawTileMode.None)
                {
                    break;
                }
            }
        }
        finally
        {
            activeBrushes.Remove(brush);
        }
    }

    private Texture2D GetOrCreateCommandBrushTexture(
        IDrawBrush brush,
        IReadOnlyList<DrawCommand> commands,
        DrawRect contentBounds,
        DrawRect tile)
    {
        DrawRect localTile = new(0, 0, MathF.Max(1 / coordinateScale, tile.Width), MathF.Max(1 / coordinateScale, tile.Height));
        Rectangle pixels = Mapper.MapRectangle(localTile);
        int contentVersion = GetCommandContentHash(commands, contentBounds);
        BrushTextureKey key = new(brush, localTile, Math.Max(1, pixels.Width), Math.Max(1, pixels.Height), coordinateScale, contentVersion);
        if (brushTextureCache.TryGetValue(key, out Texture2D? cached))
        {
            activeBrushTextureKeys.Add(key);
            return cached;
        }

        foreach (BrushTextureKey staleKey in brushTextureCache.Keys
            .Where(candidate => Equals(candidate.Brush, brush) && candidate.ContentVersion != contentVersion)
            .ToArray())
        {
            brushTextureCache[staleKey].Dispose();
            brushTextureCache.Remove(staleKey);
        }

        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        Rectangle previousScissor = device.ScissorRectangle;
        BlendState previousBlend = device.BlendState;
        SamplerState previousSampler = device.SamplerStates[0];
        DepthStencilState previousDepth = device.DepthStencilState;
        RasterizerState previousRasterizer = device.RasterizerState;
        MonoGameClipStack? previousClipStack = clipStack;
        RenderTarget2D target = new(device, key.Width, key.Height, false, SurfaceFormat.Color, DepthFormat.None);

        EndSpriteBatch();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(XnaColor.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);
            clipStack = new MonoGameClipStack(device.ScissorRectangle);
            BeginSpriteBatch(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                previousRasterizer,
                transformMatrix: Matrix.Identity);
            try
            {
                foreach (DrawCommand command in commands)
                {
                    RenderCommand(MapBrushCommand(command, contentBounds, localTile, 1, flipX: false, flipY: false));
                }
            }
            finally
            {
                EndSpriteBatch();
            }
        }
        catch
        {
            target.Dispose();
            throw;
        }
        finally
        {
            EndSpriteBatch();
            if (previousTargets.Length == 0)
            {
                device.SetRenderTarget(null);
            }
            else
            {
                device.SetRenderTargets(previousTargets);
            }

            device.ScissorRectangle = previousScissor;
            clipStack = previousClipStack;
            BeginSpriteBatch(
                SpriteSortMode.Immediate,
                previousBlend,
                previousSampler,
                previousDepth,
                previousRasterizer);
        }

        brushTextureCache.Add(key, target);
        activeBrushTextureKeys.Add(key);
        return target;
    }

    private static int GetCommandContentHash(IReadOnlyList<DrawCommand> commands, DrawRect bounds)
    {
        HashCode hash = new();
        hash.Add(bounds);
        foreach (DrawCommand command in commands)
        {
            hash.Add(command);
        }

        return hash.ToHashCode();
    }

    private static DrawCommand MapBrushCommand(
        DrawCommand command,
        DrawRect source,
        DrawRect destination,
        float opacity,
        bool flipX,
        bool flipY)
    {
        DrawPoint MapPoint(DrawPoint point)
        {
            float normalizedX = (point.X - source.X) / source.Width;
            float normalizedY = (point.Y - source.Y) / source.Height;
            if (flipX) normalizedX = 1 - normalizedX;
            if (flipY) normalizedY = 1 - normalizedY;
            return new DrawPoint(destination.X + (normalizedX * destination.Width), destination.Y + (normalizedY * destination.Height));
        }

        DrawRect MapRect(DrawRect rect)
        {
            DrawPoint first = MapPoint(new DrawPoint(rect.X, rect.Y));
            DrawPoint second = MapPoint(new DrawPoint(rect.Right, rect.Bottom));
            return new DrawRect(MathF.Min(first.X, second.X), MathF.Min(first.Y, second.Y), MathF.Abs(second.X - first.X), MathF.Abs(second.Y - first.Y));
        }

        float thicknessScale = MathF.Min(destination.Width / source.Width, destination.Height / source.Height);
        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(MapRect(command.Rect), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(MapRect(command.Rect), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(MapRect(command.Rect), command.Brush, command.Thickness * thicknessScale, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(MapRect(command.Rect), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.FillRoundedRectangle when command.Brush is not null => DrawCommand.FillRoundedRectangle(MapRect(command.Rect), Scale(command.CornerRadius, thicknessScale), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRoundedRectangle => DrawCommand.FillRoundedRectangle(MapRect(command.Rect), Scale(command.CornerRadius, thicknessScale), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRoundedRectangle when command.Pen is not null => DrawCommand.DrawRoundedRectangle(MapRect(command.Rect), Scale(command.CornerRadius, thicknessScale), new DrawPen(command.Pen.Brush, command.Pen.Thickness * thicknessScale, command.Pen.Style), command.BrushOpacity * opacity),
            DrawCommandKind.DrawRoundedRectangle => DrawCommand.DrawRoundedRectangle(MapRect(command.Rect), Scale(command.CornerRadius, thicknessScale), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(MapRect(command.Rect), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(MapRect(command.Rect), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(MapRect(command.Rect), command.Brush, command.Thickness * thicknessScale, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(MapRect(command.Rect), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(MapPoint(command.Position), MapPoint(command.EndPoint), command.Brush, command.Thickness * thicknessScale, command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(MapPoint(command.Position), MapPoint(command.EndPoint), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.FillPath when command.Brush is not null => DrawCommand.FillPath(command.Path!, command.SourceRect, MapRect(command.Rect), command.Brush, command.FillRule, command.BrushOpacity * opacity),
            DrawCommandKind.FillPath => DrawCommand.FillPath(command.Path!, command.SourceRect, MapRect(command.Rect), ApplyOpacity(command.Color, opacity), command.FillRule),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(
                command.Image!,
                MapRect(command.Rect),
                command.ImageSource,
                ApplyOpacity(command.Color, opacity),
                command.ImageRotation,
                command.ImageOrigin,
                CombineFlip(command.ImageFlip, flipX, flipY),
                command.LayerDepth),
            DrawCommandKind.RenderSurface2D => DrawCommand.RenderSurface2D(command.RenderSurface!, MapRect(command.Rect), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(command.TextRun!, MapPoint(command.Position), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, MapPoint(command.Position), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawTextLayout => DrawCommand.DrawTextLayout(command.TextLayout!, MapPoint(command.Position), command.BrushOpacity * opacity),
            DrawCommandKind.PushClip => DrawCommand.PushClip(MapRect(command.Rect)),
            DrawCommandKind.PopClip => command,
            DrawCommandKind.BeginPrism => command,
            DrawCommandKind.EndPrism => command,
            _ => command
        };

        static DrawCornerRadius Scale(DrawCornerRadius radius, float scale) =>
            new(
                radius.TopLeft * scale,
                radius.TopRight * scale,
                radius.BottomRight * scale,
                radius.BottomLeft * scale);
    }

    private static DrawRect FitTile(
        DrawRect tile,
        float sourceWidth,
        float sourceHeight,
        DrawBrushStretch stretch,
        DrawBrushAlignmentX alignmentX,
        DrawBrushAlignmentY alignmentY)
    {
        if (stretch == DrawBrushStretch.Fill)
        {
            return tile;
        }

        float scale = stretch switch
        {
            DrawBrushStretch.None => 1,
            DrawBrushStretch.Uniform => MathF.Min(tile.Width / sourceWidth, tile.Height / sourceHeight),
            DrawBrushStretch.UniformToFill => MathF.Max(tile.Width / sourceWidth, tile.Height / sourceHeight),
            _ => 1
        };
        float width = sourceWidth * scale;
        float height = sourceHeight * scale;
        float x = alignmentX switch
        {
            DrawBrushAlignmentX.Left => tile.X,
            DrawBrushAlignmentX.Right => tile.Right - width,
            _ => tile.X + ((tile.Width - width) / 2)
        };
        float y = alignmentY switch
        {
            DrawBrushAlignmentY.Top => tile.Y,
            DrawBrushAlignmentY.Bottom => tile.Bottom - height,
            _ => tile.Y + ((tile.Height - height) / 2)
        };
        return new DrawRect(x, y, width, height);
    }

    internal static DrawRect FitTileForDiagnostics(
        DrawRect tile,
        float sourceWidth,
        float sourceHeight,
        DrawBrushStretch stretch,
        DrawBrushAlignmentX alignmentX,
        DrawBrushAlignmentY alignmentY)
    {
        return FitTile(tile, sourceWidth, sourceHeight, stretch, alignmentX, alignmentY);
    }

    internal static void ValidateBrushGraphForDiagnostics(IDrawBrush brush)
    {
        ArgumentNullException.ThrowIfNull(brush);
        ValidateBrushGraph(brush, new HashSet<IDrawBrush>(ReferenceEqualityComparer.Instance));
    }

    private static void ValidateBrushGraph(IDrawBrush brush, HashSet<IDrawBrush> active)
    {
        if (!active.Add(brush))
        {
            throw new InvalidOperationException("Brush rendering cycle detected.");
        }

        try
        {
            IReadOnlyList<DrawCommand>? commands = brush.CreateDescriptor() switch
            {
                DrawingDrawBrushDescriptor drawing => drawing.Commands,
                VisualDrawBrushDescriptor visual => visual.Commands,
                _ => null
            };
            if (commands is null)
            {
                return;
            }

            foreach (IDrawBrush nested in commands.Where(command => command.Brush is not null).Select(command => command.Brush!))
            {
                ValidateBrushGraph(nested, active);
            }
        }
        finally
        {
            active.Remove(brush);
        }
    }

    private static SpriteEffects GetTileEffects(DrawTileMode mode, int column, int row)
    {
        SpriteEffects effects = SpriteEffects.None;
        if (column % 2 != 0 && mode is DrawTileMode.FlipX or DrawTileMode.FlipXY)
        {
            effects |= SpriteEffects.FlipHorizontally;
        }

        if (row % 2 != 0 && mode is DrawTileMode.FlipY or DrawTileMode.FlipXY)
        {
            effects |= SpriteEffects.FlipVertically;
        }

        return effects;
    }

    private static SpriteEffects ToSpriteEffects(DrawImageFlip flip)
    {
        SpriteEffects effects = SpriteEffects.None;
        if ((flip & DrawImageFlip.Horizontal) != 0)
        {
            effects |= SpriteEffects.FlipHorizontally;
        }
        if ((flip & DrawImageFlip.Vertical) != 0)
        {
            effects |= SpriteEffects.FlipVertically;
        }

        return effects;
    }

    private static DrawImageFlip CombineFlip(
        DrawImageFlip flip,
        bool flipX,
        bool flipY)
    {
        if (flipX)
        {
            flip ^= DrawImageFlip.Horizontal;
        }
        if (flipY)
        {
            flip ^= DrawImageFlip.Vertical;
        }

        return flip;
    }

    private static Rectangle ClampSourceRectangle(Rectangle source, int width, int height)
    {
        int left = Math.Clamp(source.Left, 0, width);
        int top = Math.Clamp(source.Top, 0, height);
        int right = Math.Clamp(source.Right, left, width);
        int bottom = Math.Clamp(source.Bottom, top, height);
        return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private static Rectangle ToSourceRectangle(DrawRect rect)
    {
        int left = (int)MathF.Round(rect.X);
        int top = (int)MathF.Round(rect.Y);
        int right = (int)MathF.Round(rect.Right);
        int bottom = (int)MathF.Round(rect.Bottom);
        return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static bool TryGetSolidColor(DrawBrushDescriptor descriptor, float commandOpacity, out XnaColor color)
    {
        if (descriptor is SolidDrawBrushDescriptor solid)
        {
            color = ToColor(ApplyOpacity(solid.Color, solid.Opacity * commandOpacity));
            return true;
        }

        color = default;
        return false;
    }

    private static CernealaColor Sample(DrawBrushDescriptor descriptor, DrawPoint point)
    {
        return descriptor switch
        {
            SolidDrawBrushDescriptor solid => ApplyOpacity(solid.Color, solid.Opacity),
            LinearGradientDrawBrushDescriptor linear => ApplyOpacity(SampleLinear(linear, point), linear.Opacity),
            RadialGradientDrawBrushDescriptor radial => ApplyOpacity(SampleRadial(radial, point), radial.Opacity),
            _ => CernealaColor.Transparent
        };
    }

    internal static CernealaColor SampleBrushForDiagnostics(IDrawBrush brush, DrawPoint point, float commandOpacity = 1)
    {
        ArgumentNullException.ThrowIfNull(brush);
        return ApplyOpacity(Sample(brush.CreateDescriptor(), point), commandOpacity);
    }

    internal static CernealaColor SampleBrushInBoundsForDiagnostics(
        IDrawBrush brush,
        DrawRect bounds,
        DrawPoint point,
        float commandOpacity = 1)
    {
        ArgumentNullException.ThrowIfNull(brush);
        return ApplyOpacity(SampleInBounds(brush.CreateDescriptor(), bounds, point), commandOpacity);
    }

    private static CernealaColor SampleInBounds(DrawBrushDescriptor descriptor, DrawRect bounds, DrawPoint point)
    {
        return Sample(descriptor, new DrawPoint(point.X - bounds.X, point.Y - bounds.Y));
    }

    private static CernealaColor SampleLinear(LinearGradientDrawBrushDescriptor gradient, DrawPoint point)
    {
        float dx = gradient.EndPoint.X - gradient.StartPoint.X;
        float dy = gradient.EndPoint.Y - gradient.StartPoint.Y;
        float lengthSquared = (dx * dx) + (dy * dy);
        float offset = lengthSquared <= float.Epsilon
            ? 1
            : (((point.X - gradient.StartPoint.X) * dx) + ((point.Y - gradient.StartPoint.Y) * dy)) / lengthSquared;
        return InterpolateStops(gradient.Stops, offset);
    }

    private static CernealaColor SampleRadial(RadialGradientDrawBrushDescriptor gradient, DrawPoint point)
    {
        float dx = (point.X - gradient.Center.X) / gradient.RadiusX;
        float dy = (point.Y - gradient.Center.Y) / gradient.RadiusY;
        return InterpolateStops(gradient.Stops, MathF.Sqrt((dx * dx) + (dy * dy)));
    }

    private static CernealaColor InterpolateStops(IReadOnlyList<DrawGradientStop> stops, float offset)
    {
        if (stops.Count == 1 || offset <= stops[0].Offset)
        {
            return stops[0].Color;
        }

        for (int i = 1; i < stops.Count; i++)
        {
            DrawGradientStop next = stops[i];
            if (offset > next.Offset)
            {
                continue;
            }

            DrawGradientStop previous = stops[i - 1];
            float range = next.Offset - previous.Offset;
            float amount = range <= float.Epsilon ? 1 : Math.Clamp((offset - previous.Offset) / range, 0, 1);
            return new CernealaColor(
                Lerp(previous.Color.R, next.Color.R, amount),
                Lerp(previous.Color.G, next.Color.G, amount),
                Lerp(previous.Color.B, next.Color.B, amount),
                Lerp(previous.Color.A, next.Color.A, amount));
        }

        return stops[^1].Color;
    }

    private static byte Lerp(byte first, byte second, float amount)
    {
        return (byte)Math.Clamp((int)MathF.Round(first + ((second - first) * amount)), 0, 255);
    }

    private static CernealaColor ApplyOpacity(CernealaColor color, float opacity)
    {
        return new CernealaColor(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * Math.Clamp(opacity, 0, 1)), 0, 255));
    }

    private static XnaColor OpacityTint(float opacity)
    {
        byte alpha = (byte)Math.Clamp((int)MathF.Round(255 * Math.Clamp(opacity, 0, 1)), 0, 255);
        return new XnaColor(alpha, alpha, alpha, alpha);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ClearTextTextureCaches();
        ClearBrushTextureCache();
        ClearPathMeshCache();
        prismExecutor?.Dispose();
        if (_spriteBatch?.GraphicsDevice is GraphicsDevice graphicsDevice)
        {
            graphicsDevice.DeviceReset -= OnDeviceReset;
        }
        redTextBlendState?.Dispose();
        greenTextBlendState?.Dispose();
        blueTextBlendState?.Dispose();
        textMaskBlendState?.Dispose();
        multiplyBlendState?.Dispose();
        screenBlendState?.Dispose();
        stencilWriteBlendState?.Dispose();
        stencilWriteState?.Dispose();
        stencilTestState?.Dispose();
        DisposeDrawingLayerPool();
        pathEffect?.Dispose();
        pathRasterizerState?.Dispose();
        scissorRasterizerState?.Dispose();
        disposed = true;
    }

    internal static void ValidateGraphicsResources(
        SpriteBatch spriteBatch,
        Texture2D whitePixel,
        string whitePixelParameterName)
    {
        ObjectDisposedException.ThrowIf(spriteBatch.IsDisposed, spriteBatch);
        ObjectDisposedException.ThrowIf(whitePixel.IsDisposed, whitePixel);

        GraphicsDevice? spriteBatchDevice = spriteBatch.GraphicsDevice;
        GraphicsDevice? whitePixelDevice = whitePixel.GraphicsDevice;
        if (spriteBatchDevice is not null)
        {
            ObjectDisposedException.ThrowIf(spriteBatchDevice.IsDisposed, spriteBatchDevice);
        }

        if (spriteBatchDevice is not null &&
            whitePixelDevice is not null &&
            !ReferenceEquals(spriteBatchDevice, whitePixelDevice))
        {
            throw new ArgumentException(
                "WhitePixel must belong to the same GraphicsDevice as SpriteBatch.",
                whitePixelParameterName);
        }
    }

    internal int ClipStackDepth => clipStack?.Depth ?? 0;

    internal int DrawingLayerPoolCount => drawingLayerPool?.Count ?? 0;

    internal int ActiveDrawingLayerCount => drawingLayers?.Count ?? 0;

    internal int TextTextureCacheCount => _textTextureCache.Count;

    internal TextTextureCacheDiagnosticSnapshot TextTextureCacheDiagnostics =>
        new(
            textTextureCacheHits,
            textTextureCacheMisses,
            textTextureCacheEvictions,
            textTextureCacheEstimatedBytes);

    DrawingBackendFrameTiming IDrawingBackendFrameTimingSource.LastFrameTiming => LastFrameTiming;

    internal DrawingBackendFrameTiming LastFrameTiming { get; private set; }

    internal int BrushTextureCacheCount => brushTextureCache?.Count ?? 0;

    internal int StrokeMeshCacheCount => strokeMeshCache.Count;

    internal static object CreateStrokeMeshKeyForDiagnostics(
        DrawCommand command,
        float coordinateScale) =>
        StrokeMeshKey.From(command, coordinateScale);

    internal bool UsesGraphicsDevice(
        GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return !disposed &&
            !_spriteBatch.IsDisposed &&
            ReferenceEquals(
                _spriteBatch.GraphicsDevice,
                graphicsDevice);
    }

    public PrismRendererDiagnostics RendererDiagnostics =>
        prismExecutor?.RendererDiagnostics ??
        PrismRendererDiagnostics.Empty(
            prismRetainedCacheEnabled);

    internal PrismExecutionDiagnostics PrismDiagnostics =>
        prismDiagnostics;

    internal PrismRetainedSurfaceCache? PrismRetainedCacheForDiagnostics =>
        prismExecutor?.RetainedSurfaceCache;

    internal void EnablePrism()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        prismEnabled = true;
    }

    private void ConsumePrismCacheInvalidations(
        PrismCacheInvalidationQueue? invalidations)
    {
        if (invalidations is null)
        {
            return;
        }

        while (invalidations.TryDequeue(
            out PrismCacheInvalidation invalidation))
        {
            prismExecutor?.Invalidate(invalidation);
        }
    }

    private void OnDeviceReset(object? sender, EventArgs args)
    {
        ClearTextTextureCaches();
        ClearBrushTextureCache();
        ClearPathMeshCache();
        prismExecutorUnavailable = false;
        prismExecutor?.Reset();
        DisposeDrawingLayerPool();
    }

    private void DisposeDrawingLayerPool()
    {
        if (drawingLayerPool is null)
        {
            return;
        }

        while (drawingLayerPool.TryPop(out RenderTarget2D? target))
        {
            target.Dispose();
        }
    }

    private bool TryEnsurePrismExecutor(GraphicsDevice graphicsDevice)
    {
        if (!prismEnabled)
        {
            return false;
        }
        if (prismExecutor is not null)
        {
            return true;
        }
        if (prismExecutorUnavailable)
        {
            return false;
        }

        try
        {
            prismExecutor = new PrismGraphExecutor(
                graphicsDevice,
                prismDiagnostics,
                prismRendererOptions,
                prismRetainedCacheEnabled);
            return true;
        }
        catch (PrismShaderUnavailableException exception)
        {
            prismExecutorUnavailable = true;
            prismDiagnostics.Record(
                null,
                -1,
                PrismFallbackReason.ShaderUnavailable,
                exception.Message);
            return false;
        }
    }

    GraphicsDevice IPrismCommandRenderer.GraphicsDevice =>
        _spriteBatch.GraphicsDevice;

    void IPrismCommandRenderer.BeginCommandBatch()
    {
        BeginSpriteBatch(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            scissorRasterizerState!);
    }

    void IPrismCommandRenderer.BeginKernelBatch(
        Effect effect,
        BlendState blendState,
        SamplerState samplerState,
        Rectangle? scissorRectangle)
    {
        if (scissorRectangle is Rectangle scissor)
        {
            _spriteBatch.GraphicsDevice.ScissorRectangle = scissor;
        }
        BeginSpriteBatch(
            SpriteSortMode.Immediate,
            blendState,
            samplerState,
            DepthStencilState.None,
            scissorRectangle.HasValue
                ? scissorRasterizerState!
                : RasterizerState.CullNone,
            effect,
            Matrix.Identity);
    }

    void IPrismCommandRenderer.EndBatch()
    {
        EndSpriteBatch();
    }

    void IPrismCommandRenderer.RenderCommand(DrawCommand command)
    {
        RenderCommand(command);
    }

    void IPrismCommandRenderer.DrawFullscreen(
        Texture2D texture,
        Rectangle destination)
    {
        _spriteBatch.Draw(texture, destination, XnaColor.White);
    }

    void IPrismCommandRenderer.RestoreHostTarget()
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        MonoGameGraphicsDeviceStateSnapshot snapshot =
            deviceStateSnapshot ??
            throw new InvalidOperationException(
                "The backend graphics state snapshot is unavailable.");
        snapshot.RestoreRenderTargetsAndViewport(graphicsDevice);
        if (clipStack is not null)
        {
            graphicsDevice.ScissorRectangle = clipStack.CurrentClip;
        }
    }

    private void ClearTextTextureCaches()
    {
        if (_textTextureCache is not null)
        {
            foreach (TextTexture text in _textTextureCache.Values)
            {
                ReleaseRgbTextures(text);
                text.MaskTexture?.Dispose();
            }

            _textTextureCache.Clear();
        }

        textTextureCacheMetadata?.Clear();

        if (textBrushTextureCache is null)
        {
            return;
        }

        foreach (Texture2D texture in textBrushTextureCache.Values)
        {
            texture.Dispose();
        }
        textBrushTextureCache.Clear();
        sharedTextTextureReferenceCounts?.Clear();
        textTextureCacheEstimatedBytes = 0;
    }

    private void MarkTextTextureUsed(TextTextureKey key)
    {
        activeTextTextureKeys ??= [];
        activeTextTextureKeys.Add(key);
        Dictionary<TextTextureKey, TextTextureCacheMetadata> metadata =
            textTextureCacheMetadata ??= new Dictionary<TextTextureKey, TextTextureCacheMetadata>();
        if (metadata.TryGetValue(key, out TextTextureCacheMetadata current))
        {
            metadata[key] = current with { LastUsedGeneration = textTextureCacheGeneration };
            return;
        }

        metadata[key] = new TextTextureCacheMetadata(
            textTextureCacheGeneration,
            textTextureCacheInsertionSequence++);
    }

    private void CompleteTextTextureFrame(int maximumEntries, long maximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (_textTextureCache is null)
        {
            return;
        }

        Dictionary<TextTextureKey, TextTextureCacheMetadata> metadata =
            textTextureCacheMetadata ??= new Dictionary<TextTextureKey, TextTextureCacheMetadata>();
        foreach (TextTextureKey key in _textTextureCache.Keys)
        {
            if (!metadata.ContainsKey(key))
            {
                metadata[key] = new TextTextureCacheMetadata(
                    activeTextTextureKeys?.Contains(key) == true ? textTextureCacheGeneration : -1,
                    textTextureCacheInsertionSequence++);
            }
            else if (activeTextTextureKeys?.Contains(key) == true)
            {
                metadata[key] = metadata[key] with { LastUsedGeneration = textTextureCacheGeneration };
            }
        }

        if (_textTextureCache.Count <= maximumEntries &&
            textTextureCacheEstimatedBytes <= maximumBytes)
        {
            textTextureCacheGeneration++;
            return;
        }

        List<TextTextureKey> candidates = textTextureEvictionCandidates ??= [];
        candidates.Clear();
        candidates.AddRange(_textTextureCache.Keys);
        candidates.Sort((left, right) =>
        {
            TextTextureCacheMetadata leftMetadata = metadata[left];
            TextTextureCacheMetadata rightMetadata = metadata[right];
            int generationOrder = leftMetadata.LastUsedGeneration.CompareTo(rightMetadata.LastUsedGeneration);
            return generationOrder != 0
                ? generationOrder
                : leftMetadata.InsertionSequence.CompareTo(rightMetadata.InsertionSequence);
        });

        int candidateIndex = 0;
        while ((_textTextureCache.Count > maximumEntries ||
                textTextureCacheEstimatedBytes > maximumBytes) &&
            candidateIndex < candidates.Count)
        {
            EvictTextTexture(candidates[candidateIndex++]);
        }

        textTextureCacheGeneration++;
    }

    private void EvictTextTexture(TextTextureKey key)
    {
        if (!_textTextureCache.Remove(key, out TextTexture text))
        {
            return;
        }

        long releasedBytes = ReleaseRgbTextures(text);
        if (text.MaskTexture is not null)
        {
            releasedBytes += EstimateBytes(text.MaskTexture);
            text.MaskTexture.Dispose();
        }
        textTextureCacheMetadata?.Remove(key);

        if (textBrushTextureCache is not null)
        {
            List<TextBrushTextureKey> dependentKeys = [];
            foreach (TextBrushTextureKey brushKey in textBrushTextureCache.Keys)
            {
                if (brushKey.Text.Equals(key))
                {
                    dependentKeys.Add(brushKey);
                }
            }

            foreach (TextBrushTextureKey brushKey in dependentKeys)
            {
                Texture2D texture = textBrushTextureCache[brushKey];
                releasedBytes += EstimateBytes(texture);
                texture.Dispose();
                textBrushTextureCache.Remove(brushKey);
            }
        }

        textTextureCacheEstimatedBytes = Math.Max(0, textTextureCacheEstimatedBytes - releasedBytes);
        textTextureCacheEvictions++;
    }

    private void ClearBrushTextureCache()
    {
        if (brushTextureCache is null)
        {
            return;
        }

        foreach (Texture2D texture in brushTextureCache.Values)
        {
            texture.Dispose();
        }

        brushTextureCache.Clear();
        activeBrushTextureKeys.Clear();
        brushTextureEvictionCandidates.Clear();
    }

    private void CompleteBrushTextureFrame()
    {
        if (brushTextureCache.Count == activeBrushTextureKeys.Count)
        {
            return;
        }

        brushTextureEvictionCandidates.Clear();
        foreach (BrushTextureKey key in brushTextureCache.Keys)
        {
            if (!activeBrushTextureKeys.Contains(key))
            {
                brushTextureEvictionCandidates.Add(key);
            }
        }

        foreach (BrushTextureKey key in brushTextureEvictionCandidates)
        {
            brushTextureCache[key].Dispose();
            brushTextureCache.Remove(key);
        }
    }

    private void ClearPathMeshCache()
    {
        if (pathMeshCache is null)
        {
            return;
        }
        pathMeshCache.Clear();
        strokeMeshCache.Clear();
    }

    private MonoGameDrawMapper Mapper => new(coordinateScale);

    private static Vector2 MapTextTexturePosition(DrawPoint position, DrawPoint originOffset, float coordinateScale)
    {
        Vector2 mapped = new MonoGameDrawMapper(coordinateScale).MapVector(position);
        return new Vector2(
            MathF.Round(mapped.X + originOffset.X),
            MathF.Round(mapped.Y + originOffset.Y));
    }

    private static DrawPoint GetCanonicalPixelPhase(DrawPoint position, float coordinateScale)
    {
        return new DrawPoint(
            CanonicalizePixelPhase(position.X * coordinateScale),
            CanonicalizePixelPhase(position.Y * coordinateScale));
    }

    private static float CanonicalizePixelPhase(float physicalPosition)
    {
        float phase = physicalPosition - MathF.Floor(physicalPosition);
        int bucket = (int)MathF.Floor((phase * TextSubpixelPhaseCount) + 0.5f);
        bucket %= TextSubpixelPhaseCount;
        return bucket / (float)TextSubpixelPhaseCount;
    }

    private static BlendState CreateTextBlendState(ColorWriteChannels channels)
    {
        return new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            ColorWriteChannels = channels
        };
    }

    private static BlendState CreateTextMaskBlendState()
    {
        return new BlendState
        {
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.SourceAlpha
        };
    }

    private static BlendState CreateMultiplyBlendState() =>
        new()
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

    private static BlendState CreateScreenBlendState() =>
        new()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceColor,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

    private static Vector2 MapTextTexturePositionForDiagnostics(DrawPoint position, DrawPoint originOffset, float coordinateScale)
    {
        return MapTextTexturePosition(position, originOffset, coordinateScale);
    }

    internal static DrawPoint GetCanonicalPixelPhaseForDiagnostics(DrawPoint position, float coordinateScale)
    {
        return GetCanonicalPixelPhase(position, coordinateScale);
    }

    internal static object CreateTextTextureKeyForDiagnostics(
        DrawTextRun textRun,
        float coordinateScale,
        DrawPoint position,
        CernealaColor rasterizationColor)
    {
        return TextTextureKey.From(
            textRun,
            coordinateScale,
            GetCanonicalPixelPhase(position, coordinateScale));
    }

    internal static object CreateTextBrushTextureKeyForDiagnostics(
        DrawTextRun textRun,
        float coordinateScale,
        DrawPoint position,
        IDrawBrush brush,
        float commandOpacity)
    {
        TextTextureKey textKey = TextTextureKey.From(
            textRun,
            coordinateScale,
            GetCanonicalPixelPhase(position, coordinateScale));
        return new TextBrushTextureKey(textKey, brush, commandOpacity);
    }

    internal bool UseTextTextureForDiagnostics(object key)
    {
        if (key is not TextTextureKey textKey || _textTextureCache?.ContainsKey(textKey) != true)
        {
            textTextureCacheMisses++;
            return false;
        }

        textTextureCacheHits++;
        MarkTextTextureUsed(textKey);
        return true;
    }

    internal void CompleteTextTextureFrameForDiagnostics(int maximumEntries, long maximumBytes)
    {
        CompleteTextTextureFrame(maximumEntries, maximumBytes);
    }

    internal void RenderClipCommandsForDiagnostics(DrawCommandList commands, Rectangle viewport)
    {
        ArgumentNullException.ThrowIfNull(commands);

        clipStack = new MonoGameClipStack(viewport);
        try
        {
            foreach (DrawCommand command in commands)
            {
                if (command.Kind == DrawCommandKind.PushClip)
                {
                    clipStack.Push(Mapper.MapRectangle(command.Rect));
                }
                else if (command.Kind == DrawCommandKind.PopClip)
                {
                    clipStack.Pop();
                }
            }
        }
        finally
        {
            clipStack.Reset();
        }
    }

    private static XnaColor ToColor(CernealaColor color)
    {
        return new XnaColor(color.R, color.G, color.B, color.A);
    }

    private static XnaColor Premultiply(XnaColor color)
    {
        return new XnaColor(
            (byte)(((color.R * color.A) + 127) / 255),
            (byte)(((color.G * color.A) + 127) / 255),
            (byte)(((color.B * color.A) + 127) / 255),
            color.A);
    }

    private static long EstimateBytes(TextTexture text)
    {
        long rgbBytes = EstimateBytes(text.RedTexture);
        if (!ReferenceEquals(text.GreenTexture, text.RedTexture))
        {
            rgbBytes += EstimateBytes(text.GreenTexture);
        }

        if (!ReferenceEquals(text.BlueTexture, text.RedTexture) &&
            !ReferenceEquals(text.BlueTexture, text.GreenTexture))
        {
            rgbBytes += EstimateBytes(text.BlueTexture);
        }

        return rgbBytes +
            (text.MaskTexture is null ? 0 : EstimateBytes(text.MaskTexture));
    }

    private long ReleaseRgbTextures(TextTexture text)
    {
        long releasedBytes = ReleaseTextTexture(text.RedTexture);
        if (!ReferenceEquals(text.GreenTexture, text.RedTexture))
        {
            releasedBytes += ReleaseTextTexture(text.GreenTexture);
        }

        if (!ReferenceEquals(text.BlueTexture, text.RedTexture) &&
            !ReferenceEquals(text.BlueTexture, text.GreenTexture))
        {
            releasedBytes += ReleaseTextTexture(text.BlueTexture);
        }

        return releasedBytes;
    }

    private long ReleaseTextTexture(Texture2D texture)
    {
        if (sharedTextTextureReferenceCounts is not null &&
            sharedTextTextureReferenceCounts.TryGetValue(texture, out int references))
        {
            if (references > 1)
            {
                sharedTextTextureReferenceCounts[texture] = references - 1;
                return 0;
            }

            sharedTextTextureReferenceCounts.Remove(texture);
        }

        long releasedBytes = EstimateBytes(texture);
        texture.Dispose();
        return releasedBytes;
    }

    private static long EstimateBytes(Texture2D texture)
    {
        return (long)texture.Width * texture.Height * 4;
    }

    internal readonly record struct TextTextureCacheDiagnosticSnapshot(
        long Hits,
        long Misses,
        long Evictions,
        long EstimatedBytes);

    private readonly record struct TextTextureCacheMetadata(
        long LastUsedGeneration,
        long InsertionSequence);

    private readonly record struct TextRasterizationRequest(
        DrawTextRun TextRun,
        DrawPoint PixelPhase,
        bool NeedsMask);

    private readonly record struct TextTextureKey(
        string Text,
        object FontIdentity,
        float FontSize,
        float CoordinateScale,
        DrawPoint PixelPhase)
    {
        public static TextTextureKey From(
            DrawTextRun textRun,
            float coordinateScale,
            DrawPoint pixelPhase)
        {
            return new TextTextureKey(
                textRun.Text,
                textRun.Font is SkiaFont skiaFont ? skiaFont.Typeface : textRun.Font,
                textRun.Size,
                coordinateScale,
                pixelPhase);
        }
    }

    private readonly record struct TextTexture(
        Texture2D RedTexture,
        Texture2D GreenTexture,
        Texture2D BlueTexture,
        Texture2D? MaskTexture,
        DrawPoint OriginOffset)
    {
        public Rectangle? RedSource { get; init; }

        public Rectangle? GreenSource { get; init; }

        public Rectangle? BlueSource { get; init; }
    }

    private readonly record struct TextBrushTextureKey(
        TextTextureKey Text,
        IDrawBrush Brush,
        float CommandOpacity);

    private readonly record struct BrushTextureKey(
        IDrawBrush Brush,
        DrawRect Bounds,
        int Width,
        int Height,
        float CoordinateScale,
        int ContentVersion);

    private readonly record struct PathMeshKey(
        long PathStableId,
        DrawFillRule FillRule,
        DrawRect SourceBounds,
        int Width,
        int Height,
        float PhysicalWidth,
        float PhysicalHeight,
        float PhaseX,
        float PhaseY,
        XnaColor Color);

    private readonly record struct StrokeMeshKey(
        DrawCommandKind Kind,
        long PathStableId,
        DrawRect Bounds,
        DrawRect SourceBounds,
        DrawPoint Start,
        DrawPoint End,
        DrawPen? Pen,
        CernealaColor Color,
        float Thickness,
        float BrushOpacity,
        float CoordinateScale)
    {
        public static StrokeMeshKey From(
            DrawCommand command,
            float coordinateScale)
        {
            return new StrokeMeshKey(
                command.Kind,
                command.Path?.StableId ?? 0,
                command.Rect,
                command.SourceRect,
                command.Position,
                command.EndPoint,
                command.Pen,
                command.Color,
                command.Thickness,
                command.BrushOpacity,
                coordinateScale);
        }
    }

    private readonly record struct DrawingLayerScope(
        RenderTarget2D Target,
        RenderTargetBinding[] HostTargets,
        Viewport HostViewport,
        Rectangle HostScissor,
        float Opacity,
        DrawBlendMode BlendMode,
        bool IsGeometricClip);
}
