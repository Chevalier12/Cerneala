namespace Cerneala.UI.Input;

public interface IInputCommandSource
{
    bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap);
}
