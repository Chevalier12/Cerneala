using Microsoft.Xna.Framework.Input;

namespace Cerneala.UI.Input.MonoGame;

public static class MonoGameInputMapper
{
    public static InputKey MapKey(Keys key)
    {
        return key switch
        {
            Keys.Back => InputKey.Back,
            Keys.Tab => InputKey.Tab,
            Keys.Enter => InputKey.Enter,
            Keys.Escape => InputKey.Escape,
            Keys.Space => InputKey.Space,
            Keys.PageUp => InputKey.PageUp,
            Keys.PageDown => InputKey.PageDown,
            Keys.End => InputKey.End,
            Keys.Home => InputKey.Home,
            Keys.Left => InputKey.Left,
            Keys.Up => InputKey.Up,
            Keys.Right => InputKey.Right,
            Keys.Down => InputKey.Down,
            Keys.Insert => InputKey.Insert,
            Keys.Delete => InputKey.Delete,
            Keys.D0 => InputKey.D0,
            Keys.D1 => InputKey.D1,
            Keys.D2 => InputKey.D2,
            Keys.D3 => InputKey.D3,
            Keys.D4 => InputKey.D4,
            Keys.D5 => InputKey.D5,
            Keys.D6 => InputKey.D6,
            Keys.D7 => InputKey.D7,
            Keys.D8 => InputKey.D8,
            Keys.D9 => InputKey.D9,
            Keys.A => InputKey.A,
            Keys.B => InputKey.B,
            Keys.C => InputKey.C,
            Keys.D => InputKey.D,
            Keys.E => InputKey.E,
            Keys.F => InputKey.F,
            Keys.G => InputKey.G,
            Keys.H => InputKey.H,
            Keys.I => InputKey.I,
            Keys.J => InputKey.J,
            Keys.K => InputKey.K,
            Keys.L => InputKey.L,
            Keys.M => InputKey.M,
            Keys.N => InputKey.N,
            Keys.O => InputKey.O,
            Keys.P => InputKey.P,
            Keys.Q => InputKey.Q,
            Keys.R => InputKey.R,
            Keys.S => InputKey.S,
            Keys.T => InputKey.T,
            Keys.U => InputKey.U,
            Keys.V => InputKey.V,
            Keys.W => InputKey.W,
            Keys.X => InputKey.X,
            Keys.Y => InputKey.Y,
            Keys.Z => InputKey.Z,
            Keys.LeftShift => InputKey.LeftShift,
            Keys.RightShift => InputKey.RightShift,
            Keys.LeftControl => InputKey.LeftCtrl,
            Keys.RightControl => InputKey.RightCtrl,
            Keys.LeftAlt => InputKey.LeftAlt,
            Keys.RightAlt => InputKey.RightAlt,
            Keys.F1 => InputKey.F1,
            Keys.F2 => InputKey.F2,
            Keys.F3 => InputKey.F3,
            Keys.F4 => InputKey.F4,
            Keys.F5 => InputKey.F5,
            Keys.F6 => InputKey.F6,
            Keys.F7 => InputKey.F7,
            Keys.F8 => InputKey.F8,
            Keys.F9 => InputKey.F9,
            Keys.F10 => InputKey.F10,
            Keys.F11 => InputKey.F11,
            Keys.F12 => InputKey.F12,
            _ => InputKey.Unknown
        };
    }

    public static InputMouseButton MapMouseButton(int buttonIndex)
    {
        return buttonIndex switch
        {
            0 => InputMouseButton.Left,
            1 => InputMouseButton.Middle,
            2 => InputMouseButton.Right,
            3 => InputMouseButton.XButton1,
            4 => InputMouseButton.XButton2,
            _ => InputMouseButton.None
        };
    }
}
