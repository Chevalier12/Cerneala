# UserControl<TViewModel> Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/UserControl.cs`

User-control base class with a strongly typed `DataContext` accessor.

```csharp
public class UserControl<TViewModel> : UserControl where TViewModel : class
```

## Examples
```csharp
public sealed class SettingsView : UserControl<SettingsViewModel>
{
    public string Title => ViewModel.Title;
}
```

## Remarks
The protected `ViewModel` property throws `InvalidOperationException` when `DataContext` is not assignable to `TViewModel`, making an incorrect generated or runtime data context explicit.

## Properties
| Name | Description |
| --- | --- |
| `ViewModel` | Protected typed view model resolved from `DataContext`. |

## Applies to
Typed user controls.
