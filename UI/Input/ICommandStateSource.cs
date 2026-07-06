namespace Cerneala.UI.Input;

public interface ICommandStateSource
{
    bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap);
}
