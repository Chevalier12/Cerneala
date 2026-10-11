namespace Cerneala.Tests.VisualStudio;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cerneala.VisualStudio.Preview;

public sealed class PreviewPixelsTests
{
    [Fact]
    public void ProtocolFrameDecodesToTheMarkupColorInTheClientBitmapFormat()
    {
        // PanelAltBrush #FF0D1014 as the preview host sends it: R, G, B, A.
        byte[] frame = [13, 16, 20, 255, 255, 0, 0, 255];

        CernealaPreviewPixels.ConvertRgbaToBgra(frame, width: 2, height: 1, stride: 8);

        byte[] rgb = DecodeAsRgb(frame, width: 2, height: 1, stride: 8);
        Assert.Equal(new byte[] { 13, 16, 20, 255, 0, 0 }, rgb);
    }

    [Fact]
    public void ConversionSkipsRowPadding()
    {
        byte[] frame =
        [
            1, 2, 3, 4, 99, 98, 97, 96,
            5, 6, 7, 8, 95, 94, 93, 92
        ];

        CernealaPreviewPixels.ConvertRgbaToBgra(frame, width: 1, height: 2, stride: 8);

        Assert.Equal(
            new byte[]
            {
                3, 2, 1, 4, 99, 98, 97, 96,
                7, 6, 5, 8, 95, 94, 93, 92
            },
            frame);
    }

    private static byte[] DecodeAsRgb(byte[] bgra, int width, int height, int stride)
    {
        // Decode through WPF with the same pixel format the preview session uses.
        BitmapSource source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, stride);
        FormatConvertedBitmap converted = new(source, PixelFormats.Rgb24, null, 0);
        byte[] rgb = new byte[width * height * 3];
        converted.CopyPixels(rgb, width * 3, 0);
        return rgb;
    }
}
