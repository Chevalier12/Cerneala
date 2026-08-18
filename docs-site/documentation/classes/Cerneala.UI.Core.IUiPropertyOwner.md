# IUiPropertyOwner Interface
## Definition
Namespace: `Cerneala.UI.Core`
Assembly/Project: `Cerneala`
Source: `UI/Core/IUiPropertyOwner.cs`

Defines the callback used by a UI object to report invalidation caused by an effective UI property change.
```csharp
public interface IUiPropertyOwner
```

## Examples

```csharp
public sealed class ElementOwner : UiObject, IUiPropertyOwner
{
    public void OnPropertyInvalidated(
        UiPropertyChangedEventArgs args,
        UiPropertyOptions options)
    {
        // Schedule the subsystems represented by options for this element.
    }
}
```

## Remarks
`UiObject` calls `OnPropertyInvalidated` only when an effective value changes, the property's metadata contains one or more invalidation flags, and the object implements this interface. The `options` argument contains the relevant flags from `UiPropertyOptions`; it is not necessarily the complete metadata value.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options)` | `void` | Receives the changed property event data and the invalidation flags selected for the change. |

## Applies to
Cerneala UI runtime and framework API consumers.
