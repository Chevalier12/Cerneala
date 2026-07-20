namespace Cerneala.Drawing.Prism;

public interface IBackdropFrameLease : IDisposable
{
    BackdropFrameMetadata Metadata { get; }
}
