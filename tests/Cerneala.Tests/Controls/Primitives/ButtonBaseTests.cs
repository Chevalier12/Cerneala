using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class ButtonBaseTests
{
    [Fact]
    public void ButtonBaseInheritsControlProperties()
    {
        ButtonBase button = new()
        {
            Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
            Padding = new Cerneala.UI.Layout.Thickness(2)
        };

        Assert.IsAssignableFrom<Control>(button);
        Assert.Equal(new Cerneala.UI.Media.SolidColorBrush(Color.White), button.Background);
        Assert.Equal(new Cerneala.UI.Layout.Thickness(2), button.Padding);
    }
}
