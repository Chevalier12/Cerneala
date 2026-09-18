namespace Cerneala.UI.Input;

// Live readiness can change after a route snapshot was built (for example a
// worker-published spatial catalog). Guard both fresh hits and retained targets.
internal interface IInputRouteGuard
{
    bool IsInputRouteAvailable { get; }
}
