namespace Cerneala.UI.Text;

public enum TextWrapping
{
    NoWrap = 0,
    Wrap = 1,
    // As Wrap, but a word that is wider than the line stays whole on its line and overflows, as in WPF.
    WrapWithOverflow = 2
}
