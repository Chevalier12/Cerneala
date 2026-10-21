# IPlatformServices Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IPlatformServices.cs`

Groups the optional platform services that a UI root can use.

```csharp
public interface IPlatformServices
```

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Platform;

UIRoot root = new(200, 200);
Console.WriteLine(root.PlatformServices == PlatformServices.Empty);
// True

root.SetPlatformServices(new PlatformServices(Dpi: new FixedDpi()));
Console.WriteLine(root.PlatformServices.Dpi?.Scale);
// 2

root.SetPlatformServices(null);
Console.WriteLine(root.PlatformServices == PlatformServices.Empty);
// True

sealed class FixedDpi : IDpiProvider
{
    public float Scale => 2;

    public float DpiX => 192;

    public float DpiY => 192;
}
```

## Remarks

Every member may be `null`, which means the platform does not offer that service. `PlatformServices` is the built-in implementation, and `PlatformServices.Empty` has every member `null`.

A `UIRoot` starts with `PlatformServices.Empty`. `UIRoot.SetPlatformServices` replaces the set; passing `null` installs `PlatformServices.Empty`. It must be called on the root's UI thread, otherwise it throws `InvalidOperationException`. It does not dispose the previous set. When the new set has a `ReducedMotion` source, the root applies its mode; when it has none, the current reduced-motion mode is kept.

`UiHost` installs `UiHostOptions.PlatformServices` on its root when it is created and when its root is replaced. A window application installs the services of its windowing platform on each window root. The SDL platform supplies `Clipboard` (the system clipboard), `Cursor`, and `TextInput`, and leaves the other members `null`.

Which Cerneala code reads each member:

| Member | Read by |
| --- | --- |
| `Clipboard` | `TextBox` and `PasswordBox` clipboard shortcuts. |
| `Cursor` | `UiHost`, once per frame. |
| `TextInput` | `TextBox` and `PasswordBox`, only for `ITextInputPlatform.Clipboard`. |
| `ReducedMotion` | `UIRoot.SetPlatformServices`, when the set is installed. |
| `FileDialogs`, `Dpi`, `Accessibility` | No Cerneala code calls them; application code can. |

`Detective` reports whether each service except `ReducedMotion` is present.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clipboard` | `IClipboard?` | Gets the clipboard service. |
| `Cursor` | `ICursorService?` | Gets the cursor service. |
| `FileDialogs` | `IFileDialogService?` | Gets the file dialog service. |
| `TextInput` | `ITextInputPlatform?` | Gets the text input service. |
| `Dpi` | `IDpiProvider?` | Gets the DPI provider. |
| `Accessibility` | `IAccessibilityPlatform?` | Gets the accessibility publisher. |
| `ReducedMotion` | `IReducedMotionSource?` | Gets the reduced-motion preference source. |

## Applies to

`UIRoot.PlatformServices`, `UIRoot.SetPlatformServices(IPlatformServices?)`, and `UiHostOptions.PlatformServices`.

## See also

- `Cerneala.UI.Platform.PlatformServices`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Hosting.UiHostOptions`
