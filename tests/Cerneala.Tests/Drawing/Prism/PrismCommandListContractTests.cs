using System.Collections;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCommandListContractTests
{
    [Fact]
    public void StructuralVersionAdvancesDeterministicallyForEveryMutation()
    {
        DrawCommandList commands = new();
        long initial = commands.Version;

        commands.Add(DrawCommand.PopClip());
        long afterAdd = commands.Version;
        commands.Clear();
        long afterNonEmptyClear = commands.Version;
        commands.Clear();
        long afterEmptyClear = commands.Version;

        Assert.Equal(unchecked(initial + 1), afterAdd);
        Assert.Equal(unchecked(afterAdd + 1), afterNonEmptyClear);
        Assert.Equal(unchecked(afterNonEmptyClear + 1), afterEmptyClear);
    }

    [Fact]
    public void PrismPayloadHasNoGenericMetadataEscapeHatch()
    {
        Type[] payloadTypes =
        [
            typeof(PrismDrawScope),
            typeof(PrismCacheOwnerToken)
        ];

        Assert.DoesNotContain(
            payloadTypes.SelectMany(type => type.GetProperties()),
            property =>
                property.PropertyType == typeof(object) ||
                typeof(IDictionary).IsAssignableFrom(property.PropertyType));
    }
}
