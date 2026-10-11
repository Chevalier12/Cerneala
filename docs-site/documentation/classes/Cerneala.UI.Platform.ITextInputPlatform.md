# ITextInputPlatform Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/ITextInputPlatform.cs`

Describes the text input features of a platform.

```csharp
public interface ITextInputPlatform
```

## Examples

```csharp
using Cerneala.UI.Platform;

ITextInputPlatform textInput = new BasicTextInput();
Console.WriteLine($"{textInput.SupportsIme} {textInput.Clipboard is null}");
// False True

sealed class BasicTextInput : ITextInputPlatform
{
    public bool SupportsIme => false;
}
```

## Remarks

`Clipboard` is a default interface member that returns `null`. `TextBox` and `PasswordBox` read it only when `IPlatformServices.Clipboard` of their root is `null`; see `IClipboard`.

No Cerneala code reads `SupportsIme`. The SDL platform supplies an implementation whose `SupportsIme` is `true` and whose `Clipboard` is the default `null`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clipboard` | `IClipboard?` | Gets a clipboard that text controls use when `IPlatformServices.Clipboard` is `null`. Returns `null` unless an implementation overrides it. |
| `SupportsIme` | `bool` | Gets whether the platform supports input method editor composition. |

## Applies to

`IPlatformServices.TextInput`.

## See also

- `Cerneala.UI.Platform.IClipboard`
- `Cerneala.UI.Platform.IPlatformServices`
