using System.Text.Json;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneJsonValue2DTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("123456789012345678901234567890.50")]
    [InlineData("\"text\"")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"a\": [1, null], \"a\": false}")]
    public void ContentSurvivesDisposalOfItsInputDocument(string json)
    {
        SceneJsonValue2D value;
        using (JsonDocument document = JsonDocument.Parse(json)) { value = new(document.RootElement); }
        Assert.Equal(json, value.Value.GetRawText());
    }

    [Fact]
    public void IdentityIsExplicitlyReferenceBasedEvenForTheSameJsonElement()
    {
        using JsonDocument document = JsonDocument.Parse("{\"n\":1}");
        SceneJsonValue2D first = new(document.RootElement);
        SceneJsonValue2D second = new(document.RootElement);
        Assert.True(first.Equals(first));
        Assert.False(first.Equals(second));
        Assert.Equal(first.Value.GetRawText(), second.Value.GetRawText());
        Assert.Equal(2, new HashSet<SceneJsonValue2D> { first, first, second }.Count);
    }

    [Fact]
    public void UndefinedAndDisposedInputCannotProduceMetadata()
    {
        Assert.Throws<ArgumentException>(() => new SceneJsonValue2D(default));
        using JsonDocument document = JsonDocument.Parse("1");
        JsonElement element = document.RootElement;
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() => new SceneJsonValue2D(element));
    }
}
