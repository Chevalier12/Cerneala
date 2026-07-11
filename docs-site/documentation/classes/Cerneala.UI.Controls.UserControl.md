# UserControl Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/UserControl.cs`

Base control for author-defined reusable UI components.

```csharp
public class UserControl : Control
```

## Examples
```csharp
public sealed class SettingsView : UserControl
{
}
```

## Remarks
`UserControl` adds the semantic boundary used by generated `.cui.xml` user controls while retaining normal `Control` layout, resources, and rendering behavior.

## Applies to
Reusable application controls and source-generated user controls.
