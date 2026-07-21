using Cerneala.Drawing.Prism;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismRendererOptionsTests
{
    [Fact]
    public void DefaultsMatchMeasuredReferenceBudgets()
    {
        PrismRendererOptions options = new();

        Assert.Equal(
            512L * 1024 * 1024,
            options.SurfaceHardByteLimit);
        Assert.Equal(
            256L * 1024 * 1024,
            options.RetainedCacheSoftByteLimit);
        Assert.Equal(256, options.RetainedCacheEntryLimit);
        Assert.False(options.EnableDevelopmentDiagnostics);
        options.Validate();
    }

    [Fact]
    public void InvalidBudgetsAreRejectedBeforeGpuResourcesAreCreated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismRendererOptions
            {
                SurfaceHardByteLimit = -1
            }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismRendererOptions
            {
                SurfaceHardByteLimit = 10,
                RetainedCacheSoftByteLimit = 11
            }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismRendererOptions
            {
                RetainedCacheEntryLimit = -1
            }.Validate());
    }

    [Fact]
    public void DiagnosticReasonCountersRejectUnknownValues()
    {
        PrismRendererDiagnostics diagnostics = default;

        Assert.Equal(
            0,
            diagnostics.GetMissCount(
                PrismCacheMissReason.None));
        Assert.Equal(
            0,
            diagnostics.GetEvictionCount(
                PrismCacheEvictionReason.None));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => diagnostics.GetMissCount(
                (PrismCacheMissReason)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => diagnostics.GetEvictionCount(
                (PrismCacheEvictionReason)int.MaxValue));
    }
}
