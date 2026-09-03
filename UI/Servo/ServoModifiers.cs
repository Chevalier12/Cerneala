namespace Cerneala.UI.Servo;

[Flags]
public enum ServoModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2
}
