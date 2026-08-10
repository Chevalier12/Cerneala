using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Automation;

public interface IAutomationInputDriver
{
    void Click(UIElement target);

    void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None);

    void SendText(string text);
}
