namespace Cerneala.Drawing.Prism.Graph;

[Flags]
internal enum PrismGraphCapabilities
{
    None = 0,
    ControlCapture = 1 << 0,
    FilterProcessing = 1 << 1,
    StyleProcessing = 1 << 2,
    MaskProcessing = 1 << 3,
    GroupProcessing = 1 << 4,
    GroupIsolation = 1 << 5,
    Clipping = 1 << 6,
    AdvancedBlending = 1 << 7,
    ColorConversion = 1 << 8,
    BackdropInput = 1 << 9
}
