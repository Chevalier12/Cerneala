using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing;

public readonly struct DrawingFrameContext
{
    public DrawingFrameContext(
        PrismFrameAnalysis prismAnalysis,
        IBackdropFrameLease? backdropLease = null)
    {
        PrismAnalysis = prismAnalysis ??
            throw new ArgumentNullException(nameof(prismAnalysis));
        BackdropLease = backdropLease;
    }

    public PrismFrameAnalysis PrismAnalysis { get; }

    public IBackdropFrameLease? BackdropLease { get; }

    public void EnsureCurrent(DrawCommandList commands)
    {
        if (PrismAnalysis is null)
        {
            throw new InvalidOperationException(
                "The drawing frame context has not been initialized.");
        }

        PrismAnalysis.EnsureCurrent(commands);
    }
}
