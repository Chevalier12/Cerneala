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
}
