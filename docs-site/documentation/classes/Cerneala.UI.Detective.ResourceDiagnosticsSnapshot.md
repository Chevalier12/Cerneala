# ResourceDiagnosticsSnapshot Record

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Stores image-resource cache availability and its load count when a cache exists.

```csharp
public sealed record ResourceDiagnosticsSnapshot(
    bool HasImageCache,
    int? ImageCacheLoadCount);
```

## Examples

```csharp
ResourceDiagnosticsSnapshot resources = root.Detective.Capture(stats).Resources;
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasImageCache` | `bool` | Gets whether the root has an image resource cache. |
| `ImageCacheLoadCount` | `int?` | Gets the cache load count, or `null` when no cache exists. |
