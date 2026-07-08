using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Aspect;

public static class DefaultAspectTokens
{
    public static class Color
    {
        public static readonly AspectToken<DrawColor> Background = AspectToken.Color("color.background");
        public static readonly AspectToken<DrawColor> Foreground = AspectToken.Color("color.foreground");
        public static readonly AspectToken<DrawColor> Surface = AspectToken.Color("color.surface");
        public static readonly AspectToken<DrawColor> Border = AspectToken.Color("color.border");
        public static readonly AspectToken<DrawColor> Accent = AspectToken.Color("color.accent");
    }

    public static class Typography
    {
        public static readonly AspectToken<string> FontFamily = AspectToken.String("typography.font-family");
        public static readonly AspectToken<float> FontSize = AspectToken.Float("typography.font-size");
    }

    public static class Spacing
    {
        public static readonly AspectToken<Thickness> ControlPadding = AspectToken.Thickness("spacing.control-padding");
    }

    public static class Stroke
    {
        public static readonly AspectToken<Thickness> ControlBorderThickness = AspectToken.Thickness("stroke.control-border-thickness");
    }

    public static class Motion
    {
        public static readonly AspectToken<MotionSpec> Fast = AspectToken.Motion("motion.fast");
        public static readonly AspectToken<MotionSpec> Normal = AspectToken.Motion("motion.normal");
    }
}
