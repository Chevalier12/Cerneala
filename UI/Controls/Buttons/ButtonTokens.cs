using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonTokens
{
    public static readonly AspectToken<Color> Background = AspectToken.Color("button.background");
    public static readonly AspectToken<Color> Foreground = AspectToken.Color("button.foreground");
    public static readonly AspectToken<Color> BorderBrush = AspectToken.Color("button.border");
    public static readonly AspectToken<Color> HoverBackground = AspectToken.Color("button.hover-background");
    public static readonly AspectToken<Color> PressedBackground = AspectToken.Color("button.pressed-background");
    public static readonly AspectToken<float> DisabledOpacity = AspectToken.Float("button.disabled-opacity");
    public static readonly AspectToken<Thickness> Padding = AspectToken.Thickness("button.padding");
}
