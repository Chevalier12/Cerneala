using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectVariantTests
{
    [Fact]
    public void VariantKeyIsTypedByOwnerControl()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");

        Assert.Equal("kind", key.Name);
        Assert.Equal(typeof(Button), key.OwnerType);
        Assert.Equal(typeof(ButtonKind), key.ValueType);
    }

    [Fact]
    public void VariantSetStoresTypedVariantValues()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
        AspectVariantSet variants = AspectVariantSet.Empty.Set(key, ButtonKind.Primary);

        Assert.True(variants.TryGet(key, out ButtonKind value));
        Assert.Equal(ButtonKind.Primary, value);
    }

    [Fact]
    public void VariantSetRejectsValueFromDifferentKeyType()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");

        Assert.Throws<ArgumentException>(() => AspectVariantSet.Empty.Set(key, "primary"));
    }

    private enum ButtonKind
    {
        Neutral,
        Primary
    }
}
