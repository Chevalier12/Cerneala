using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed class ControlTextFont : IDrawFont
{
    public ControlTextFont(string familyName, float size)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family cannot be empty.", nameof(familyName));
        }

        if (size <= 0 || !float.IsFinite(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Font size must be positive and finite.");
        }

        FamilyName = familyName;
        Size = size;
    }

    public string FamilyName { get; }

    public float Size { get; }
}
