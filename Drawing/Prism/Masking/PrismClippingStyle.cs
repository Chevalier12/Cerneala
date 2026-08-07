using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Masking;

internal static class PrismClippingStyle
{
    internal static PrismPremultipliedColor Apply(
        PrismPremultipliedColor content,
        PrismPremultipliedColor clippingBase) =>
        PrismMaskMath.Scale(
            content,
            Math.Clamp(clippingBase.Alpha, 0, 1));
}
