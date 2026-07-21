namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal readonly record struct PrismSurfaceBudget
{
    public PrismSurfaceBudget(
        long hardByteLimit,
        long retainedSoftByteLimit,
        int retainedEntryLimit)
    {
        if (hardByteLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hardByteLimit));
        }
        if (retainedSoftByteLimit < 0 ||
            retainedSoftByteLimit > hardByteLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retainedSoftByteLimit));
        }
        if (retainedEntryLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retainedEntryLimit));
        }

        HardByteLimit = hardByteLimit;
        RetainedSoftByteLimit = retainedSoftByteLimit;
        RetainedEntryLimit = retainedEntryLimit;
    }

    public long HardByteLimit { get; }

    public long RetainedSoftByteLimit { get; }

    public int RetainedEntryLimit { get; }

    public static PrismSurfaceBudget Unbounded { get; } =
        new(long.MaxValue, long.MaxValue, int.MaxValue);
}

internal sealed class PrismSurfaceMemoryAccountant
{
    private readonly int ownerThreadId =
        Environment.CurrentManagedThreadId;

    public PrismSurfaceMemoryAccountant(
        PrismSurfaceBudget budget)
    {
        Budget = budget;
    }

    public PrismSurfaceBudget Budget { get; }

    public long TransientByteCount { get; private set; }

    public long RetainedByteCount { get; private set; }

    public long TotalByteCount =>
        TransientByteCount + RetainedByteCount;

    public long PeakTotalByteCount { get; private set; }

    public bool CanReserveTransient(long byteCount)
    {
        VerifyAccess();
        ValidateByteCount(byteCount);
        return TotalByteCount <=
            Budget.HardByteLimit - byteCount;
    }

    public bool TryReserveTransient(long byteCount)
    {
        if (!CanReserveTransient(byteCount))
        {
            return false;
        }

        TransientByteCount =
            checked(TransientByteCount + byteCount);
        PeakTotalByteCount = Math.Max(
            PeakTotalByteCount,
            TotalByteCount);
        return true;
    }

    public void ReleaseTransient(long byteCount)
    {
        VerifyAccess();
        ValidateRelease(
            byteCount,
            TransientByteCount,
            "transient");
        TransientByteCount -= byteCount;
    }

    public void TransferTransientToRetained(long byteCount)
    {
        VerifyAccess();
        ValidateRelease(
            byteCount,
            TransientByteCount,
            "transient");
        TransientByteCount -= byteCount;
        RetainedByteCount =
            checked(RetainedByteCount + byteCount);
    }

    public void TransferRetainedToTransient(long byteCount)
    {
        VerifyAccess();
        ValidateRelease(
            byteCount,
            RetainedByteCount,
            "retained");
        RetainedByteCount -= byteCount;
        TransientByteCount =
            checked(TransientByteCount + byteCount);
    }

    public void ReleaseRetained(long byteCount)
    {
        VerifyAccess();
        ValidateRelease(
            byteCount,
            RetainedByteCount,
            "retained");
        RetainedByteCount -= byteCount;
    }

    public void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException(
                "Prism GPU surfaces must remain on their owning render thread.");
        }
    }

    private static void ValidateRelease(
        long byteCount,
        long available,
        string owner)
    {
        ValidateByteCount(byteCount);
        if (byteCount > available)
        {
            throw new InvalidOperationException(
                $"Prism {owner} surface byte accounting underflowed.");
        }
    }

    private static void ValidateByteCount(long byteCount)
    {
        if (byteCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }
    }
}
