namespace Cerneala.Drawing.Prism.ColorManagement;

internal static class PrismSrgbStyle
{
    internal static PrismColorChannels DecodeInput(PrismColorChannels value) =>
        PrismColorPipeline.DecodeSrgb(value);

    internal static PrismColorChannels EncodeOutput(PrismColorChannels value) =>
        PrismColorPipeline.EncodeSrgb(value);
}
