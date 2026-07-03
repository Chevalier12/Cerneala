namespace Cerneala.UI.Input;

public readonly record struct Cursor(string Name)
{
    public static Cursor Arrow { get; } = new("Arrow");

    public static Cursor Hand { get; } = new("Hand");

    public static Cursor IBeam { get; } = new("IBeam");

    public static Cursor Crosshair { get; } = new("Crosshair");
}
