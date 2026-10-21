namespace Cerneala.UI.Text;

public enum LineStackingStrategy
{
    // A line is as high as the larger of its content and LineHeight.
    MaxHeight = 0,
    // A line is exactly LineHeight high.
    BlockLineHeight = 1
}
