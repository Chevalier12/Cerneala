using System.Numerics;

namespace Cerneala.Drawing.Prism.ColorManagement;


internal static class PrismOklab
{
    public static Vector3 FromLinearSrgb(Vector3 color)
    {
        double l =
            (0.4122214708 * color.X) +
            (0.5363325363 * color.Y) +
            (0.0514459929 * color.Z);
        double m =
            (0.2119034982 * color.X) +
            (0.6806995451 * color.Y) +
            (0.1073969566 * color.Z);
        double s =
            (0.0883024619 * color.X) +
            (0.2817188376 * color.Y) +
            (0.6299787005 * color.Z);

        l = Math.Cbrt(l);
        m = Math.Cbrt(m);
        s = Math.Cbrt(s);

        return new Vector3(
            (float)((0.2104542553 * l) +
                (0.7936177850 * m) -
                (0.0040720468 * s)),
            (float)((1.9779984951 * l) -
                (2.4285922050 * m) +
                (0.4505937099 * s)),
            (float)((0.0259040371 * l) +
                (0.7827717662 * m) -
                (0.8086757660 * s)));
    }

    public static Vector3 ToLinearSrgb(Vector3 color)
    {
        double l = color.X +
            (0.3963377774 * color.Y) +
            (0.2158037573 * color.Z);
        double m = color.X -
            (0.1055613458 * color.Y) -
            (0.0638541728 * color.Z);
        double s = color.X -
            (0.0894841775 * color.Y) -
            (1.2914855480 * color.Z);

        l = l * l * l;
        m = m * m * m;
        s = s * s * s;

        return new Vector3(
            (float)((4.0767416621 * l) -
                (3.3077115913 * m) +
                (0.2309699292 * s)),
            (float)((-1.2684380046 * l) +
                (2.6097574011 * m) -
                (0.3413193965 * s)),
            (float)((-0.0041960863 * l) -
                (0.7034186147 * m) +
                (1.7076147010 * s)));
    }
}
