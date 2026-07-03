using Cerneala.UI.Accessibility;

namespace Cerneala.UI.Platform;

public interface IAccessibilityPlatform
{
    void Publish(SemanticsTree tree);
}
