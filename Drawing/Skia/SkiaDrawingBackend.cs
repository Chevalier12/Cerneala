using Cerneala.Drawing.Text;
using SkiaSharp;

namespace Cerneala.Drawing.Skia;

internal sealed class SkiaDrawingBackend : IDrawingBackend, IDisposable
{
    private SKBitmap bitmap;
    private SKCanvas canvas;
    private float coordinateScale;
    private bool disposed;
    private int clipDepth;

    public SkiaDrawingBackend(int pixelWidth, int pixelHeight, float coordinateScale)
    {
        this.coordinateScale = coordinateScale;
        bitmap = CreateBitmap(pixelWidth, pixelHeight);
        canvas = new SKCanvas(bitmap);
    }

    public int PixelWidth => bitmap.Width;

    public int PixelHeight => bitmap.Height;

    public nint Pixels => bitmap.GetPixels();

    public int RowBytes => bitmap.RowBytes;

    public void Resize(int pixelWidth, int pixelHeight, float scale)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (pixelWidth == bitmap.Width && pixelHeight == bitmap.Height && scale == coordinateScale)
        {
            return;
        }

        canvas.Dispose();
        bitmap.Dispose();
        coordinateScale = scale;
        bitmap = CreateBitmap(pixelWidth, pixelHeight);
        canvas = new SKCanvas(bitmap);
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);
        canvas.ResetMatrix();
        while (clipDepth > 0)
        {
            canvas.Restore();
            clipDepth--;
        }

        canvas.Clear(SKColors.White);
        canvas.Scale(coordinateScale);
        foreach (DrawCommand command in commands)
        {
            RenderCommand(command);
        }

        canvas.Flush();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        canvas.Dispose();
        bitmap.Dispose();
        disposed = true;
    }

    private void RenderCommand(DrawCommand command)
    {
        using SKPaint paint = CreatePaint(command.Color);
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                canvas.DrawRect(ToRect(command.Rect), paint);
                break;
            case DrawCommandKind.DrawRectangle:
                ConfigureStroke(paint, command.Thickness);
                canvas.DrawRect(ToRect(command.Rect), paint);
                break;
            case DrawCommandKind.FillEllipse:
                canvas.DrawOval(ToRect(command.Rect), paint);
                break;
            case DrawCommandKind.DrawEllipse:
                ConfigureStroke(paint, command.Thickness);
                canvas.DrawOval(ToRect(command.Rect), paint);
                break;
            case DrawCommandKind.DrawLine:
                ConfigureStroke(paint, command.Thickness);
                canvas.DrawLine(command.Position.X, command.Position.Y, command.EndPoint.X, command.EndPoint.Y, paint);
                break;
            case DrawCommandKind.DrawText:
                DrawText(command, paint);
                break;
            case DrawCommandKind.DrawImage:
                DrawImage(command, paint);
                break;
            case DrawCommandKind.PushClip:
                canvas.Save();
                clipDepth++;
                canvas.ClipRect(ToRect(command.Rect));
                break;
            case DrawCommandKind.PopClip:
                if (clipDepth == 0)
                {
                    throw new InvalidOperationException("Draw command list contains an unmatched PopClip command.");
                }

                canvas.Restore();
                clipDepth--;
                break;
            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void DrawText(DrawCommand command, SKPaint paint)
    {
        if (command.TextRun?.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaDrawingBackend requires SkiaFont for text commands.");
        }

        using SKFont skFont = new(font.Typeface, command.TextRun.Size);
        canvas.DrawText(
            command.TextRun.Text,
            command.Position.X,
            command.Position.Y + command.TextRun.Size,
            SKTextAlign.Left,
            skFont,
            paint);
    }

    private void DrawImage(DrawCommand command, SKPaint paint)
    {
        if (command.Image is not SkiaDrawImage image)
        {
            throw new InvalidOperationException("SkiaDrawingBackend requires SkiaDrawImage for image commands.");
        }

        canvas.DrawImage(
            image.Image,
            ToRect(command.Rect),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None),
            paint);
    }

    private static SKBitmap CreateBitmap(int width, int height)
    {
        return new SKBitmap(
            Math.Max(1, width),
            Math.Max(1, height),
            SKColorType.Bgra8888,
            SKAlphaType.Premul);
    }

    private static SKPaint CreatePaint(DrawColor color)
    {
        return new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
    }

    private static void ConfigureStroke(SKPaint paint, float thickness)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = thickness;
    }

    private static SKRect ToRect(DrawRect rect)
    {
        return new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    }
}
