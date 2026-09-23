using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Surfaces;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.SdlGpu.Prism;

public sealed class PrismSurfaceOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmLeaseAcquisitionAndReleaseAfterFullCollectionDoNotAllocate(bool retained)
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        if (retained) fixture.Promote(key);
        for (int i = 0; i < 8; i++) AcquireAndRelease();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 64; i++) AcquireAndRelease();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(1, fixture.Resources.CreatedSurfaceCount);

        void AcquireAndRelease()
        {
            if (retained)
            {
                if (!fixture.Resources.TryAcquireRetained(key, fixture.Session.WindowIdentity, out var lease))
                    throw new InvalidOperationException("The retained entry disappeared.");
                lease.Dispose();
            }
            else fixture.Rent().Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmLeaseAcquisitionAndReleaseDoNotAllocate(bool retained)
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        if (retained) fixture.Promote(key);
        for (int i = 0; i < 8; i++) AcquireAndRelease();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 64; i++) AcquireAndRelease();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(1, fixture.Resources.CreatedSurfaceCount);
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);

        void AcquireAndRelease()
        {
            if (retained)
            {
                if (!fixture.Resources.TryAcquireRetained(key, fixture.Session.WindowIdentity, out var lease))
                    throw new InvalidOperationException("The retained entry disappeared.");
                lease.Dispose();
            }
            else fixture.Rent().Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposedLeaseCopiesCannotReleaseALaterAcquisition(bool retained)
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        if (retained) fixture.Promote(key);
        SdlGpuPrismSurfaceLease original = retained ? fixture.Acquire(key) : fixture.Rent();
        SdlGpuPrismSurfaceLease copy = original;
        original.Dispose();
        using SdlGpuPrismSurfaceLease current = retained ? fixture.Acquire(key) : fixture.Rent();
        Assert.Same(original.Target, current.Target);
        copy.Dispose();
        original.Dispose();
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        using (SdlGpuPrismSurfaceLease other = fixture.Rent())
            Assert.NotSame(current.Target, other.Target);
        Assert.Equal(retained ? 1 : 0, fixture.Resources.RetainedCount);
        current.Dispose();
        current.Dispose();
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void PromotionIsVisibleToEveryLiveCopyAndReleasesOnlyOnePin()
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        using SdlGpuPrismSurfaceLease original = fixture.Rent();
        SdlGpuPrismSurfaceLease copy = original;
        fixture.Resources.Promote(key, original);
        Assert.True(copy.IsRetained);
        Assert.Equal(key, copy.RetainedKey);
        copy.Dispose();
        original.Dispose();
        using (SdlGpuPrismSurfaceLease retained = fixture.Acquire(key))
            Assert.Same(original.Target, retained.Target);
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void PromotionRejectsForeignAndReleasedLeasesWithoutChangingEitherPool()
    {
        using Fixture first = new();
        using Fixture second = new();
        using SdlGpuPrismSurfaceLease live = first.Rent();
        Assert.Throws<ArgumentException>(() => second.Resources.Promote(Key(1), live));
        Assert.Equal(0, second.Resources.TotalBytes);
        Assert.Equal(0, second.Resources.RetainedCount);
        live.Dispose();
        Assert.Throws<ObjectDisposedException>(() => first.Resources.Promote(Key(1), live));
        Assert.Equal(0, first.Resources.RetainedCount);
        Assert.Equal(first.Resources.TotalBytes, first.Resources.FreeBytes);
    }

    [Theory]
    [InlineData(0, 8, false)]
    [InlineData(-1, 8, false)]
    [InlineData(8, 0, false)]
    [InlineData(8, -1, false)]
    [InlineData(0, 8, true)]
    [InlineData(-1, 8, true)]
    [InlineData(8, 0, true)]
    [InlineData(8, -1, true)]
    public void InvalidDimensionsAreRejectedBeforeChangingOwnership(int width, int height, bool mipmapped)
    {
        using Fixture fixture = new();
        int created = fixture.Api.TextureCreationCount;
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Rent(width, height, mipmapped: mipmapped));
        Assert.Equal(created, fixture.Api.TextureCreationCount);
        Assert.Equal(0, fixture.Resources.TotalBytes);
        Assert.Equal(0, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void InvalidFormatEnumIsRejectedBeforeChangingOwnership()
    {
        using Fixture fixture = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Rent(format: (SdlGpuTextureFormat)int.MaxValue));
        Assert.Equal(0, fixture.Resources.TotalBytes);
        Assert.Equal(0, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void SurfaceFormatAdmissionPreservesDefinedUnsupportedValuesAndNumericHoles()
    {
        using Fixture fixture = new();
        IEnumerable<int> ids = Enumerable.Range(-1, 65)
            .Concat(Enum.GetValues<SdlGpuTextureFormat>().Select(value => (int)value))
            .Append(int.MinValue).Append(int.MaxValue).Distinct();
        foreach (int id in ids)
        {
            SdlGpuTextureFormat format = (SdlGpuTextureFormat)id;
            if (Enum.IsDefined(format) && format is not
                (SdlGpuTextureFormat.Invalid or SdlGpuTextureFormat.D24UnormS8Uint))
            {
                using SdlGpuPrismSurfaceLease lease = fixture.Rent(format: format);
                Assert.Equal(format, lease.Target.ColorFormat);
                continue;
            }

            int created = fixture.Api.TextureCreationCount;
            long bytes = fixture.Resources.TotalBytes;
            if (Enum.IsDefined(format))
                Assert.Throws<NotSupportedException>(() => fixture.Rent(format: format));
            else
                Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Rent(format: format));
            Assert.Equal(created, fixture.Api.TextureCreationCount);
            Assert.Equal(bytes, fixture.Resources.TotalBytes);
            Assert.Equal(bytes, fixture.Resources.FreeBytes);
        }
        Assert.Equal(7, fixture.Resources.CreatedSurfaceCount);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void InvalidFormatCannotConsumeAnAlreadyPooledSurface()
    {
        using Fixture fixture = new();
        SdlGpuRenderTarget target;
        using (SdlGpuPrismSurfaceLease lease = fixture.Rent()) target = lease.Target;
        int created = fixture.Api.TextureCreationCount;
        long bytes = fixture.Resources.TotalBytes;
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Rent(format: (SdlGpuTextureFormat)int.MaxValue));
        Assert.Equal(created, fixture.Api.TextureCreationCount);
        Assert.Equal(bytes, fixture.Resources.TotalBytes);
        Assert.Equal(bytes, fixture.Resources.FreeBytes);
        using SdlGpuPrismSurfaceLease reused = fixture.Rent();
        Assert.Same(target, reused.Target);
        Assert.Equal(1, fixture.Resources.CreatedSurfaceCount);
    }

    [Fact]
    public void ExecutorHonorsOptimizerPeakAndReleasesEveryGraphLease()
    {
        using Fixture fixture = new();
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Surface lifetime",
            PrismTestData.Layer(1, "Filtered layer")), bounds: new(0, 0, 8, 8));
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(0, 0, 5, 5), Color.White), DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(analysis));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(fixture.Session.DrawingBackend);
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        DrawingFrameContext context = new(analysis);
        fixture.Session.BeginFrame(Color.Transparent);
        try { backend.Render(commands, context); }
        finally { fixture.Session.CompleteFrame(false); }
        Assert.Equal(0, backend.PrismDiagnostics.Count);
        // The unpruned plan is a ceiling, not mandatory transient work. SDL
        // promotes completed cache candidates while later graph nodes run.
        Assert.InRange(backend.PrismDiagnostics.Counters.PeakLiveSurfaceCount, 1, plan.PeakLiveSurfaces);
        Assert.True(resources.RetainedCount > 0);
        Assert.Equal(0, backend.PrismDiagnostics.Counters.ActiveSurfaceCount);
        resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(0, resources.RetainedCount);
        Assert.Equal(resources.TotalBytes, resources.FreeBytes);
    }

    [Fact]
    public void MipChainCreationAndByteAccountingIncludeEveryLevel()
    {
        using Fixture fixture = new();
        using SdlGpuPrismSurfaceLease lease = fixture.Rent(8, 4, mipmapped: true);
        Assert.Equal(4u, lease.Target.MipLevelCount);
        Assert.Equal(172, fixture.Resources.TotalBytes);
        Assert.Equal(4u, fixture.Api.GpuTextures[lease.Target.ColorTexture].CreateInfo.MipLevelCount);
    }

    [Fact]
    public void CompatibleSurfacesAreReusedAndExceptionsReleaseLeases()
    {
        using Fixture fixture = new();
        SdlGpuRenderTarget target;
        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using SdlGpuPrismSurfaceLease lease = fixture.Rent();
            throw new InvalidOperationException("Injected surface consumer failure.");
        }));
        using (SdlGpuPrismSurfaceLease lease = fixture.Rent())
        {
            target = lease.Target;
        }
        using (SdlGpuPrismSurfaceLease lease = fixture.Rent())
        {
            Assert.Same(target, lease.Target);
        }
        Assert.Equal(1, fixture.Resources.CreatedSurfaceCount);
        Assert.Equal(2, fixture.Resources.ReusedSurfaceCount);
        Assert.Equal(1, fixture.Resources.FreeSurfaceCount);
        Assert.Equal(256, fixture.Resources.FreeBytes);
    }

    [Theory]
    [InlineData(9, 8, (int)SdlGpuTextureFormat.R8G8B8A8Unorm, false)]
    [InlineData(8, 9, (int)SdlGpuTextureFormat.R8G8B8A8Unorm, false)]
    [InlineData(8, 8, (int)SdlGpuTextureFormat.B8G8R8A8Unorm, false)]
    [InlineData(8, 8, (int)SdlGpuTextureFormat.R8G8B8A8UnormSrgb, false)]
    [InlineData(8, 8, (int)SdlGpuTextureFormat.R8G8B8A8Unorm, true)]
    public void IncompatibleSurfaceStorageIsNotReused(
        int width, int height, int format, bool mipmapped)
    {
        using Fixture fixture = new();
        SdlGpuRenderTarget first;
        using (SdlGpuPrismSurfaceLease lease = fixture.Rent())
        {
            first = lease.Target;
        }
        using SdlGpuPrismSurfaceLease other = fixture.Rent(width, height, (SdlGpuTextureFormat)format, mipmapped);
        Assert.NotSame(first, other.Target);
        Assert.Equal(2, fixture.Resources.CreatedSurfaceCount);
    }

    [Fact]
    public void ThousandsOfFramesKeepOwnershipBounded()
    {
        using Fixture fixture = new();
        for (int frame = 0; frame < 2048; frame++)
        {
            int size = frame % 256 == 0 ? 9 : 8;
            using (fixture.Rent(size, size)) { }
            Assert.InRange(fixture.Resources.FreeSurfaceCount, 1, 2);
            Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
        }
        Assert.InRange(fixture.Resources.CreatedSurfaceCount, 1, 17);
    }

    [Fact]
    public void PromotionTransfersOwnershipWithoutDuplicatingStorage()
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        SdlGpuRenderTarget target;
        using (SdlGpuPrismSurfaceLease lease = fixture.Rent())
        {
            target = lease.Target;
            long before = fixture.Resources.TotalBytes;
            fixture.Resources.Promote(key, lease);
            Assert.True(lease.IsRetained);
            Assert.Equal(before, fixture.Resources.TotalBytes);
            Assert.Equal(1, fixture.Resources.RetainedCount);
        }
        Assert.Equal(0, fixture.Resources.FreeSurfaceCount);
        using (SdlGpuPrismSurfaceLease transient = fixture.Rent())
        {
            Assert.NotSame(target, transient.Target);
        }
        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using SdlGpuPrismSurfaceLease pinned = fixture.Acquire(key);
            Assert.Same(target, pinned.Target);
            throw new InvalidOperationException("Injected cached draw failure.");
        }));
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnerInvalidationHidesStaleEntriesWithoutRecyclingPinnedStorage(bool staleKeys)
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey first = Key(1);
        PrismRetainedCacheKey second = Key(2);
        fixture.Promote(first);
        fixture.Promote(second);
        using SdlGpuPrismSurfaceLease pinned = fixture.Acquire(first);
        if (staleKeys)
        {
            fixture.Resources.InvalidateStaleOwnerEntries(first.StableNodeId.ScopeOwnerToken,
                new HashSet<PrismRetainedCacheKey>());
        }
        else
        {
            fixture.Resources.Invalidate(PrismCacheInvalidation.ForOwner(first.StableNodeId.ScopeOwnerToken));
        }
        Assert.False(fixture.Resources.TryAcquireRetained(first, 1, out _));
        using (fixture.Acquire(second)) { }
        using (SdlGpuPrismSurfaceLease transient = fixture.Rent())
        {
            Assert.NotSame(pinned.Target, transient.Target);
        }
        pinned.Dispose();
        Assert.Equal(1, fixture.Resources.RetainedCount);
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Theory]
    [InlineData(512, 10)]
    [InlineData(4096, 2)]
    public void EntryAndByteBudgetsEvictLeastRecentlyUsed(long bytes, int entries)
    {
        using Fixture fixture = new(softBytes: bytes, entries: entries);
        PrismRetainedCacheKey first = Key(1);
        PrismRetainedCacheKey second = Key(2);
        PrismRetainedCacheKey third = Key(3);
        fixture.Promote(first);
        fixture.Promote(second);
        using (fixture.Acquire(first)) { }
        fixture.Promote(third);
        using (fixture.Acquire(first)) { }
        Assert.False(fixture.Resources.TryAcquireRetained(second, 1, out _));
        using (fixture.Acquire(third)) { }
        Assert.Equal(2, fixture.Resources.RetainedCount);
    }

    [Fact]
    public void PinnedEntryCannotBeEvictedAndRejectedPromotionKeepsTransientOwnership()
    {
        using Fixture fixture = new(softBytes: 256, entries: 1);
        PrismRetainedCacheKey first = Key(1);
        PrismRetainedCacheKey second = Key(2);
        fixture.Promote(first);
        using SdlGpuPrismSurfaceLease pinned = fixture.Acquire(first);
        using (SdlGpuPrismSurfaceLease next = fixture.Rent())
        {
            fixture.Resources.Promote(second, next);
            Assert.False(next.IsRetained);
            Assert.Equal(1, fixture.Resources.RetainedCount);
        }
        using (SdlGpuPrismSurfaceLease stillPinned = fixture.Acquire(first))
        {
            Assert.Same(pinned.Target, stillPinned.Target);
        }
        pinned.Dispose();
        fixture.Promote(second);
        Assert.False(fixture.Resources.TryAcquireRetained(first, 1, out _));
        using (fixture.Acquire(second)) { }
    }

    [Fact]
    public void ZeroRetentionBudgetPreservesTransientOwnership()
    {
        using Fixture fixture = new(softBytes: 0, entries: 0);
        using SdlGpuPrismSurfaceLease lease = fixture.Rent();
        fixture.Resources.Promote(Key(1), lease);
        Assert.False(lease.IsRetained);
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(256, fixture.Resources.TotalBytes);
    }

    [Fact]
    public void TransientPressureReclaimsRetainedStorageBeforeReportingHardCapFailure()
    {
        using Fixture fixture = new(hardBytes: 768, softBytes: 768);
        fixture.Promote(Key(1));
        fixture.Promote(Key(2));
        fixture.Promote(Key(3));
        using SdlGpuPrismSurfaceLease large = fixture.Rent(12, 12);
        Assert.Equal(576, fixture.Resources.TotalBytes);
        Assert.Equal(0, fixture.Resources.RetainedCount);
    }

    [Fact]
    public void HardCapEvictionCanReplaceTheLastSurfaceOfTheRequestedSize()
    {
        using Fixture fixture = new(hardBytes: 256, softBytes: 256);
        fixture.Promote(Key(1));
        using (SdlGpuPrismSurfaceLease replacement = fixture.Rent())
        {
            Assert.Equal(0, fixture.Resources.RetainedCount);
            Assert.Equal(256, fixture.Resources.TotalBytes);
        }
        Assert.Equal(1, fixture.Resources.FreeSurfaceCount);
        Assert.Equal(256, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void HardCapFailureLeavesNoPartialOwnershipAndReportsTheBudget()
    {
        using Fixture fixture = new(hardBytes: 255, softBytes: 0, entries: 0);
        PrismSurfaceAllocationException failure = Assert.Throws<PrismSurfaceAllocationException>(
            () => fixture.Rent());
        Assert.Contains("requestedBytes=256", failure.Message, StringComparison.Ordinal);
        Assert.Contains("currentBytes=0", failure.Message, StringComparison.Ordinal);
        Assert.Contains("hardByteLimit=255", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, fixture.Resources.TotalBytes);
        Assert.Equal(0, fixture.Resources.FreeSurfaceCount);
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(PrismFallbackAction.BypassComposition,
            PrismFallbackPolicy.Resolve(PrismFallbackReason.SurfaceAllocationFailed));
    }

    [Fact]
    public void DisposalRejectsNewWorkAndLateLeaseReleaseDoesNotResurrectThePool()
    {
        using Fixture fixture = new();
        SdlGpuPrismSurfaceLease transient = fixture.Rent();
        PrismRetainedCacheKey key = Key(1);
        fixture.Promote(key);
        SdlGpuPrismSurfaceLease pinned = fixture.Acquire(key);
        fixture.Resources.Dispose();
        Assert.Throws<ObjectDisposedException>(() => fixture.Rent());
        Assert.Throws<ObjectDisposedException>(() => fixture.Resources.TryAcquireRetained(Key(1), 1, out _));
        fixture.Session.DrawingResources.FlushRetired();
        Assert.Contains(pinned.Target.ColorTexture, fixture.Api.GpuTextures.Keys);
        Assert.Contains(transient.Target.ColorTexture, fixture.Api.GpuTextures.Keys);
        pinned.Dispose();
        transient.Dispose();
        Assert.Equal(0, fixture.Resources.TotalBytes);
        Assert.Equal(0, fixture.Resources.FreeBytes);
        Assert.Equal(0, fixture.Resources.FreeSurfaceCount);
        fixture.Session.DrawingResources.FlushRetired();
        Assert.DoesNotContain(pinned.Target.ColorTexture, fixture.Api.GpuTextures.Keys);
        Assert.DoesNotContain(transient.Target.ColorTexture, fixture.Api.GpuTextures.Keys);
    }

    [Fact]
    public void RetainedEntryMetadataContainsNoUiOrLifecycleObjects()
    {
        Type entry = typeof(SdlGpuPrismDeviceResources).GetNestedType("RetainedEntry", BindingFlags.NonPublic)!;
        Type[] forbidden = [typeof(UIElement), typeof(Cerneala.UI.Data.Binding), typeof(Delegate),
            typeof(MotionHandle), typeof(IBackdropFrameLease), typeof(PrismInstance)];
        foreach (FieldInfo field in entry.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Assert.DoesNotContain(forbidden, type => type.IsAssignableFrom(field.FieldType));
        }
    }

    [Theory]
    [InlineData("rent")]
    [InlineData("acquire")]
    [InlineData("promote")]
    [InlineData("invalidate")]
    [InlineData("invalidate-stale")]
    [InlineData("dispose")]
    [InlineData("release")]
    public void CrossThreadMutationIsRejectedWithoutChangingOwnership(string operation)
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        fixture.Promote(key);
        using SdlGpuPrismSurfaceLease pinned = fixture.Acquire(key);
        using SdlGpuPrismSurfaceLease transient = fixture.Rent();
        long bytes = fixture.Resources.TotalBytes;
        Exception? failure = null;
        Thread thread = new(() =>
        {
            failure = Record.Exception(() =>
            {
                switch (operation)
                {
                    case "rent": using (fixture.Rent()) { } break;
                    case "acquire": using (fixture.Acquire(key)) { } break;
                    case "promote": fixture.Resources.Promote(Key(2), transient); break;
                    case "invalidate": fixture.Resources.Invalidate(PrismCacheInvalidation.All); break;
                    case "invalidate-stale": fixture.Resources.InvalidateStaleOwnerEntries(key.StableNodeId.ScopeOwnerToken, new HashSet<PrismRetainedCacheKey>()); break;
                    case "dispose": fixture.Resources.Dispose(); break;
                    case "release": pinned.Dispose(); break;
                }
            });
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "Cross-thread operation did not finish.");
        Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal(bytes, fixture.Resources.TotalBytes);
        Assert.Equal(1, fixture.Resources.RetainedCount);
        Assert.Equal(0, fixture.Resources.FreeSurfaceCount);
        using (SdlGpuPrismSurfaceLease existing = fixture.Acquire(key)) { Assert.Same(pinned.Target, existing.Target); }
        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        pinned.Dispose();
        transient.Dispose();
        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Equal(fixture.Resources.TotalBytes, fixture.Resources.FreeBytes);
    }

    [Fact]
    public void ReplacementReusesOneCacheKeyAndKeepsTheReplacementOutOfTransientStorage()
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(1);
        fixture.Promote(key);
        SdlGpuRenderTarget previous;
        using (SdlGpuPrismSurfaceLease first = fixture.Acquire(key)) { previous = first.Target; }
        SdlGpuRenderTarget replacement;
        using (SdlGpuPrismSurfaceLease next = fixture.Rent())
        {
            replacement = next.Target;
            Assert.NotSame(previous, replacement);
            fixture.Resources.Promote(key, next);
            Assert.True(next.IsRetained);
        }
        Assert.Equal(1, fixture.Resources.RetainedCount);
        using (SdlGpuPrismSurfaceLease current = fixture.Acquire(key)) { Assert.Same(replacement, current.Target); }
        using (SdlGpuPrismSurfaceLease reusable = fixture.Rent()) { Assert.Same(previous, reusable.Target); }
    }

    [Fact]
    public void InvalidatedPendingPromotionIsNotReusedOrResurrectedAtSubmit()
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(71);
        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuRenderTarget pendingTarget;
        using (SdlGpuPrismSurfaceLease pending = fixture.Rent())
        {
            pendingTarget = pending.Target;
            fixture.Resources.Promote(fixture.Session, key, pending);
        }

        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        Assert.False(fixture.Resources.TryAcquireRetained(
            fixture.Session,
            key,
            fixture.Session.WindowIdentity,
            out _));
        using (SdlGpuPrismSurfaceLease other = fixture.Rent())
        {
            Assert.NotSame(pendingTarget, other.Target);
        }

        fixture.Session.CompleteFrame(present: false);

        Assert.False(fixture.Resources.TryAcquireRetained(
            key,
            fixture.Session.WindowIdentity,
            out _));
        Assert.Equal(0, fixture.Resources.RetainedCount);
    }

    [Fact]
    public void InvalidatedPendingPromotionStaysPinnedAfterItsHostLeaseIsReleased()
    {
        using Fixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "pending invalidation contender");
        PrismRetainedCacheKey key = Key(711);
        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuPrismSurfaceLease pending = fixture.Rent();
        SdlGpuRenderTarget pendingTarget = pending.Target;
        fixture.Resources.Promote(fixture.Session, key, pending);

        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        pending.Dispose();

        using (SdlGpuPrismSurfaceLease other = fixture.Resources.RentSurface(
            second.WindowIdentity,
            8,
            8,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            mipmapped: false))
        {
            Assert.NotSame(pendingTarget, other.Target);
        }

        fixture.Session.CompleteFrame(present: false);
        Assert.Equal(0, fixture.Resources.RetainedCount);
    }

    [Fact]
    public void InvalidatedSubmittedHitStaysPinnedUntilItsUsingBufferEnds()
    {
        using Fixture fixture = new();
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "submitted invalidation contender");
        PrismRetainedCacheKey key = Key(712);
        fixture.Promote(key);
        fixture.Session.BeginFrame(Color.Transparent);
        Assert.True(fixture.Resources.TryAcquireRetained(
            fixture.Session,
            key,
            fixture.Session.WindowIdentity,
            out SdlGpuPrismSurfaceLease hit));
        SdlGpuRenderTarget submittedTarget = hit.Target;

        fixture.Resources.Invalidate(PrismCacheInvalidation.All);
        hit.Dispose();

        using (SdlGpuPrismSurfaceLease other = fixture.Resources.RentSurface(
            second.WindowIdentity,
            8,
            8,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            mipmapped: false))
        {
            Assert.NotSame(submittedTarget, other.Target);
        }

        fixture.Session.CompleteFrame(present: false);
        Assert.Equal(0, fixture.Resources.RetainedCount);
    }

    [Fact]
    public void SubmittedHitCannotBeReusedDuringBudgetEvictionBeforeItsBufferEnds()
    {
        using Fixture fixture = new(hardBytes: 768, softBytes: 512, entries: 1);
        using SdlGpuWindowGraphicsSession second = fixture.CreateAdditionalSession(
            "submitted budget contender");
        PrismRetainedCacheKey firstKey = Key(713);
        PrismRetainedCacheKey secondKey = Key(714);
        fixture.Promote(firstKey);
        fixture.Session.BeginFrame(Color.Transparent);
        Assert.True(fixture.Resources.TryAcquireRetained(
            fixture.Session,
            firstKey,
            fixture.Session.WindowIdentity,
            out SdlGpuPrismSurfaceLease hit));
        SdlGpuRenderTarget submittedTarget = hit.Target;
        hit.Dispose();

        using (SdlGpuPrismSurfaceLease pressure = fixture.Rent())
        {
            fixture.Resources.Promote(secondKey, pressure);
            Assert.False(pressure.IsRetained);
        }
        using (SdlGpuPrismSurfaceLease other = fixture.Resources.RentSurface(
            second.WindowIdentity,
            8,
            8,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            mipmapped: false))
        {
            Assert.NotSame(submittedTarget, other.Target);
        }

        fixture.Session.CompleteFrame(present: false);
    }

    [Fact]
    public void PendingPromotionCannotBeEvictedOrReusedToSatisfyItsOwnBudget()
    {
        using Fixture fixture = new(hardBytes: 512, softBytes: 512, entries: 1);
        PrismRetainedCacheKey firstKey = Key(72);
        PrismRetainedCacheKey secondKey = Key(73);
        fixture.Session.BeginFrame(Color.Transparent);
        SdlGpuRenderTarget firstTarget;
        using (SdlGpuPrismSurfaceLease first = fixture.Rent())
        {
            firstTarget = first.Target;
            fixture.Resources.Promote(fixture.Session, firstKey, first);
        }

        using (SdlGpuPrismSurfaceLease second = fixture.Rent())
        {
            Assert.NotSame(firstTarget, second.Target);
            fixture.Resources.Promote(fixture.Session, secondKey, second);
            Assert.False(second.IsRetained);
        }
        Assert.Equal(1, fixture.Resources.RetainedCount);
        Assert.Equal(1, fixture.Resources.FreeSurfaceCount);

        fixture.Session.CompleteFrame(present: false);

        using SdlGpuPrismSurfaceLease submitted = fixture.Acquire(firstKey);
        Assert.Same(firstTarget, submitted.Target);
        Assert.False(fixture.Resources.TryAcquireRetained(
            secondKey,
            fixture.Session.WindowIdentity,
            out _));
    }

    [Fact]
    public void DisposedPendingResourceOwnerCannotPublishDuringLaterSubmitCleanup()
    {
        using Fixture fixture = new();
        PrismRetainedCacheKey key = Key(74);
        fixture.Session.BeginFrame(Color.Transparent);
        using (SdlGpuPrismSurfaceLease pending = fixture.Rent())
        {
            fixture.Resources.Promote(fixture.Session, key, pending);
        }

        fixture.Resources.Dispose();
        fixture.Session.CompleteFrame(present: false);

        Assert.Equal(0, fixture.Resources.RetainedCount);
        Assert.Throws<ObjectDisposedException>(() =>
            fixture.Resources.TryAcquireRetained(key, fixture.Session.WindowIdentity, out _));
        Assert.Equal(0, fixture.Api.LiveCommandBufferCount);
    }

    private static PrismRetainedCacheKey Key(long owner)
    {
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Surface ownership",
            PrismTestData.Layer(1, "Content")), owner);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(PrismTestData.Commands(DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(new DrawRect(0, 0, 5, 5), Color.White), DrawCommand.EndPrism()))));
        PrismGraphNodeId output = Assert.IsType<PrismGraphNodeId>(Assert.Single(plan.OptimizedGraph.Scopes).Output);
        PrismRetainedRasterContext context = new(8, 8, PrismColorProfile.Srgb, BackdropPixelFormat.Rgba8Unorm,
            PrismSampling.Linear, PrismGraphCapabilities.ControlCapture | PrismGraphCapabilities.ColorConversion, 1);
        Assert.True(PrismRetainedCacheKey.TryCreate(plan, output, context, out PrismRetainedCacheKey key));
        return key;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        private readonly nint window;
        private readonly List<nint> additionalWindows = [];
        public FakeSdlApi Api { get; } = new() { WindowPixelDensity = 1 };
        public SdlGpuWindowGraphicsSession Session { get; }
        public SdlGpuPrismDeviceResources Resources { get; }

        public Fixture(long hardBytes = 65536, long softBytes = 4096, int entries = 16)
        {
            window = Api.CreateWindow("prism-surface-ownership", 8, 8, SdlWindowOptions.Hidden);
            factory = new(Api, useMultisampling: false);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)), 8, 8, 1));
            Resources = new(Api, Session.Device, SdlGpuShaderFormats.Dxil, Session.DrawingResources,
                new PrismRendererOptions
                {
                    SurfaceHardByteLimit = hardBytes,
                    RetainedCacheSoftByteLimit = softBytes,
                    RetainedCacheEntryLimit = entries
                });
        }

        public SdlGpuPrismSurfaceLease Rent(int width = 8, int height = 8,
            SdlGpuTextureFormat format = SdlGpuTextureFormat.R8G8B8A8Unorm, bool mipmapped = false) =>
            Resources.RentSurface(Session.WindowIdentity, width, height, format, mipmapped);

        public SdlGpuPrismSurfaceLease Acquire(PrismRetainedCacheKey key)
        {
            Assert.True(Resources.TryAcquireRetained(key, Session.WindowIdentity, out SdlGpuPrismSurfaceLease lease));
            return lease;
        }

        public SdlGpuWindowGraphicsSession CreateAdditionalSession(string title)
        {
            nint additionalWindow = Api.CreateWindow(title, 8, 8, SdlWindowOptions.Hidden);
            additionalWindows.Add(additionalWindow);
            return Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(additionalWindow, Api.GetWindowId(additionalWindow)),
                8,
                8,
                1));
        }

        public void Promote(PrismRetainedCacheKey key)
        {
            using SdlGpuPrismSurfaceLease lease = Rent();
            Resources.Promote(key, lease);
            Assert.True(lease.IsRetained);
        }

        public void Dispose()
        {
            Resources.Dispose();
            Session.Dispose();
            factory.Dispose();
            foreach (nint additionalWindow in additionalWindows)
            {
                Api.DestroyWindow(additionalWindow);
            }
            Api.DestroyWindow(window);
        }
    }
}
