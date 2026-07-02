namespace Cerneala.UI.Core;

public interface IUiPropertyOwner
{
    void OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options);
}
