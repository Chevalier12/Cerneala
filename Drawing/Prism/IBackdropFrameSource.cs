namespace Cerneala.Drawing.Prism;

public interface IBackdropFrameSource
{
    bool IsCompatibleWith(IDrawingBackend drawingBackend);

    IBackdropFrameLease AcquireFrame(in BackdropFrameRequest request);
}
