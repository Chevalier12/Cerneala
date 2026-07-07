using Cerneala.UI.Media;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Layout;

public sealed class LayoutMotionOptions
{
    private LayoutMotionOptions(MotionSpec<Transform> correctionSpec)
    {
        CorrectionSpec = correctionSpec ?? throw new ArgumentNullException(nameof(correctionSpec));
    }

    public MotionSpec<Transform> CorrectionSpec { get; }

    public static LayoutMotionOptions Spring(MotionSpec<Transform> correctionSpec)
    {
        return new LayoutMotionOptions(correctionSpec);
    }
}
