using System.Globalization;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Detective;

// The layout records format their float members with the current culture, and string.Create(InvariantCulture, ...)
// does not reach them because they are not IFormattable. These overloads write the same record text with
// invariant-culture numbers, so the Detective snapshot strings are identical in every culture.
internal static class DetectiveInvariantText
{
    public static string Format(LayoutPoint point) =>
        string.Create(CultureInfo.InvariantCulture, $"LayoutPoint {{ X = {point.X}, Y = {point.Y} }}");

    public static string Format(LayoutSize size) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"LayoutSize {{ Width = {size.Width}, Height = {size.Height}, IsWidthUnconstrained = {size.IsWidthUnconstrained}, IsHeightUnconstrained = {size.IsHeightUnconstrained} }}");

    public static string Format(LayoutRect rect) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"LayoutRect {{ X = {rect.X}, Y = {rect.Y}, Width = {rect.Width}, Height = {rect.Height}, Location = {Format(rect.Location)}, Size = {Format(rect.Size)} }}");
}
