using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing;

public readonly struct DrawingFrameContext
{
    public DrawingFrameContext(
        PrismFrameAnalysis prismAnalysis,
        IBackdropFrameLease? backdropLease = null)
        : this(
            prismAnalysis,
            backdropLease,
            default,
            prismCacheInvalidations: null)
    {
    }

    internal DrawingFrameContext(
        PrismFrameAnalysis prismAnalysis,
        IBackdropFrameLease? backdropLease,
        PrismBackdropSourceToken backdropSourceToken,
        PrismCacheInvalidationQueue? prismCacheInvalidations = null)
    {
        PrismAnalysis = prismAnalysis ??
            throw new ArgumentNullException(nameof(prismAnalysis));
        BackdropLease = backdropLease;
        BackdropSourceToken = backdropSourceToken;
        PrismCacheInvalidations = prismCacheInvalidations;
    }

    public PrismFrameAnalysis PrismAnalysis { get; }

    public IBackdropFrameLease? BackdropLease { get; }

    internal PrismBackdropSourceToken BackdropSourceToken { get; }

    internal PrismCacheInvalidationQueue? PrismCacheInvalidations { get; }

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
