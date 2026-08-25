namespace Cerneala.Drawing.Paths;

internal static class SvgPathFlattener
{
    public static IReadOnlyList<DrawPoint[]> Flatten(string data, float tolerance) =>
        DrawPathFlattener.Flatten(DrawPathParser.ParseSvg(data), tolerance);
}
