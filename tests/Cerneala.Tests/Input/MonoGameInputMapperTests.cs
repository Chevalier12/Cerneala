using Cerneala.UI.Input;
using Cerneala.UI.Input.MonoGame;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Tests.Input;

public sealed class MonoGameInputMapperTests
{
    [Theory]
    [InlineData(Keys.Enter, InputKey.Enter)]
    [InlineData(Keys.Escape, InputKey.Escape)]
    [InlineData(Keys.A, InputKey.A)]
    [InlineData(Keys.LeftShift, InputKey.LeftShift)]
    public void MapsMonoGameKeys(Keys monoGameKey, InputKey expected)
    {
        Assert.Equal(expected, MonoGameInputMapper.MapKey(monoGameKey));
    }

    [Fact]
    public void UnmappedKeysReturnUnknown()
    {
        Assert.Equal(InputKey.Unknown, MonoGameInputMapper.MapKey((Keys)9999));
    }
}
