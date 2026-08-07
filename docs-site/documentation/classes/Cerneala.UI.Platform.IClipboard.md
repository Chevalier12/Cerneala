# IClipboard Interface
## Definition
Namespace: `Cerneala.UI.Platform`
Assembly/Project: `Cerneala`
Source: `UI/Platform/IClipboard.cs`
Provides the `Cerneala.UI.Platform.IClipboard` API surface.
```csharp
public interface IClipboard
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasText` | `bool` | Gets whether the clipboard currently contains text. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetText()` | `string?` | Gets the current clipboard text, or `null` when no text is available. |
| `SetText(string text)` | `void` | Replaces the clipboard text. |
## Remarks
This page is generated from the repository API index so the documentation surface stays aligned with the source tree.
## Applies to
Cerneala UI runtime and framework API consumers.
