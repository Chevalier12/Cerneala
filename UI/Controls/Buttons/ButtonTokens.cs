using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonTokens
{
    public static readonly AspectToken<DrawColor> Background = AspectToken.Color("button.background");
    public static readonly AspectToken<DrawColor> Foreground = AspectToken.Color("button.foreground");
    public static readonly AspectToken<DrawColor> BorderColor = AspectToken.Color("button.border");
    public static readonly AspectToken<DrawColor> HoverBackground = AspectToken.Color("button.hover-background");
    public static readonly AspectToken<DrawColor> PressedBackground = AspectToken.Color("button.pressed-background");
    public static readonly AspectToken<float> DisabledOpacity = AspectToken.Float("button.disabled-opacity");
    public static readonly AspectToken<Thickness> Padding = AspectToken.Thickness("button.padding");
}
