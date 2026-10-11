# IInputSource Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/IInputSource.cs`

Supplies one `InputFrame` of keyboard, pointer, and text input each time the host updates.

```csharp
public interface IInputSource
```

## Examples

A scripted source that replays pointer snapshots. Each call to `GetFrame` returns the change from the previous snapshot to the next one.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

ScriptedInput input = new();
UIRoot root = new(200, 200);
Button button = new() { Width = 80, Height = 30 };
int clicks = 0;
button.Click += (_, _) => clicks++;
root.VisualChildren.Add(button);
UiHost host = new(new UiHostOptions { Root = root, InputSource = input, Viewport = new UiViewport(200, 200) });

PointerSnapshot over = PointerSnapshot.Empty.WithPosition(100, 100);
input.Enqueue(over);
input.Enqueue(over.WithButton(InputMouseButton.Left, true));
input.Enqueue(over);
host.Update();
host.Update();
host.Update();
Console.WriteLine(clicks); // 1

sealed class ScriptedInput : IInputSource
{
    private readonly Queue<PointerSnapshot> pending = new();
    private PointerSnapshot previous = PointerSnapshot.Empty;

    public void Enqueue(PointerSnapshot snapshot) => pending.Enqueue(snapshot);

    public InputFrame GetFrame()
    {
        PointerSnapshot current = pending.Count > 0 ? pending.Dequeue() : previous;
        InputFrame frame = new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
        previous = current;
        return frame;
    }
}
```

## Remarks

`UiHost.Update(UiViewport?, TimeSpan?)` calls `GetFrame` once and processes the returned frame. It uses `UiHost.InputSource` (set directly or through `UiHostOptions.InputSource`) and falls back to `IUiBackend.InputSource` of `UiHost.Backend`. When both are `null`, it throws `InvalidOperationException`. `UiHost.Update(InputFrame, ...)` takes the frame directly and does not call `GetFrame`.

An `InputFrame` holds the previous and current snapshots. "Pressed" and "released" are computed by comparing the two, so a source must return each change exactly once. Return the same snapshot for both sides when nothing changed. Treat `GetFrame` as consuming: on every call, the built-in SDL source clears its queued text input and moves its previous snapshots forward. A second call right after the first returns no presses, releases, or text.

Windows created through `Application` get their input from the platform window's source. The SDL source is internal; applications do not create it.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetFrame()` | `InputFrame` | Returns the input that changed since the previous call. |

## Applies to

`UiHost`, `UiHostOptions`, and `IUiBackend`.

## See also

- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.PointerSnapshot`
- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.UiHostOptions`
