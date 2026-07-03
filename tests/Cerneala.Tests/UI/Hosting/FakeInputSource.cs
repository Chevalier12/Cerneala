using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Hosting;

internal sealed class FakeInputSource : IInputSource
{
    public int GetFrameCalls { get; private set; }

    public InputFrame NextFrame { get; set; } = CreateFrame();

    public InputFrame GetFrame()
    {
        GetFrameCalls++;
        return NextFrame;
    }

    public static InputFrame CreateFrame(float x = 0, float y = 0)
    {
        PointerSnapshot previousPointer = PointerSnapshot.Empty;
        PointerSnapshot currentPointer = PointerSnapshot.Empty.WithPosition(x, y);
        KeyboardSnapshot keyboard = KeyboardSnapshot.Empty;
        return new InputFrame(previousPointer, currentPointer, keyboard, keyboard, []);
    }
}
