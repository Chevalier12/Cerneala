using System.Buffers;
using System.Diagnostics;
using Cerneala.Drawing.Text;
using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend, IDrawingBackendFrameTimingSource, IDisposable
{
    private const int TextSubpixelPhaseCount = 8;
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
    private readonly Dictionary<PathMeshKey, MonoGamePathMesh> pathMeshCache = new();
    private readonly HashSet<IDrawBrush> activeBrushes = new(ReferenceEqualityComparer.Instance);
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;
    private BasicEffect? pathEffect;
    private RasterizerState? pathRasterizerState;
    private readonly BlendState redTextBlendState;
    private readonly BlendState greenTextBlendState;
    private readonly BlendState blueTextBlendState;
    private readonly BlendState textMaskBlendState;
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

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        _textRasterizer = textRasterizer;
        redTextBlendState = CreateTextBlendState(ColorWriteChannels.Red);
        greenTextBlendState = CreateTextBlendState(ColorWriteChannels.Green);
        blueTextBlendState = CreateTextBlendState(ColorWriteChannels.Blue);
        textMaskBlendState = CreateTextMaskBlendState();
        if (_spriteBatch.GraphicsDevice is GraphicsDevice graphicsDevice)
        {
            CreatePathResources(graphicsDevice);
            graphicsDevice.DeviceReset += OnDeviceReset;
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
            }

            coordinateScale = value;
        }
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);

        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        Rectangle previousScissor = graphicsDevice.ScissorRectangle;
        Rectangle viewportClip = new(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        graphicsDevice.ScissorRectangle = viewportClip;
        clipStack = new MonoGameClipStack(viewportClip);
        activeTextTextureKeys.Clear();
        lastTextRequestCollectionTime = TimeSpan.Zero;
        lastTextRasterizationTime = TimeSpan.Zero;
        lastTextAtlasUploadTime = TimeSpan.Zero;
        lastTextRequestCount = 0;
        lastRasterizedPixelCount = 0;
        long preparationStarted = Stopwatch.GetTimestamp();
        PrepareTextRasterizations(commands);
        TimeSpan preparationTime = Stopwatch.GetElapsedTime(preparationStarted);
        long commandRenderingStarted = Stopwatch.GetTimestamp();

        try
        {
            for (int index = 0; index < commands.Count; index++)
            {
                RenderCommand(commands[index]);
            }
        }
        finally
        {
            TimeSpan commandRenderingTime = Stopwatch.GetElapsedTime(commandRenderingStarted);
            long cleanupStarted = Stopwatch.GetTimestamp();
            clipStack.Reset();
            graphicsDevice.ScissorRectangle = previousScissor;
            foreach (RasterizedText[] layers in preparedTextRasterizations.Values)
            {
                ReturnRasterizedTextPixels(layers);
            }
            preparedTextRasterizations.Clear();
            preparedTextTextureKeys.Clear();
            CompleteTextTextureFrame(
                DefaultMaximumTextTextureEntries,
                DefaultMaximumTextTextureBytes);
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

    private void RenderCommand(DrawCommand command)
    {
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
                if (command.Brush is null)
                {
                    DrawRectangle(command.Rect, command.Color, command.Thickness);
                }
                else
                {
                    DrawRectangle(command.Rect, command.Brush, command.Thickness, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.FillEllipse:
                if (command.Brush is null)
                {
                    FillEllipse(command.Rect, command.Color);
                }
                else
                {
                    FillEllipse(command.Rect, command.Brush, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.DrawEllipse:
                if (command.Brush is null)
                {
                    DrawEllipse(command.Rect, command.Color, command.Thickness);
                }
                else
                {
                    DrawEllipse(command.Rect, command.Brush, command.Thickness, command.BrushOpacity);
                }
                break;

            case DrawCommandKind.DrawLine:
                if (command.Brush is null)
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

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawText:
                DrawText(command);
                break;

            case DrawCommandKind.PushClip:
                PushClip(command.Rect);
                break;

            case DrawCommandKind.PopClip:
                PopClip();
                break;

            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void FillRectangle(DrawRect rect, CernealaColor color)
    {
        _spriteBatch.Draw(_whitePixel, Mapper.MapRectangle(rect), ToColor(color));
    }

    private void FillRectangle(DrawRect rect, IDrawBrush brush, float commandOpacity)
    {
        DrawBrushDescriptor descriptor = brush.CreateDescriptor();
        if (TryGetSolidColor(descriptor, commandOpacity, out XnaColor solid))
        {
            _spriteBatch.Draw(_whitePixel, Mapper.MapRectangle(rect), solid);
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
        XnaColor monoGameColor = ToColor(color);

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

    private void FillEllipse(DrawRect rect, CernealaColor color)
    {
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        XnaColor monoGameColor = ToColor(color);
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

        if (!ReferenceEquals(image.Texture.GraphicsDevice, _spriteBatch.GraphicsDevice))
        {
            throw new InvalidOperationException("A MonoGameImage can only be drawn by the GraphicsDevice that created it.");
        }

        _spriteBatch.Draw(image.Texture, Mapper.MapRectangle(command.Rect), ToColor(command.Color));
    }

    private void FillPath(DrawCommand command)
    {
        float physicalLeft = command.Rect.X * coordinateScale;
        float physicalTop = command.Rect.Y * coordinateScale;
        float physicalRight = command.Rect.Right * coordinateScale;
        float physicalBottom = command.Rect.Bottom * coordinateScale;
        int left = (int)MathF.Floor(physicalLeft);
        int top = (int)MathF.Floor(physicalTop);
        int right = (int)MathF.Ceiling(physicalRight);
        int bottom = (int)MathF.Ceiling(physicalBottom);
        Rectangle destination = new(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        if (destination.Width <= 0 || destination.Height <= 0 || command.PathData is null || command.Brush is null)
        {
            return;
        }

        if (!TryGetSolidColor(command.Brush.CreateDescriptor(), command.BrushOpacity, out XnaColor color))
        {
            throw new NotSupportedException("Filled paths currently require a solid brush.");
        }

        float phaseX = physicalLeft - left;
        float phaseY = physicalTop - top;
        float physicalWidth = physicalRight - physicalLeft;
        float physicalHeight = physicalBottom - physicalTop;
        PathMeshKey key = new(
            command.PathData,
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
                command.PathData,
                command.SourceRect,
                physicalWidth,
                physicalHeight,
                phaseX,
                phaseY,
                Premultiply(color));
            pathMeshCache.Add(key, mesh);
        }

        if (!mesh.IsEmpty)
        {
            DrawPathMesh(mesh, left, top);
        }
    }

    private void DrawPathMesh(MonoGamePathMesh mesh, int left, int top)
    {
        GraphicsDevice device = _spriteBatch.GraphicsDevice;
        CreatePathResources(device);
        BlendState previousBlend = device.BlendState;
        SamplerState previousSampler = device.SamplerStates[0];
        DepthStencilState previousDepth = device.DepthStencilState;
        RasterizerState previousRasterizer = device.RasterizerState;

        _spriteBatch.End();
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = pathRasterizerState!;
            pathEffect!.World = Matrix.CreateTranslation(left, top, 0);
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
            _spriteBatch.Begin(
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

        CernealaColor rasterizationColor = solidTextColor is XnaColor brushColor
            ? new CernealaColor(brushColor.R, brushColor.G, brushColor.B)
            : command.Brush is null
                ? new CernealaColor(command.Color.R, command.Color.G, command.Color.B)
                : CernealaColor.White;
        DrawPoint pixelPhase = GetCanonicalPixelPhase(command.Position, coordinateScale);
        TextTextureKey key = TextTextureKey.FromWithRasterizationColor(command.TextRun, coordinateScale, pixelPhase, rasterizationColor);
        bool needsMask = command.Brush is not null && solidTextColor is null;

        if (!_textTextureCache.TryGetValue(key, out TextTexture cachedText))
        {
            textTextureCacheMisses++;
            RasterizedText[] layers = preparedTextRasterizations.Remove(key, out RasterizedText[]? prepared)
                ? prepared
                : _textRasterizer.RasterizeSubpixelAtPhase(
                    command.TextRun,
                    rasterizationColor,
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
                rasterizationColor,
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
            if (command.Kind != DrawCommandKind.DrawText || command.TextRun is null)
            {
                continue;
            }

            CernealaColor rasterizationColor = ResolveTextRasterizationColor(command);
            DrawPoint pixelPhase = GetCanonicalPixelPhase(command.Position, coordinateScale);
            TextTextureKey key = TextTextureKey.FromWithRasterizationColor(
                command.TextRun,
                coordinateScale,
                pixelPhase,
                rasterizationColor);
            if (!_textTextureCache.ContainsKey(key))
            {
                bool needsMask = command.Brush is not null &&
                    !TryGetSolidColor(command.Brush.CreateDescriptor(), command.BrushOpacity, out _);
                textRasterizationRequests.TryAdd(
                    key,
                    new TextRasterizationRequest(
                        command.TextRun,
                        rasterizationColor,
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
                    request.RasterizationColor,
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
                            request.RasterizationColor,
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

    private static CernealaColor ResolveTextRasterizationColor(DrawCommand command)
    {
        if (command.Brush is not null &&
            TryGetSolidColor(command.Brush.CreateDescriptor(), command.BrushOpacity, out XnaColor solid))
        {
            return new CernealaColor(solid.R, solid.G, solid.B);
        }

        return command.Brush is null
            ? new CernealaColor(command.Color.R, command.Color.G, command.Color.B)
            : CernealaColor.White;
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

        _spriteBatch.End();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(XnaColor.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);
            clipStack = new MonoGameClipStack(device.ScissorRectangle);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, previousSampler, DepthStencilState.None, previousRasterizer);
            FillRectangle(localBounds, brush, commandOpacity);
            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.Immediate, textMaskBlendState, SamplerState.PointClamp, DepthStencilState.None, previousRasterizer);
            _spriteBatch.Draw(mask, Vector2.Zero, XnaColor.White);
            _spriteBatch.End();
        }
        catch
        {
            target.Dispose();
            throw;
        }
        finally
        {
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
            _spriteBatch.Begin(SpriteSortMode.Immediate, previousBlend, previousSampler, previousDepth, previousRasterizer);
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

    private Texture2D GetOrCreateBrushTexture(IDrawBrush brush, DrawBrushDescriptor descriptor, DrawRect rect)
    {
        Rectangle pixelBounds = Mapper.MapRectangle(rect);
        int width = Math.Max(1, pixelBounds.Width);
        int height = Math.Max(1, pixelBounds.Height);
        BrushTextureKey key = new(brush, rect, width, height, coordinateScale, 0);
        if (brushTextureCache.TryGetValue(key, out Texture2D? cached))
        {
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
        texture.SetData(pixels.Select(ToColor).ToArray());
        brushTextureCache.Add(key, texture);
        return texture;
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

        _spriteBatch.End();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(XnaColor.Transparent);
            device.ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);
            clipStack = new MonoGameClipStack(device.ScissorRectangle);
            _spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                previousRasterizer);
            try
            {
                foreach (DrawCommand command in commands)
                {
                    RenderCommand(MapBrushCommand(command, contentBounds, localTile, 1, flipX: false, flipY: false));
                }
            }
            finally
            {
                _spriteBatch.End();
            }
        }
        catch
        {
            target.Dispose();
            throw;
        }
        finally
        {
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
            _spriteBatch.Begin(
                SpriteSortMode.Immediate,
                previousBlend,
                previousSampler,
                previousDepth,
                previousRasterizer);
        }

        brushTextureCache.Add(key, target);
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
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(MapRect(command.Rect), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(MapRect(command.Rect), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(MapRect(command.Rect), command.Brush, command.Thickness * thicknessScale, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(MapRect(command.Rect), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(MapPoint(command.Position), MapPoint(command.EndPoint), command.Brush, command.Thickness * thicknessScale, command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(MapPoint(command.Position), MapPoint(command.EndPoint), ApplyOpacity(command.Color, opacity), command.Thickness * thicknessScale),
            DrawCommandKind.FillPath => DrawCommand.FillPath(command.PathData!, command.SourceRect, MapRect(command.Rect), command.Brush!, command.BrushOpacity * opacity),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(command.Image!, MapRect(command.Rect), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(command.TextRun!, MapPoint(command.Position), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, MapPoint(command.Position), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.PushClip => DrawCommand.PushClip(MapRect(command.Rect)),
            DrawCommandKind.PopClip => command,
            _ => command
        };
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
        if (_spriteBatch?.GraphicsDevice is GraphicsDevice graphicsDevice)
        {
            graphicsDevice.DeviceReset -= OnDeviceReset;
        }
        redTextBlendState?.Dispose();
        greenTextBlendState?.Dispose();
        blueTextBlendState?.Dispose();
        textMaskBlendState?.Dispose();
        pathEffect?.Dispose();
        pathRasterizerState?.Dispose();
        disposed = true;
    }

    internal int ClipStackDepth => clipStack?.Depth ?? 0;

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

    private void OnDeviceReset(object? sender, EventArgs args)
    {
        ClearTextTextureCaches();
        ClearBrushTextureCache();
        ClearPathMeshCache();
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
    }

    private void ClearPathMeshCache()
    {
        if (pathMeshCache is null)
        {
            return;
        }
        pathMeshCache.Clear();
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
        return TextTextureKey.FromWithRasterizationColor(
            textRun,
            coordinateScale,
            GetCanonicalPixelPhase(position, coordinateScale),
            rasterizationColor);
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
        CernealaColor RasterizationColor,
        DrawPoint PixelPhase,
        bool NeedsMask);

    private readonly record struct TextTextureKey(
        string Text,
        object FontIdentity,
        float FontSize,
        float CoordinateScale,
        DrawPoint PixelPhase,
        CernealaColor RasterizationColor)
    {
        public static TextTextureKey From(
            DrawTextRun textRun,
            float coordinateScale,
            DrawPoint pixelPhase)
        {
            return FromWithRasterizationColor(textRun, coordinateScale, pixelPhase, CernealaColor.White);
        }

        public static TextTextureKey FromWithRasterizationColor(
            DrawTextRun textRun,
            float coordinateScale,
            DrawPoint pixelPhase,
            CernealaColor rasterizationColor)
        {
            return new TextTextureKey(
                textRun.Text,
                textRun.Font is SkiaFont skiaFont ? skiaFont.Typeface : textRun.Font,
                textRun.Size,
                coordinateScale,
                pixelPhase,
                rasterizationColor);
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
        string Data,
        DrawRect SourceBounds,
        int Width,
        int Height,
        float PhysicalWidth,
        float PhysicalHeight,
        float PhaseX,
        float PhaseY,
        XnaColor Color);
}
