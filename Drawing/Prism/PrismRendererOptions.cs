namespace Cerneala.Drawing.Prism;

public sealed class PrismRendererOptions
{
    public long SurfaceHardByteLimit { get; init; } =
        512L * 1024 * 1024;

    public long RetainedCacheSoftByteLimit { get; init; } =
        256L * 1024 * 1024;

    public int RetainedCacheEntryLimit { get; init; } = 256;

    public bool EnableDevelopmentDiagnostics { get; init; }

    internal void Validate()
    {
        if (SurfaceHardByteLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SurfaceHardByteLimit));
        }
        if (RetainedCacheSoftByteLimit < 0 ||
            RetainedCacheSoftByteLimit > SurfaceHardByteLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetainedCacheSoftByteLimit));
        }
        if (RetainedCacheEntryLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetainedCacheEntryLimit));
        }
    }
}

public enum PrismCacheMissReason
{
    None,
    NotFound,
    NotCacheable,
    DependencyChanged,
    Invalidated,
    Disabled
}

public enum PrismCacheEvictionReason
{
    None,
    Capacity,
    Invalidation,
    TransientPressure,
    Replacement,
    InvalidSurface,
    DeviceReset,
    Disposal,
    ExplicitRemoval
}

[Flags]
public enum PrismDependencyChange
{
    None = 0,
    Owner = 1 << 0,
    Structure = 1 << 1,
    Values = 1 << 2,
    Resources = 1 << 3,
    RasterBounds = 1 << 4,
    SurfaceSize = 1 << 5,
    LowerUi = 1 << 6,
    PixelScale = 1 << 7,
    Transform = 1 << 8,
    WorkingColorProfile = 1 << 9,
    OutputColorProfile = 1 << 10,
    SurfaceFormat = 1 << 11,
    Sampling = 1 << 12,
    Capabilities = 1 << 13,
    ShaderPackage = 1 << 14
}

