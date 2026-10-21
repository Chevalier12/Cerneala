# IClipboard Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IClipboard.cs`

Reads and writes plain text on a clipboard.

```csharp
public interface IClipboard
```

## Examples

An in-memory clipboard, installed on a root so that `TextBox` copy, cut, and paste use it:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Platform;

MemoryClipboard clipboard = new();
UIRoot root = new(200, 200);
root.SetPlatformServices(new PlatformServices(Clipboard: clipboard));

clipboard.SetText("hello");
Console.WriteLine($"{clipboard.HasText} {clipboard.GetText()}");
// True hello

sealed class MemoryClipboard : IClipboard
{
    private string? text;

    public bool HasText => !string.IsNullOrEmpty(text);

    public string? GetText() => text;

    public void SetText(string text) => this.text = text;
}
```

## Remarks

`TextBox` and `PasswordBox` use the clipboard for keyboard shortcuts while Control is held and Alt is not:

| Shortcut | Control | Clipboard call |
| --- | --- | --- |
| Control+C | `TextBox` | `SetText` with the selected text. Nothing happens when the selection is empty. |
| Control+X | `TextBox`, when editing is allowed | `SetText` with the selected text, then the selection is deleted. |
| Control+V | `TextBox` and `PasswordBox`, when editing is allowed | Reads `GetText` only when `HasText` is `true`. A `null` result is treated as an empty string, and an empty result inserts nothing. |

`PasswordBox` never calls `SetText`: Control+C and Control+X are handled without touching the clipboard.

The controls look for the clipboard in `IPlatformServices.Clipboard` of their root first, then in `IPlatformServices.TextInput.Clipboard`. When neither is set, the shortcuts do nothing. The SDL platform that `Cerneala.Backends.SdlGpu` registers sets `IPlatformServices.Clipboard` to the system clipboard (`SDL_SetClipboardText`, `SDL_GetClipboardText`, `SDL_HasClipboardText`), so in an SDL window these shortcuts work without application code. `HasText` is `false` when the system clipboard holds no text or an empty text, and `GetText` then returns `null`. `SetText` does nothing and does not throw when SDL refuses the write, for example while another program holds the system clipboard open, so a copy in that moment leaves the clipboard and the control unchanged. On Windows, SDL returns line breaks as `\r\n`: after `SetText("a\nb")`, `GetText()` returns `"a\r\nb"`. An application that calls `UIRoot.SetPlatformServices` with its own `IPlatformServices` replaces the whole set of that root, including the SDL clipboard, cursor, and text input.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasText` | `bool` | Gets whether the clipboard holds text that `GetText` can return. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetText()` | `string?` | Returns the clipboard text, or `null` when there is none. |
| `SetText(string text)` | `void` | Replaces the clipboard contents with `text`. |

## Applies to

`IPlatformServices.Clipboard` and `ITextInputPlatform.Clipboard`.

## See also

- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Platform.ITextInputPlatform`
- `Cerneala.UI.Controls.TextBox`
- `Cerneala.UI.Controls.PasswordBox`
