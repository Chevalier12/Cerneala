namespace Cerneala.Drawing;

public interface IFontSource
{
    IDrawFont LoadFont(string familyName, float size);

    // A source that has no weight, style or stretch faces returns the face of LoadFont(familyName, size).
    IDrawFont LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch) =>
        LoadFont(familyName, size);
}
