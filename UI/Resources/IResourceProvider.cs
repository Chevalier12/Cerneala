namespace Cerneala.UI.Resources;

public interface IResourceProvider
{
    bool TryGetResource<T>(ResourceId<T> id, out T resource);

    T GetResource<T>(ResourceId<T> id)
    {
        return TryGetResource(id, out T? resource)
            ? resource
            : throw new KeyNotFoundException($"Resource '{id}' was not found.");
    }
}
