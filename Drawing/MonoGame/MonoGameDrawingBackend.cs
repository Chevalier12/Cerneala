using Cerneala.Drawing.Text;
using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend, IDisposable
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Dictionary<TextTextureKey, TextTexture> _textTextureCache = new();
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;
    private readonly BlendState redTextBlendState;
    private readonly BlendState greenTextBlendState;
    private readonly BlendState blueTextBlendState;
    private float coordinateScale = 1;
    private bool disposed;
    private MonoGameClipStack? clipStack;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        _textRasterizer = textRasterizer;
        redTextBlendState = CreateTextBlendState(ColorWriteChannels.Red);
        greenTextBlendState = CreateTextBlendState(ColorWriteChannels.Green);
        blueTextBlendState = CreateTextBlendState(ColorWriteChannels.Blue);
    }

    public static RasterizerState ScissorRasterizerState => new() { ScissorTestEnable = true };

    public float CoordinateScale
    {
        get => coordinateScale;
        set
        {
            UiCoordinateMapper.ValidateScale(value);
            coordinateScale = value;
        }
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);

        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        Rectangle previousScissor = graphicsDevice.ScissorRectangle;
        clipStack = new MonoGameClipStack(new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height));

        try
        {
            foreach (DrawCommand command in commands)
            {
                RenderCommand(command);
            }
        }
        finally
        {
            clipStack.Reset();
            graphicsDevice.ScissorRectangle = previousScissor;
        }
    }

    private void RenderCommand(DrawCommand command)
    {
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                FillRectangle(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawRectangle:
                DrawRectangle(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.FillEllipse:
                FillEllipse(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawEllipse:
                DrawEllipse(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.DrawLine:
                DrawLine(command.Position, command.EndPoint, command.Color, command.Thickness);
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

    private void FillRectangle(DrawRect rect, DrawColor color)
    {
        _spriteBatch.Draw(_whitePixel, Mapper.MapRectangle(rect), ToColor(color));
    }

    private void DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Mapper.MapThickness(thickness);
        Rectangle bounds = Mapper.MapRectangle(rect);
        Color monoGameColor = ToColor(color);

        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Bottom - lineThickness, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, lineThickness, bounds.Height), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - lineThickness, bounds.Top, lineThickness, bounds.Height), monoGameColor);
    }

    private void FillEllipse(DrawRect rect, DrawColor color)
    {
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Color monoGameColor = ToColor(color);
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

    private void DrawEllipse(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Mapper.MapThickness(thickness);
        Rectangle bounds = Mapper.MapRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawEllipseRing(bounds, ToColor(color), lineThickness);
    }

    private void DrawEllipseRing(Rectangle bounds, Color color, int thickness)
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

    private void DrawLine(DrawPoint start, DrawPoint end, DrawColor color, float thickness)
    {
        DrawLine(Mapper.MapVector(start), Mapper.MapVector(end), ToColor(color), Mapper.MapThickness(thickness));
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0)
        {
            _spriteBatch.Draw(_whitePixel, new Rectangle((int)MathF.Round(start.X), (int)MathF.Round(start.Y), thickness, thickness), color);
            return;
        }

        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _whitePixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
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

    private void DrawText(DrawCommand command)
    {
        if (_textRasterizer is null || command.TextRun is null)
        {
            return;
        }

        DrawPoint pixelPhase = GetPixelPhase(command.Position, coordinateScale);
        TextTextureKey key = TextTextureKey.From(command.TextRun, command.Color, coordinateScale, pixelPhase);

        if (!_textTextureCache.TryGetValue(key, out TextTexture cachedText))
        {
            RasterizedText[] layers = _textRasterizer.RasterizeSubpixel(
                command.TextRun,
                command.Color,
                coordinateScale,
                command.Position);
            cachedText = new TextTexture(
                CreateTexture(layers[0]),
                CreateTexture(layers[1]),
                CreateTexture(layers[2]),
                layers[0].OriginOffset);
            _textTextureCache.Add(key, cachedText);
        }

        Vector2 origin = MapTextTexturePosition(command.Position, cachedText.OriginOffset, coordinateScale);
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        BlendState previousBlendState = graphicsDevice.BlendState;
        try
        {
            DrawTextLayer(cachedText.RedTexture, origin, redTextBlendState);
            DrawTextLayer(cachedText.GreenTexture, origin, greenTextBlendState);
            DrawTextLayer(cachedText.BlueTexture, origin, blueTextBlendState);
        }
        finally
        {
            graphicsDevice.BlendState = previousBlendState;
        }
    }

    private Texture2D CreateTexture(RasterizedText text)
    {
        Texture2D texture = new(_spriteBatch.GraphicsDevice, text.Width, text.Height);
        texture.SetData(text.RgbaPixels);
        return texture;
    }

    private void DrawTextLayer(Texture2D texture, Vector2 origin, BlendState blendState)
    {
        _spriteBatch.GraphicsDevice.BlendState = blendState;
        _spriteBatch.Draw(texture, origin, Color.White);
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

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (TextTexture text in _textTextureCache.Values)
        {
            text.RedTexture.Dispose();
            text.GreenTexture.Dispose();
            text.BlueTexture.Dispose();
        }

        _textTextureCache.Clear();
        redTextBlendState?.Dispose();
        greenTextBlendState?.Dispose();
        blueTextBlendState?.Dispose();
        disposed = true;
    }

    internal int ClipStackDepth => clipStack?.Depth ?? 0;

    internal int TextTextureCacheCount => _textTextureCache.Count;

    private MonoGameDrawMapper Mapper => new(coordinateScale);

    private static Vector2 MapTextTexturePosition(DrawPoint position, DrawPoint originOffset, float coordinateScale)
    {
        Vector2 mapped = new MonoGameDrawMapper(coordinateScale).MapVector(position);
        return new Vector2(
            MathF.Round(mapped.X + originOffset.X),
            MathF.Round(mapped.Y + originOffset.Y));
    }

    private static DrawPoint GetPixelPhase(DrawPoint position, float coordinateScale)
    {
        float x = position.X * coordinateScale;
        float y = position.Y * coordinateScale;
        return new DrawPoint(x - MathF.Floor(x), y - MathF.Floor(y));
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

    private static Vector2 MapTextTexturePositionForDiagnostics(DrawPoint position, DrawPoint originOffset, float coordinateScale)
    {
        return MapTextTexturePosition(position, originOffset, coordinateScale);
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

    private static Color ToColor(DrawColor color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }

    private readonly record struct TextTextureKey(
        string Text,
        IDrawFont Font,
        float FontSize,
        DrawColor Color,
        float CoordinateScale,
        DrawPoint PixelPhase)
    {
        public static TextTextureKey From(
            DrawTextRun textRun,
            DrawColor color,
            float coordinateScale,
            DrawPoint pixelPhase)
        {
            return new TextTextureKey(
                textRun.Text,
                textRun.Font,
                textRun.Size,
                color,
                coordinateScale,
                pixelPhase);
        }
    }

    private readonly record struct TextTexture(
        Texture2D RedTexture,
        Texture2D GreenTexture,
        Texture2D BlueTexture,
        DrawPoint OriginOffset);
}
