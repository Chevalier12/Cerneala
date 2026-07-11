using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonTokens
{
    public static readonly AspectToken<Brush?> Background = AspectToken.Create<Brush?>("button.background");
    public static readonly AspectToken<Color> Foreground = AspectToken.Color("button.foreground");
    public static readonly AspectToken<Brush?> BorderBrush = AspectToken.Create<Brush?>("button.border");
    public static readonly AspectToken<Brush?> HoverBackground = AspectToken.Create<Brush?>("button.hover-background");
    public static readonly AspectToken<Brush?> PressedBackground = AspectToken.Create<Brush?>("button.pressed-background");
    public static readonly AspectToken<float> DisabledOpacity = AspectToken.Float("button.disabled-opacity");
    public static readonly AspectToken<Thickness> Padding = AspectToken.Thickness("button.padding");
}
