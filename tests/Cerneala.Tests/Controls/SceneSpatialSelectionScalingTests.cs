using System.Collections;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneSpatialSelectionScalingTests
{
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    public async Task ExactRequestsScanOneImmutableCatalogOnceRatherThanOncePerEntry(int count)
    {
        SceneSpatialEntry2D[] entries = Enumerable.Range(0, count).Select(i => Entry(i.ToString())).ToArray();
        CountingCatalog catalog = new(entries);
        Source source = new(catalog);
        using SceneSpatialResidency2D<object> residency = new(source);
        for (int i = 0; i < 32; i++)
        {
            using var region = await residency.AcquireEntriesAsync(catalog, [entries[i * (count / 32)]]);
            Assert.Same(entries[i * (count / 32)], Assert.Single(region.Entries));
        }
        Assert.Equal(32, source.Loads);
        Assert.Equal(32, source.Releases);
        Assert.Equal(0, residency.ResidentCount);
        Assert.InRange(catalog.Visits, count, count * 2L);

        catalog.Visits = 0;
        using var last = await residency.AcquireEntriesAsync(catalog, [entries[^1], entries[0]]);
        Assert.Equal([entries[0], entries[^1]], last.Entries);
        Assert.Equal(0, catalog.Visits);
    }

    [Fact]
    public async Task ReplacementSnapshotChangesMembershipAndOrderWithoutRevokingOldRegions()
    {
        SceneSpatialEntry2D a = Entry("a"), b = Entry("b");
        CountingCatalog first = new([a, b]);
        Source source = new(first);
        using SceneSpatialResidency2D<object> residency = new(source);
        using var old = await residency.AcquireEntriesAsync(first, [b, a]);
        object original = old.GetValue("a");
        SceneSpatialEntry2D movedA = new("a", new(100, 0, 1, 1));
        CountingCatalog replacement = new([b, movedA]);
        source.Catalog = replacement;
        await Assert.ThrowsAsync<InvalidOperationException>(() => residency.AcquireEntriesAsync(first, [a]).AsTask());
        await Assert.ThrowsAsync<ArgumentException>(() => residency.AcquireEntriesAsync(replacement, [a]).AsTask());
        using var current = await residency.AcquireEntriesAsync(replacement, [movedA, b]);
        Assert.Equal([b, movedA], current.Entries);
        Assert.Same(original, current.GetValue("a"));
        Assert.False(old.IsCurrent);
        Assert.True(current.IsCurrent);
        Assert.Equal(2, source.Loads);
        Assert.Equal(0, source.Releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidReplacementCatalogIsRejectedBeforeAnyAcquisitionAndDoesNotPoisonLaterSnapshots(bool nullEntry)
    {
        SceneSpatialEntry2D a = Entry("a");
        CountingCatalog first = new([a]);
        Source source = new(first);
        using SceneSpatialResidency2D<object> residency = new(source);
        using (var initial = await residency.AcquireEntriesAsync(first, [a])) { }
        CountingCatalog invalid = new([a, nullEntry ? null! : Entry("a")]);
        source.Catalog = invalid;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Exception? failure = await Record.ExceptionAsync(() => residency.AcquireEntriesAsync(invalid, [a]).AsTask());
            if (nullEntry) Assert.IsType<ArgumentNullException>(failure);
            else Assert.IsType<InvalidOperationException>(failure);
        }
        Assert.Equal(1, source.Loads);
        Assert.Equal(1, source.Releases);
        SceneSpatialEntry2D b = Entry("b");
        CountingCatalog valid = new([b, a]);
        source.Catalog = valid;
        using var recovered = await residency.AcquireEntriesAsync(valid, [a, b]);
        Assert.Equal([b, a], recovered.Entries);
        Assert.Equal(3, source.Loads);
    }

    [Fact]
    public async Task ConcurrentExactRequestsShareCatalogValidationAndKeepIndependentRegions()
    {
        SceneSpatialEntry2D[] entries = Enumerable.Range(0, 4096).Select(i => Entry(i.ToString())).ToArray();
        CountingCatalog catalog = new(entries);
        Source source = new(catalog);
        using SceneSpatialResidency2D<object> residency = new(source);
        Task<SceneSpatialRegion2D<object>>[] work = Enumerable.Range(0, 32)
            .Select(i => Task.Run(async () => await residency.AcquireEntriesAsync(catalog, [entries[i]]))).ToArray();
        SceneSpatialRegion2D<object>[] regions = await Task.WhenAll(work);
        try
        {
            Assert.Equal(32, residency.ResidentCount);
            Assert.InRange(catalog.Visits, entries.Length, entries.Length * 2L);
            for (int i = 0; i < regions.Length; i++) Assert.Same(entries[i], Assert.Single(regions[i].Entries));
        }
        finally { foreach (var region in regions) region.Dispose(); }
        Assert.Equal(32, source.Loads);
        Assert.Equal(32, source.Releases);
        Assert.Equal(0, residency.ResidentCount);
        Assert.Equal(0, residency.PendingLoadCount);
    }

    private static SceneSpatialEntry2D Entry(string id) => new(id, new DrawRect(0, 0, 1, 1));

    private sealed class CountingCatalog(SceneSpatialEntry2D[] entries) : IReadOnlyList<SceneSpatialEntry2D>
    {
        internal long Visits;
        public int Count => entries.Length;
        public SceneSpatialEntry2D this[int index] { get { Interlocked.Increment(ref Visits); return entries[index]; } }
        public IEnumerator<SceneSpatialEntry2D> GetEnumerator()
        {
            foreach (var entry in entries) { Interlocked.Increment(ref Visits); yield return entry; }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class Source(CountingCatalog catalog) : ISceneSpatialSource2D<object>
    {
        internal CountingCatalog Catalog = catalog;
        internal int Loads, Releases;
        public IReadOnlyList<SceneSpatialEntry2D> Entries => Catalog;
        public event EventHandler? Changed { add { } remove { } }
        public ValueTask<SceneSpatialLease2D<object>> LoadAsync(SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref Loads);
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(new(), _ => Interlocked.Increment(ref Releases)));
        }
    }
}
