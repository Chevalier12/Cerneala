using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Controls;

internal static class SliderTemplates
{
    public static readonly ComponentTemplate<Slider> Default = new("Slider.Default", context =>
    {
        Track track = new();
        context.RequirePart("PART_Track", track);
        return track;
    });
}
