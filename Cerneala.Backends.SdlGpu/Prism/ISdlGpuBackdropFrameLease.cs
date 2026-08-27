using Cerneala.Drawing.Prism;

namespace Cerneala.Backends.SdlGpu;

internal interface ISdlGpuBackdropFrameLease : IBackdropFrameLease
{
    nint Texture { get; }
}
