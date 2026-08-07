namespace Cerneala.Drawing.Prism.ColorManagement;

internal static class PrismDisplayP3Style
{
    internal static PrismColorChannels DecodeInput(PrismColorChannels value) =>
        PrismColorPipeline.LinearDisplayP3ToLinearSrgb(
            PrismColorPipeline.DecodeSrgb(value));

    internal static PrismColorChannels EncodeOutput(PrismColorChannels value) =>
        PrismColorPipeline.EncodeSrgb(
            PrismColorPipeline.LinearSrgbToLinearDisplayP3(value));
}
