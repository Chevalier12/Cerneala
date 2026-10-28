# ResourceDictionary Class

## Definition
Namespace: `Cerneala.UI.Resources`  
Assembly/Project: `Cerneala`  
Source: `UI/Resources/ResourceDictionary.cs`

Stores keyed resources and publishes resource-change notifications to the UI tree.

```csharp
public sealed class ResourceDictionary : IObservableResourceProvider, IEnumerable<KeyValuePair<object, object?>>
```

## Examples
```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

TextBlock element = new();
element.Resources.Add("Accent", new SolidColorBrush(Color.CornflowerBlue));
SolidColorBrush accent = element.FindResource<SolidColorBrush>("Accent");
```

## Remarks
Keys cannot be `null`. Replacing or removing a resource increments the dictionary version and raises `ResourceChanged`; typed `ResourceId<T>` access preserves the requested resource type in `ResourceChangedEventArgs.ResourceType`.

The dictionary keeps one entry per key, whatever type a reader asks for. So every change, made through the indexer, `Add`, `SetResource<T>`, `Remove`, or `Clear`, reaches every element that read the key from this dictionary as its root provider, whatever type it read the key as. For example, an element that called `TryFindResource<Brush>("Accent", out _)` is invalidated by `dictionary["Accent"] = new SolidColorBrush(...)`, although that event's `ResourceType` is `SolidColorBrush`, and also by `dictionary.Remove("Accent")`.

For a dictionary owned by an attached `UIElement`, UI-thread notifications invalidate synchronously and off-thread notifications are posted to the element root's Relay in FIFO order. `ResourceDictionary` itself wraps a normal `Dictionary` and is not a concurrent collection; callers must not mutate and read it concurrently.

## Properties
| Name | Description |
| --- | --- |
| `Count` | Number of entries. |
| `Keys` | Current keys. |
| `Values` | Current values. |
| `this[object]` | Gets or replaces a resource by key. |

## Methods
| Name | Description |
| --- | --- |
| `Add` | Adds a new keyed resource. |
| `SetResource<T>` | Sets a typed resource by `ResourceId<T>`. |
| `ContainsKey`, `TryGetValue`, `TryGetResource<T>` | Queries resources. |
| `Remove`, `Clear` | Removes entries and raises change notifications. |

## Events
| Name | Description |
| --- | --- |
| `ResourceChanged` | Raised when a resource entry changes. |

## Applies to
UI resources, markup lookup, and resource dependency tracking.
