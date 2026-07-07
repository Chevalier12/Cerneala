namespace Cerneala.UI.Motion;

public sealed class MotionStateBuilder
{
    internal MotionStateBuilder(MotionElementFacade facade)
    {
        Facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    internal MotionElementFacade Facade { get; }
}
