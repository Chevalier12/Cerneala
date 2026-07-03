using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Path : Shape
{
    public static readonly UiProperty<PathGeometry?> DataProperty = UiProperty<PathGeometry?>.Register(
        nameof(Data),
        typeof(Path),
        new UiPropertyMetadata<PathGeometry?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public PathGeometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Geometry? ResolveGeometry(LayoutRect arrangedBounds)
    {
        return Geometry ?? Data;
    }
}
