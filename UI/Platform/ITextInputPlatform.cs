using Cerneala.UI.Text;

namespace Cerneala.UI.Platform;

public interface ITextInputPlatform
{
    ClipboardAdapter Clipboard { get; }

    bool SupportsIme { get; }
}
