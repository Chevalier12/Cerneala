namespace Cerneala.UI.Relay;

internal interface IUiThreadAccess
{
    bool CheckAccess();

    void VerifyAccess();
}
