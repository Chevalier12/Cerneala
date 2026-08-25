using Cerneala.Drawing;

namespace Cerneala.Tests;

public sealed class GameBootstrapTests
{
    [Fact]
    public void CreateDefaultClearColorReturnsCornflowerBlue()
    {
        Assert.Equal(Color.CornflowerBlue, GameBootstrap.CreateDefaultClearColor());
    }
}
