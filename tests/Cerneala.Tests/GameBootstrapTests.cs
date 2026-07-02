using Microsoft.Xna.Framework;

namespace Cerneala.Tests;

public sealed class GameBootstrapTests
{
    [Fact]
    public void CreateDefaultClearColorReturnsCornflowerBlue()
    {
        Assert.Equal(Color.CornflowerBlue, GameBootstrap.CreateDefaultClearColor());
    }
}
