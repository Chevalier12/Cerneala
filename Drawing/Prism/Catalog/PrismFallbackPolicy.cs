namespace Cerneala.Drawing.Prism.Catalog;

internal enum PrismFallbackReason
{
    UnsupportedCapability,
    MissingKernel,
    MissingBackdrop,
    InvalidColorProfile,
    SurfaceAllocationFailed,
    ShaderUnavailable
}

internal enum PrismFallbackAction
{
    BypassOperation,
    OmitBackdrop,
    BypassComposition
}

internal static class PrismFallbackPolicy
{
    public static PrismFallbackAction Resolve(PrismFallbackReason reason)
    {
        return reason switch
        {
            PrismFallbackReason.MissingBackdrop => PrismFallbackAction.OmitBackdrop,
            PrismFallbackReason.InvalidColorProfile => PrismFallbackAction.BypassComposition,
            PrismFallbackReason.SurfaceAllocationFailed => PrismFallbackAction.BypassComposition,
            _ => PrismFallbackAction.BypassOperation
        };
    }
}
