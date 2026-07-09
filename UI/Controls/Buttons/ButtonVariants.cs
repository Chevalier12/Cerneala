using Cerneala.UI.Aspect;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonVariants
{
    public static readonly AspectVariantKey<Button, ButtonKind> Kind = AspectVariantKey.For<Button, ButtonKind>("kind");
    public static readonly AspectVariantKey<Button, ButtonSize> Size = AspectVariantKey.For<Button, ButtonSize>("size");
}
