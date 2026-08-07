namespace Cerneala.Drawing.Prism.ColorManagement;

internal static class PrismLinearDisplayP3Style
{
    internal static PrismColorChannels DecodeInput(PrismColorChannels value) =>
        PrismColorPipeline.LinearDisplayP3ToLinearSrgb(value);

    internal static PrismColorChannels EncodeOutput(PrismColorChannels value) =>
        PrismColorPipeline.LinearSrgbToLinearDisplayP3(value);
}
