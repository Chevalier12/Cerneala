# Window<TViewModel> Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Window.cs`

Window base class with a strongly typed `DataContext` accessor.

```csharp
public class Window<TViewModel> : Window where TViewModel : class
```

## Examples
```csharp
public sealed class MainWindow : Window<AppViewModel>
{
    public string Heading => ViewModel.Heading;
}
```

## Remarks
The protected `ViewModel` property validates `DataContext` at access time and throws a descriptive `InvalidOperationException` on a type mismatch.

## Properties
| Name | Description |
| --- | --- |
| `ViewModel` | Protected typed view model resolved from `DataContext`. |

## Applies to
Typed Windows desktop views.
