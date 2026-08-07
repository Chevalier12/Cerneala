using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismAdvancedBlendingStyle
{
    internal static PrismPremultipliedColor ApplyChannelMask(
        PrismPremultipliedColor composite,
        PrismPremultipliedColor backdrop,
        PrismBlendChannels channels)
    {
        PrismBlendColor compositeStraight = PrismBlendMath.Unassociate(composite);
        PrismBlendColor backdropStraight = PrismBlendMath.Unassociate(backdrop);
        double alpha = (channels & PrismBlendChannels.Alpha) != 0
            ? composite.Alpha
            : backdrop.Alpha;
        return PrismPremultipliedColor.FromStraight(
            (channels & PrismBlendChannels.Red) != 0
                ? compositeStraight.Red
                : backdropStraight.Red,
            (channels & PrismBlendChannels.Green) != 0
                ? compositeStraight.Green
                : backdropStraight.Green,
            (channels & PrismBlendChannels.Blue) != 0
                ? compositeStraight.Blue
                : backdropStraight.Blue,
            alpha);
    }
}
