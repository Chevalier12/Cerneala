namespace Cerneala.UI.Platform;

public interface IDpiProvider
{
    float Scale { get; }

    float DpiX { get; }

    float DpiY { get; }
}
