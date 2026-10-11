namespace Cerneala.VisualStudio.Preview;

using System;

internal static class CernealaPreviewPixels
{
    // Preview frames arrive in R, G, B, A byte order (see PreviewResponse.Image).
    // WPF PixelFormats.Bgra32 reads byte 0 as blue, so red and blue are swapped
    // in place before the frame is written into the bitmap.
    public static void ConvertRgbaToBgra(byte[] pixels, int width, int height, int stride)
    {
        if (pixels is null)
        {
            throw new ArgumentNullException(nameof(pixels));
        }

        if (width < 0 || height < 0 || stride < checked(width * 4) ||
            pixels.Length < checked(stride * height))
        {
            throw new ArgumentException("The preview frame dimensions do not match its pixel buffer.", nameof(pixels));
        }

        for (int y = 0; y < height; y++)
        {
            int rowEnd = y * stride + width * 4;
            for (int offset = y * stride; offset < rowEnd; offset += 4)
            {
                byte red = pixels[offset];
                pixels[offset] = pixels[offset + 2];
                pixels[offset + 2] = red;
            }
        }
    }
}
