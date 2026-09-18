using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism;

// Keep admission independent of runtime enum metadata and catalog initialization.
// Tests compare these explicit sets with the declared enums, including invalid IDs.
internal static class PrismEnumValidation
{
    public static bool IsDefined(PrismBlendMode value) => value is
        PrismBlendMode.Normal or PrismBlendMode.Dissolve or
        PrismBlendMode.Darken or PrismBlendMode.Multiply or
        PrismBlendMode.ColorBurn or PrismBlendMode.LinearBurn or
        PrismBlendMode.DarkerColor or PrismBlendMode.Lighten or
        PrismBlendMode.Screen or PrismBlendMode.ColorDodge or
        PrismBlendMode.LinearDodge or PrismBlendMode.LighterColor or
        PrismBlendMode.Overlay or PrismBlendMode.SoftLight or
        PrismBlendMode.HardLight or PrismBlendMode.VividLight or
        PrismBlendMode.LinearLight or PrismBlendMode.PinLight or
        PrismBlendMode.HardMix or PrismBlendMode.Difference or
        PrismBlendMode.Exclusion or PrismBlendMode.Subtract or
        PrismBlendMode.Divide or PrismBlendMode.Hue or
        PrismBlendMode.Saturation or PrismBlendMode.Color or
        PrismBlendMode.Luminosity or PrismBlendMode.PassThrough;

    public static bool IsDefined(PrismColorProfile value) => value is
        PrismColorProfile.LinearSrgb or PrismColorProfile.Srgb or
        PrismColorProfile.LinearDisplayP3 or PrismColorProfile.DisplayP3 or
        PrismColorProfile.ScRgb;

    public static bool IsDefined(BackdropPixelFormat value) => value is
        BackdropPixelFormat.Rgba8Unorm or BackdropPixelFormat.Bgra8Unorm or
        BackdropPixelFormat.Rgba16Float;

    public static bool IsDefined(BackdropAlphaMode value) => value is
        BackdropAlphaMode.Opaque or BackdropAlphaMode.Premultiplied or
        BackdropAlphaMode.Straight;

    public static bool IsDefined(PrismSampling value) => value is PrismSampling.Linear;

    public static bool IsDefined(PrismKnockout value) => value is
        PrismKnockout.None or PrismKnockout.Shallow or PrismKnockout.Deep;

    public static bool IsDefined(PrismBlendIfChannel value) => value is
        PrismBlendIfChannel.Gray or PrismBlendIfChannel.Red or
        PrismBlendIfChannel.Green or PrismBlendIfChannel.Blue;

    public static bool IsDefined(PrismMaskChannel value) => value is
        PrismMaskChannel.Alpha or PrismMaskChannel.Luminance;
}
