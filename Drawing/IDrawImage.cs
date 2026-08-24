namespace Cerneala.Drawing;

public interface IDrawImage
{
    int Width { get; }

    int Height { get; }
}

internal interface IDrawImageInvalidationSource
{
    event EventHandler? ContentChanged;
}
