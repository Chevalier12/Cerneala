# GeneratedMarkup Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Factory methods used by source-generated markup to observe properties, data paths, objects, and template parts.

```csharp
public static class GeneratedMarkup
```

## Examples
```csharp
MarkupObservation observation = GeneratedMarkup.ObserveProperty(element, UIElement.IsVisibleProperty);
using IDisposable lifetime = GeneratedMarkup.AttachConditions(element, [observation], []);
```

## Methods
| Name | Description |
| --- | --- |
| `ObserveProperty` | Observes a `UiObject` property. |
| `ObserveTemplatePartProperty` | Observes a property on a named component-template part. |
| `ObserveObject` | Observes a getter-backed object value. |
| `ObserveDataPath` | Observes a `DataContext` property path. |
| `AttachConditions` | Attaches observations and rules to an element lifecycle. |

## Remarks
Returned observations are lifecycle-managed by the attached controller. The generated path and template observers reconnect when their source changes.

## Applies to
Source-generated reactive markup.
