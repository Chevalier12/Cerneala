namespace Cerneala.Drawing;

public interface IFontSource
{
    IDrawFont LoadFont(string familyName, float size);
}
