using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Drawing.Prism;

public sealed class PrismRendererOptions
{
    public long SurfaceHardByteLimit { get; init; } =
        512L * 1024 * 1024;

    public long RetainedCacheSoftByteLimit { get; init; } =
        256L * 1024 * 1024;

    public int RetainedCacheEntryLimit { get; init; } = 256;

    public PrismColorProfile HostColorProfile { get; init; } =
        PrismColorProfile.Srgb;

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
        if (!Enum.IsDefined(HostColorProfile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(HostColorProfile));
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
