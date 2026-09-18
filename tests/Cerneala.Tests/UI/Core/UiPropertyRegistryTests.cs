using Cerneala.UI.Core;
using Xunit.Abstractions;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyRegistryTests(ITestOutputHelper output)
{
    [Fact]
    public void RegisterAssignsStableUniqueIdentity()
    {
        UiProperty<int> first = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0));
        UiProperty<int> second = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0));

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.DiagnosticName, second.DiagnosticName);
    }

    [Fact]
    public void RegisterRejectsDuplicateOwnerAndName()
    {
        string name = UniqueName();

        UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(0));

        Assert.Throws<InvalidOperationException>(
            () => UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(
            () => UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(0)));
    }

    [Fact]
    public void OptionQueriesReuseSnapshotUntilAPropertyIsRegistered()
    {
        _ = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.Inherits));
        IReadOnlyList<UiProperty> first = UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.Inherits);
        IReadOnlyList<UiProperty> second = UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.Inherits);

        Assert.Same(first, second);

        UiProperty<int> registered = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.Inherits));
        IReadOnlyList<UiProperty> refreshed = UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.Inherits);

        Assert.NotSame(first, refreshed);
        Assert.Contains(registered, refreshed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SnapshotCreationAllocatesOnlyItsResultStorage(bool filtered)
    {
        const UiPropertyOptions options = UiPropertyOptions.ReadOnly;
        for (int index = 0; index < 32; index++)
        {
            _ = UiProperty<int>.Register(UniqueName(), typeof(UiPropertyRegistryTests), new(0, options));
        }
        _ = ReadSnapshot();
        _ = UiProperty<int>.Register(UniqueName(), typeof(UiPropertyRegistryTests), new(0, options));

        long before = GC.GetAllocatedBytesForCurrentThread();
        IReadOnlyList<UiProperty> snapshot = ReadSnapshot();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        // One exact-sized reference array plus its header/read-only wrapper.
        long budget = (long)snapshot.Count * IntPtr.Size + 128;
        output.WriteLine($"Snapshot filtered={filtered}: count={snapshot.Count}, allocated={allocated}, budget={budget} bytes.");
        Assert.InRange(allocated, 0, budget);

        IReadOnlyList<UiProperty> ReadSnapshot() => filtered
            ? UiPropertyRegistry.GetPropertiesWithOptions(options)
            : UiPropertyRegistry.GetRegisteredProperties();
    }

    [Fact]
    public void SnapshotsRemainImmutableAndOrderedAfterRegistration()
    {
        IReadOnlyList<UiProperty> original = UiPropertyRegistry.GetRegisteredProperties();
        UiProperty[] originalValues = original.ToArray();
        Assert.Same(original, UiPropertyRegistry.GetRegisteredProperties());
        UiProperty<int> added = UiProperty<int>.Register(UniqueName(), typeof(UiPropertyRegistryTests), new(0));
        IReadOnlyList<UiProperty> current = UiPropertyRegistry.GetRegisteredProperties();

        Assert.Equal(originalValues, original);
        Assert.DoesNotContain(added, original);
        Assert.NotSame(original, current);
        Assert.Contains(added, current);
        Assert.Equal(current.OrderBy(static property => property.Id), current);
        Assert.Equal(current, UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.None));
        Assert.Throws<NotSupportedException>(() => ((IList<UiProperty>)current).Add(added));
    }

    [Fact]
    public void DuplicateFailureDoesNotPublishOrInvalidateSnapshots()
    {
        string name = UniqueName();
        UiProperty<int> first = UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new(0));
        IReadOnlyList<UiProperty> all = UiPropertyRegistry.GetRegisteredProperties();
        IReadOnlyList<UiProperty> filtered = UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.None);

        Assert.Throws<InvalidOperationException>(() => UiProperty<double>.Register(name, typeof(UiPropertyRegistryTests), new(1)));
        Assert.Same(all, UiPropertyRegistry.GetRegisteredProperties());
        Assert.Same(filtered, UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.None));
        Assert.Same(first, Assert.Single(all.Where(property => property.OwnerType == typeof(UiPropertyRegistryTests) && property.Name == name)));
    }

    [Fact]
    public async Task ConcurrentRegistrationPublishesEachIdentityOnceInIdOrder()
    {
        string name = UniqueName();
        Task<UiProperty<int>?>[] attempts = Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            try { return UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new(0)); }
            catch (InvalidOperationException) { return null; }
        })).ToArray();
        UiProperty<int>?[] results = await Task.WhenAll(attempts);
        UiProperty<int>? winner = Assert.Single(results.Where(static property => property is not null));
        IReadOnlyList<UiProperty> snapshot = UiPropertyRegistry.GetRegisteredProperties();
        Assert.Same(winner, Assert.Single(snapshot.Where(property => property.OwnerType == typeof(UiPropertyRegistryTests) && property.Name == name)));
        Assert.Equal(snapshot.OrderBy(static property => property.Id), snapshot);
    }

    [Fact]
    public void ReentrantOwnerMetadataPreservesIdOrderRatherThanPublicationOrder()
    {
        const UiPropertyOptions options = UiPropertyOptions.ReadOnly;
        UiProperty<int>? nested = null;
        ReentrantOwnerType owner = new(() => nested = UiProperty<int>.Register(UniqueName(), typeof(UiPropertyRegistryTests), new(0, options)));
        UiProperty<int> outer = UiProperty<int>.Register(UniqueName(), owner, new(0, options));

        Assert.NotNull(nested);
        Assert.True(outer.Id < nested.Id);
        foreach (IReadOnlyList<UiProperty> snapshot in new[]
        {
            UiPropertyRegistry.GetRegisteredProperties(), UiPropertyRegistry.GetPropertiesWithOptions(options)
        })
        {
            Assert.Equal(snapshot.OrderBy(static property => property.Id), snapshot);
            Assert.Contains(outer, snapshot);
            Assert.Contains(nested, snapshot);
        }
    }

    private sealed class ReentrantOwnerType(Action onNameRead) : System.Reflection.TypeDelegator(typeof(UiPropertyRegistryTests))
    {
        private Action? callback = onNameRead;

        public override string? FullName
        {
            get
            {
                Interlocked.Exchange(ref callback, null)?.Invoke();
                return base.FullName;
            }
        }
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyRegistryTests)}_{Guid.NewGuid():N}";
    }
}
