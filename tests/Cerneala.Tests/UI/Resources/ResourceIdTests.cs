using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ResourceIdTests
{
    [Fact]
    public void ResourceIdStoresKeyAndType()
    {
        ResourceId<string> id = new("PrimaryFont");

        Assert.Equal("PrimaryFont", id.Key);
        Assert.Equal(typeof(string), id.ResourceType);
    }

    [Fact]
    public void SameKeyDifferentTypesAreNotInterchangeable()
    {
        ResourceId<string> text = new("Shared");
        ResourceId<int> number = new("Shared");

        Assert.NotEqual(text.ToString(), number.ToString());
    }

    [Fact]
    public void ResourceIdRejectsEmptyKey()
    {
        Assert.Throws<ArgumentException>(() => new ResourceId<string>(""));
    }

    [Fact]
    public void ResourceIdFormattingDoesNotRebuildTypeMetadataAfterCollection()
    {
        ResourceId<FormattingResource> id = new("atlas");
        string expected = $"{typeof(FormattingResource).FullName}:atlas";
        for (int index = 0; index < 128; index++) { _ = id.ToString(); }

        for (int cycle = 0; cycle < 4; cycle++)
        {
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            long before = GC.GetAllocatedBytesForCurrentThread();
            string afterCollection = id.ToString();
            long collectionBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            string steady = id.ToString();
            long steadyBytes = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(expected, afterCollection);
            Assert.Equal(expected, steady);
            Assert.Equal(steadyBytes, collectionBytes);
        }
    }

    private sealed class FormattingResource;
}
