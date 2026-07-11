using System.Globalization;

namespace Cerneala.Drawing;

/// <summary>
/// An immutable ARGB color value used by Cerneala drawing and controls.
/// </summary>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static Color Transparent { get; } = new(0, 0, 0, 0);
    public static Color AliceBlue { get; } = new(240, 248, 255);
    public static Color AntiqueWhite { get; } = new(250, 235, 215);
    public static Color Aqua { get; } = new(0, 255, 255);
    public static Color Aquamarine { get; } = new(127, 255, 212);
    public static Color Azure { get; } = new(240, 255, 255);
    public static Color Beige { get; } = new(245, 245, 220);
    public static Color Bisque { get; } = new(255, 228, 196);
    public static Color Black { get; } = new(0, 0, 0);
    public static Color BlanchedAlmond { get; } = new(255, 235, 205);
    public static Color Blue { get; } = new(0, 0, 255);
    public static Color BlueViolet { get; } = new(138, 43, 226);
    public static Color Brown { get; } = new(165, 42, 42);
    public static Color BurlyWood { get; } = new(222, 184, 135);
    public static Color CadetBlue { get; } = new(95, 158, 160);
    public static Color Chartreuse { get; } = new(127, 255, 0);
    public static Color Chocolate { get; } = new(210, 105, 30);
    public static Color Coral { get; } = new(255, 127, 80);
    public static Color CornflowerBlue { get; } = new(100, 149, 237);
    public static Color Cornsilk { get; } = new(255, 248, 220);
    public static Color Crimson { get; } = new(220, 20, 60);
    public static Color Cyan { get; } = new(0, 255, 255);
    public static Color DarkBlue { get; } = new(0, 0, 139);
    public static Color DarkCyan { get; } = new(0, 139, 139);
    public static Color DarkGoldenrod { get; } = new(184, 134, 11);
    public static Color DarkGray { get; } = new(169, 169, 169);
    public static Color DarkGreen { get; } = new(0, 100, 0);
    public static Color DarkKhaki { get; } = new(189, 183, 107);
    public static Color DarkMagenta { get; } = new(139, 0, 139);
    public static Color DarkOliveGreen { get; } = new(85, 107, 47);
    public static Color DarkOrange { get; } = new(255, 140, 0);
    public static Color DarkOrchid { get; } = new(153, 50, 204);
    public static Color DarkRed { get; } = new(139, 0, 0);
    public static Color DarkSalmon { get; } = new(233, 150, 122);
    public static Color DarkSeaGreen { get; } = new(143, 188, 143);
    public static Color DarkSlateBlue { get; } = new(72, 61, 139);
    public static Color DarkSlateGray { get; } = new(47, 79, 79);
    public static Color DarkTurquoise { get; } = new(0, 206, 209);
    public static Color DarkViolet { get; } = new(148, 0, 211);
    public static Color DeepPink { get; } = new(255, 20, 147);
    public static Color DeepSkyBlue { get; } = new(0, 191, 255);
    public static Color DimGray { get; } = new(105, 105, 105);
    public static Color DodgerBlue { get; } = new(30, 144, 255);
    public static Color Firebrick { get; } = new(178, 34, 34);
    public static Color FloralWhite { get; } = new(255, 250, 240);
    public static Color ForestGreen { get; } = new(34, 139, 34);
    public static Color Fuchsia { get; } = new(255, 0, 255);
    public static Color Gainsboro { get; } = new(220, 220, 220);
    public static Color GhostWhite { get; } = new(248, 248, 255);
    public static Color Gold { get; } = new(255, 215, 0);
    public static Color Goldenrod { get; } = new(218, 165, 32);
    public static Color Gray { get; } = new(128, 128, 128);
    public static Color Green { get; } = new(0, 128, 0);
    public static Color GreenYellow { get; } = new(173, 255, 47);
    public static Color Honeydew { get; } = new(240, 255, 240);
    public static Color HotPink { get; } = new(255, 105, 180);
    public static Color IndianRed { get; } = new(205, 92, 92);
    public static Color Indigo { get; } = new(75, 0, 130);
    public static Color Ivory { get; } = new(255, 255, 240);
    public static Color Khaki { get; } = new(240, 230, 140);
    public static Color Lavender { get; } = new(230, 230, 250);
    public static Color LavenderBlush { get; } = new(255, 240, 245);
    public static Color LawnGreen { get; } = new(124, 252, 0);
    public static Color LemonChiffon { get; } = new(255, 250, 205);
    public static Color LightBlue { get; } = new(173, 216, 230);
    public static Color LightCoral { get; } = new(240, 128, 128);
    public static Color LightCyan { get; } = new(224, 255, 255);
    public static Color LightGoldenrodYellow { get; } = new(250, 250, 210);
    public static Color LightGray { get; } = new(211, 211, 211);
    public static Color LightGreen { get; } = new(144, 238, 144);
    public static Color LightPink { get; } = new(255, 182, 193);
    public static Color LightSalmon { get; } = new(255, 160, 122);
    public static Color LightSeaGreen { get; } = new(32, 178, 170);
    public static Color LightSkyBlue { get; } = new(135, 206, 250);
    public static Color LightSlateGray { get; } = new(119, 136, 153);
    public static Color LightSteelBlue { get; } = new(176, 196, 222);
    public static Color LightYellow { get; } = new(255, 255, 224);
    public static Color Lime { get; } = new(0, 255, 0);
    public static Color LimeGreen { get; } = new(50, 205, 50);
    public static Color Linen { get; } = new(250, 240, 230);
    public static Color Magenta { get; } = new(255, 0, 255);
    public static Color Maroon { get; } = new(128, 0, 0);
    public static Color MediumAquamarine { get; } = new(102, 205, 170);
    public static Color MediumBlue { get; } = new(0, 0, 205);
    public static Color MediumOrchid { get; } = new(186, 85, 211);
    public static Color MediumPurple { get; } = new(147, 112, 219);
    public static Color MediumSeaGreen { get; } = new(60, 179, 113);
    public static Color MediumSlateBlue { get; } = new(123, 104, 238);
    public static Color MediumSpringGreen { get; } = new(0, 250, 154);
    public static Color MediumTurquoise { get; } = new(72, 209, 204);
    public static Color MediumVioletRed { get; } = new(199, 21, 133);
    public static Color MidnightBlue { get; } = new(25, 25, 112);
    public static Color MintCream { get; } = new(245, 255, 250);
    public static Color MistyRose { get; } = new(255, 228, 225);
    public static Color Moccasin { get; } = new(255, 228, 181);
    public static Color NavajoWhite { get; } = new(255, 222, 173);
    public static Color Navy { get; } = new(0, 0, 128);
    public static Color OldLace { get; } = new(253, 245, 230);
    public static Color Olive { get; } = new(128, 128, 0);
    public static Color OliveDrab { get; } = new(107, 142, 35);
    public static Color Orange { get; } = new(255, 165, 0);
    public static Color OrangeRed { get; } = new(255, 69, 0);
    public static Color Orchid { get; } = new(218, 112, 214);
    public static Color PaleGoldenrod { get; } = new(238, 232, 170);
    public static Color PaleGreen { get; } = new(152, 251, 152);
    public static Color PaleTurquoise { get; } = new(175, 238, 238);
    public static Color PaleVioletRed { get; } = new(219, 112, 147);
    public static Color PapayaWhip { get; } = new(255, 239, 213);
    public static Color PeachPuff { get; } = new(255, 218, 185);
    public static Color Peru { get; } = new(205, 133, 63);
    public static Color Pink { get; } = new(255, 192, 203);
    public static Color Plum { get; } = new(221, 160, 221);
    public static Color PowderBlue { get; } = new(176, 224, 230);
    public static Color Purple { get; } = new(128, 0, 128);
    public static Color Red { get; } = new(255, 0, 0);
    public static Color RosyBrown { get; } = new(188, 143, 143);
    public static Color RoyalBlue { get; } = new(65, 105, 225);
    public static Color SaddleBrown { get; } = new(139, 69, 19);
    public static Color Salmon { get; } = new(250, 128, 114);
    public static Color SandyBrown { get; } = new(244, 164, 96);
    public static Color SeaGreen { get; } = new(46, 139, 87);
    public static Color SeaShell { get; } = new(255, 245, 238);
    public static Color Sienna { get; } = new(160, 82, 45);
    public static Color Silver { get; } = new(192, 192, 192);
    public static Color SkyBlue { get; } = new(135, 206, 235);
    public static Color SlateBlue { get; } = new(106, 90, 205);
    public static Color SlateGray { get; } = new(112, 128, 144);
    public static Color Snow { get; } = new(255, 250, 250);
    public static Color SpringGreen { get; } = new(0, 255, 127);
    public static Color SteelBlue { get; } = new(70, 130, 180);
    public static Color Tan { get; } = new(210, 180, 140);
    public static Color Teal { get; } = new(0, 128, 128);
    public static Color Thistle { get; } = new(216, 191, 216);
    public static Color Tomato { get; } = new(255, 99, 71);
    public static Color Turquoise { get; } = new(64, 224, 208);
    public static Color Violet { get; } = new(238, 130, 238);
    public static Color Wheat { get; } = new(245, 222, 179);
    public static Color White { get; } = new(255, 255, 255);
    public static Color WhiteSmoke { get; } = new(245, 245, 245);
    public static Color Yellow { get; } = new(255, 255, 0);
    public static Color YellowGreen { get; } = new(154, 205, 50);

    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);

    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);

    /// <summary>
    /// Parses a WPF-style named color, #RRGGBB, #AARRGGBB, or comma-separated byte channels.
    /// </summary>
    public static bool TryParse(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        if (NamedColors.TryGetValue(value, out color))
        {
            return true;
        }

        if (value[0] == '#')
        {
            string hex = value[1..];
            if (hex.Length is not (6 or 8)
                || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
            {
                return false;
            }

            color = hex.Length == 6
                ? new((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed)
                : new((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, (byte)(packed >> 24));
            return true;
        }

        string[] channels = value.Split(',', StringSplitOptions.TrimEntries);
        if (channels.Length is not (3 or 4)
            || !TryParseByte(channels[0], out byte r)
            || !TryParseByte(channels[1], out byte g)
            || !TryParseByte(channels[2], out byte b))
        {
            return false;
        }

        byte a = 255;
        if (channels.Length == 4 && !TryParseByte(channels[3], out a))
        {
            return false;
        }

        color = new(r, g, b, a);
        return true;
    }

    private static readonly Dictionary<string, Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Transparent)] = Transparent,
        [nameof(AliceBlue)] = AliceBlue,
        [nameof(AntiqueWhite)] = AntiqueWhite,
        [nameof(Aqua)] = Aqua,
        [nameof(Aquamarine)] = Aquamarine,
        [nameof(Azure)] = Azure,
        [nameof(Beige)] = Beige,
        [nameof(Bisque)] = Bisque,
        [nameof(Black)] = Black,
        [nameof(BlanchedAlmond)] = BlanchedAlmond,
        [nameof(Blue)] = Blue,
        [nameof(BlueViolet)] = BlueViolet,
        [nameof(Brown)] = Brown,
        [nameof(BurlyWood)] = BurlyWood,
        [nameof(CadetBlue)] = CadetBlue,
        [nameof(Chartreuse)] = Chartreuse,
        [nameof(Chocolate)] = Chocolate,
        [nameof(Coral)] = Coral,
        [nameof(CornflowerBlue)] = CornflowerBlue,
        [nameof(Cornsilk)] = Cornsilk,
        [nameof(Crimson)] = Crimson,
        [nameof(Cyan)] = Cyan,
        [nameof(DarkBlue)] = DarkBlue,
        [nameof(DarkCyan)] = DarkCyan,
        [nameof(DarkGoldenrod)] = DarkGoldenrod,
        [nameof(DarkGray)] = DarkGray,
        [nameof(DarkGreen)] = DarkGreen,
        [nameof(DarkKhaki)] = DarkKhaki,
        [nameof(DarkMagenta)] = DarkMagenta,
        [nameof(DarkOliveGreen)] = DarkOliveGreen,
        [nameof(DarkOrange)] = DarkOrange,
        [nameof(DarkOrchid)] = DarkOrchid,
        [nameof(DarkRed)] = DarkRed,
        [nameof(DarkSalmon)] = DarkSalmon,
        [nameof(DarkSeaGreen)] = DarkSeaGreen,
        [nameof(DarkSlateBlue)] = DarkSlateBlue,
        [nameof(DarkSlateGray)] = DarkSlateGray,
        [nameof(DarkTurquoise)] = DarkTurquoise,
        [nameof(DarkViolet)] = DarkViolet,
        [nameof(DeepPink)] = DeepPink,
        [nameof(DeepSkyBlue)] = DeepSkyBlue,
        [nameof(DimGray)] = DimGray,
        [nameof(DodgerBlue)] = DodgerBlue,
        [nameof(Firebrick)] = Firebrick,
        [nameof(FloralWhite)] = FloralWhite,
        [nameof(ForestGreen)] = ForestGreen,
        [nameof(Fuchsia)] = Fuchsia,
        [nameof(Gainsboro)] = Gainsboro,
        [nameof(GhostWhite)] = GhostWhite,
        [nameof(Gold)] = Gold,
        [nameof(Goldenrod)] = Goldenrod,
        [nameof(Gray)] = Gray,
        [nameof(Green)] = Green,
        [nameof(GreenYellow)] = GreenYellow,
        [nameof(Honeydew)] = Honeydew,
        [nameof(HotPink)] = HotPink,
        [nameof(IndianRed)] = IndianRed,
        [nameof(Indigo)] = Indigo,
        [nameof(Ivory)] = Ivory,
        [nameof(Khaki)] = Khaki,
        [nameof(Lavender)] = Lavender,
        [nameof(LavenderBlush)] = LavenderBlush,
        [nameof(LawnGreen)] = LawnGreen,
        [nameof(LemonChiffon)] = LemonChiffon,
        [nameof(LightBlue)] = LightBlue,
        [nameof(LightCoral)] = LightCoral,
        [nameof(LightCyan)] = LightCyan,
        [nameof(LightGoldenrodYellow)] = LightGoldenrodYellow,
        [nameof(LightGray)] = LightGray,
        [nameof(LightGreen)] = LightGreen,
        [nameof(LightPink)] = LightPink,
        [nameof(LightSalmon)] = LightSalmon,
        [nameof(LightSeaGreen)] = LightSeaGreen,
        [nameof(LightSkyBlue)] = LightSkyBlue,
        [nameof(LightSlateGray)] = LightSlateGray,
        [nameof(LightSteelBlue)] = LightSteelBlue,
        [nameof(LightYellow)] = LightYellow,
        [nameof(Lime)] = Lime,
        [nameof(LimeGreen)] = LimeGreen,
        [nameof(Linen)] = Linen,
        [nameof(Magenta)] = Magenta,
        [nameof(Maroon)] = Maroon,
        [nameof(MediumAquamarine)] = MediumAquamarine,
        [nameof(MediumBlue)] = MediumBlue,
        [nameof(MediumOrchid)] = MediumOrchid,
        [nameof(MediumPurple)] = MediumPurple,
        [nameof(MediumSeaGreen)] = MediumSeaGreen,
        [nameof(MediumSlateBlue)] = MediumSlateBlue,
        [nameof(MediumSpringGreen)] = MediumSpringGreen,
        [nameof(MediumTurquoise)] = MediumTurquoise,
        [nameof(MediumVioletRed)] = MediumVioletRed,
        [nameof(MidnightBlue)] = MidnightBlue,
        [nameof(MintCream)] = MintCream,
        [nameof(MistyRose)] = MistyRose,
        [nameof(Moccasin)] = Moccasin,
        [nameof(NavajoWhite)] = NavajoWhite,
        [nameof(Navy)] = Navy,
        [nameof(OldLace)] = OldLace,
        [nameof(Olive)] = Olive,
        [nameof(OliveDrab)] = OliveDrab,
        [nameof(Orange)] = Orange,
        [nameof(OrangeRed)] = OrangeRed,
        [nameof(Orchid)] = Orchid,
        [nameof(PaleGoldenrod)] = PaleGoldenrod,
        [nameof(PaleGreen)] = PaleGreen,
        [nameof(PaleTurquoise)] = PaleTurquoise,
        [nameof(PaleVioletRed)] = PaleVioletRed,
        [nameof(PapayaWhip)] = PapayaWhip,
        [nameof(PeachPuff)] = PeachPuff,
        [nameof(Peru)] = Peru,
        [nameof(Pink)] = Pink,
        [nameof(Plum)] = Plum,
        [nameof(PowderBlue)] = PowderBlue,
        [nameof(Purple)] = Purple,
        [nameof(Red)] = Red,
        [nameof(RosyBrown)] = RosyBrown,
        [nameof(RoyalBlue)] = RoyalBlue,
        [nameof(SaddleBrown)] = SaddleBrown,
        [nameof(Salmon)] = Salmon,
        [nameof(SandyBrown)] = SandyBrown,
        [nameof(SeaGreen)] = SeaGreen,
        [nameof(SeaShell)] = SeaShell,
        [nameof(Sienna)] = Sienna,
        [nameof(Silver)] = Silver,
        [nameof(SkyBlue)] = SkyBlue,
        [nameof(SlateBlue)] = SlateBlue,
        [nameof(SlateGray)] = SlateGray,
        [nameof(Snow)] = Snow,
        [nameof(SpringGreen)] = SpringGreen,
        [nameof(SteelBlue)] = SteelBlue,
        [nameof(Tan)] = Tan,
        [nameof(Teal)] = Teal,
        [nameof(Thistle)] = Thistle,
        [nameof(Tomato)] = Tomato,
        [nameof(Turquoise)] = Turquoise,
        [nameof(Violet)] = Violet,
        [nameof(Wheat)] = Wheat,
        [nameof(White)] = White,
        [nameof(WhiteSmoke)] = WhiteSmoke,
        [nameof(Yellow)] = Yellow,
        [nameof(YellowGreen)] = YellowGreen,
    };

    private static bool TryParseByte(string value, out byte parsed)
    {
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }
}