public readonly struct PrismRendererDiagnostics
{
    private readonly long notFoundMissCount;
    private readonly long notCacheableMissCount;
    private readonly long dependencyChangedMissCount;
    private readonly long invalidatedMissCount;
    private readonly long disabledMissCount;
    private readonly long capacityEvictionCount;
    private readonly long invalidationEvictionCount;
    private readonly long transientPressureEvictionCount;
    private readonly long replacementEvictionCount;
    private readonly long invalidSurfaceEvictionCount;
    private readonly long deviceResetEvictionCount;
    private readonly long disposalEvictionCount;
    private readonly long explicitRemovalEvictionCount;

    internal PrismRendererDiagnostics(
        bool retainedCacheEnabled,
        long finalHitCount,
        long intermediateHitCount,
        long missCount,
        PrismCacheMissReason lastMissReason,
        long lookupCount,
        long promotionCount,
        long rejectedPromotionCount,
        long evictionCount,
        PrismCacheEvictionReason lastEvictionReason,
        int retainedEntryCount,
        int pinnedEntryCount,
        long transientByteCount,
        long retainedByteCount,
        long totalByteCount,
        long peakTotalByteCount,
        long savedCaptureCount,
        long savedPassCount,
        PrismDependencyChange lastDependencyChange,
        long notFoundMissCount,
        long notCacheableMissCount,
        long dependencyChangedMissCount,
        long invalidatedMissCount,
        long disabledMissCount,
        long capacityEvictionCount,
        long invalidationEvictionCount,
        long transientPressureEvictionCount,
        long replacementEvictionCount,
        long invalidSurfaceEvictionCount,
        long deviceResetEvictionCount,
        long disposalEvictionCount,
        long explicitRemovalEvictionCount)
    {
        RetainedCacheEnabled = retainedCacheEnabled;
        FinalHitCount = finalHitCount;
        IntermediateHitCount = intermediateHitCount;
        MissCount = missCount;
        LastMissReason = lastMissReason;
        LookupCount = lookupCount;
        PromotionCount = promotionCount;
        RejectedPromotionCount = rejectedPromotionCount;
        EvictionCount = evictionCount;
        LastEvictionReason = lastEvictionReason;
        RetainedEntryCount = retainedEntryCount;
        PinnedEntryCount = pinnedEntryCount;
        TransientByteCount = transientByteCount;
        RetainedByteCount = retainedByteCount;
        TotalByteCount = totalByteCount;
        PeakTotalByteCount = peakTotalByteCount;
        SavedCaptureCount = savedCaptureCount;
        SavedPassCount = savedPassCount;
        LastDependencyChange = lastDependencyChange;
        this.notFoundMissCount = notFoundMissCount;
        this.notCacheableMissCount = notCacheableMissCount;
        this.dependencyChangedMissCount =
            dependencyChangedMissCount;
        this.invalidatedMissCount = invalidatedMissCount;
        this.disabledMissCount = disabledMissCount;
        this.capacityEvictionCount = capacityEvictionCount;
        this.invalidationEvictionCount =
            invalidationEvictionCount;
        this.transientPressureEvictionCount =
            transientPressureEvictionCount;
        this.replacementEvictionCount =
            replacementEvictionCount;
        this.invalidSurfaceEvictionCount =
            invalidSurfaceEvictionCount;
        this.deviceResetEvictionCount =
            deviceResetEvictionCount;
        this.disposalEvictionCount = disposalEvictionCount;
        this.explicitRemovalEvictionCount =
            explicitRemovalEvictionCount;
    }

    internal static PrismRendererDiagnostics Empty(
        bool retainedCacheEnabled) =>
        new(
            retainedCacheEnabled,
            0,
            0,
            0,
            PrismCacheMissReason.None,
            0,
            0,
            0,
            0,
            PrismCacheEvictionReason.None,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            PrismDependencyChange.None,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);

    public bool RetainedCacheEnabled { get; }

    public long FinalHitCount { get; }

    public long IntermediateHitCount { get; }

    public long MissCount { get; }

    public PrismCacheMissReason LastMissReason { get; }

    public long LookupCount { get; }

    public long PromotionCount { get; }

    public long RejectedPromotionCount { get; }

    public long EvictionCount { get; }

    public PrismCacheEvictionReason LastEvictionReason { get; }

    public int RetainedEntryCount { get; }

    public int PinnedEntryCount { get; }

    public long TransientByteCount { get; }

    public long RetainedByteCount { get; }

    public long TotalByteCount { get; }

    public long PeakTotalByteCount { get; }

    public long SavedCaptureCount { get; }

    public long SavedPassCount { get; }

    public PrismDependencyChange LastDependencyChange { get; }

    public long GetMissCount(PrismCacheMissReason reason) =>
        reason switch
        {
            PrismCacheMissReason.None => 0,
            PrismCacheMissReason.NotFound =>
                notFoundMissCount,
            PrismCacheMissReason.NotCacheable =>
                notCacheableMissCount,
            PrismCacheMissReason.DependencyChanged =>
                dependencyChangedMissCount,
            PrismCacheMissReason.Invalidated =>
                invalidatedMissCount,
            PrismCacheMissReason.Disabled =>
                disabledMissCount,
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unknown Prism cache miss reason.")
        };

    public long GetEvictionCount(
        PrismCacheEvictionReason reason) =>
        reason switch
        {
            PrismCacheEvictionReason.None => 0,
            PrismCacheEvictionReason.Capacity =>
                capacityEvictionCount,
            PrismCacheEvictionReason.Invalidation =>
                invalidationEvictionCount,
            PrismCacheEvictionReason.TransientPressure =>
                transientPressureEvictionCount,
            PrismCacheEvictionReason.Replacement =>
                replacementEvictionCount,
            PrismCacheEvictionReason.InvalidSurface =>
                invalidSurfaceEvictionCount,
            PrismCacheEvictionReason.DeviceReset =>
                deviceResetEvictionCount,
            PrismCacheEvictionReason.Disposal =>
                disposalEvictionCount,
            PrismCacheEvictionReason.ExplicitRemoval =>
                explicitRemovalEvictionCount,
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unknown Prism cache eviction reason.")
        };
}
