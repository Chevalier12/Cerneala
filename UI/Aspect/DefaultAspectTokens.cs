using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Aspect;

public static class DefaultAspectTokens
{
    public static class Color
    {
        public static readonly AspectToken<global::Cerneala.Drawing.Color> Background = AspectToken.Color("color.background");
        public static readonly AspectToken<global::Cerneala.Drawing.Color> Foreground = AspectToken.Color("color.foreground");
        public static readonly AspectToken<global::Cerneala.Drawing.Color> Surface = AspectToken.Color("color.surface");
        public static readonly AspectToken<global::Cerneala.Drawing.Color> Border = AspectToken.Color("color.border");
        public static readonly AspectToken<global::Cerneala.Drawing.Color> Accent = AspectToken.Color("color.accent");
    }

    public static class Typography
    {
        public static readonly AspectToken<string> FontFamily = AspectToken.String("typography.font-family");
        public static readonly AspectToken<float> FontSize = AspectToken.Float("typography.font-size");
    }

    public static class Brush
    {
        public static readonly AspectToken<global::Cerneala.UI.Media.Brush?> Background =
            AspectToken.Create<global::Cerneala.UI.Media.Brush?>("brush.background");
        public static readonly AspectToken<global::Cerneala.UI.Media.Brush?> Surface =
            AspectToken.Create<global::Cerneala.UI.Media.Brush?>("brush.surface");
        public static readonly AspectToken<global::Cerneala.UI.Media.Brush?> Border =
            AspectToken.Create<global::Cerneala.UI.Media.Brush?>("brush.border");
        public static readonly AspectToken<global::Cerneala.UI.Media.Brush?> Foreground =
            AspectToken.Create<global::Cerneala.UI.Media.Brush?>("brush.foreground");
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
