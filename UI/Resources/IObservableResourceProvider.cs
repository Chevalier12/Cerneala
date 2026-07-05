namespace Cerneala.UI.Resources;

public interface IObservableResourceProvider : IResourceProvider
{
    event EventHandler<ResourceChangedEventArgs>? ResourceChanged;
}
