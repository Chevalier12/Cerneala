using Cerneala.Preview;
using Cerneala.UI.Automation;
using Cerneala.UI.Input;

namespace Cerneala.PreviewHost;

internal sealed class PreviewHostServer : IDisposable
{
    private readonly PreviewCompiler compiler = new();
    private PreviewRenderSession? session;
    private string? activeDocumentPath;
    private string? activeSource;
    private int activeWidth;
    private int activeHeight;

    public void Run(Stream input, Stream output)
    {
        while (PreviewProtocol.ReadRequest(input) is { } request)
        {
            if (request.Kind == PreviewRequestKind.Shutdown)
            {
                return;
            }

            try
            {
                PreviewResponse response = Handle(request);
                PreviewProtocol.WriteResponse(output, response);
            }
            catch (Exception exception)
            {
                PreviewProtocol.WriteResponse(output, new PreviewResponse
                {
                    Kind = PreviewResponseKind.Error,
                    RequestId = request.RequestId,
                    Error = exception.Message
                });
            }
        }
    }

    public void Dispose()
    {
        session?.Dispose();
        session = null;
        activeDocumentPath = null;
        activeSource = null;
        compiler.Dispose();
    }

    private PreviewResponse Handle(PreviewRequest request)
    {
        TimeSpan compileTime = TimeSpan.Zero;
        if (request.Kind == PreviewRequestKind.Render)
        {
            bool canUpdateLiveTree = session is not null &&
                activeSource is not null &&
                string.Equals(activeDocumentPath, request.DocumentPath, StringComparison.OrdinalIgnoreCase) &&
                activeWidth == request.Width &&
                activeHeight == request.Height;
            PreviewMarkupUpdateResult update = canUpdateLiveTree
                ? session!.TryApplyMarkup(activeSource!, request.SourceText)
                : PreviewMarkupUpdateResult.RequiresCompilation;

            if (update is PreviewMarkupUpdateResult.Applied or PreviewMarkupUpdateResult.Unchanged)
            {
                activeSource = request.SourceText;
            }
            else if (update == PreviewMarkupUpdateResult.RequiresCompilation)
            {
                PreviewCompilation compilation = compiler
                    .CompileAsync(request.DocumentPath, request.SourceText)
                    .GetAwaiter()
                    .GetResult();
                compileTime = compilation.CompileTime;
                PreviewRenderSession replacement = PreviewRenderSession.Create(
                    compilation,
                    request.Width,
                    request.Height);
                PreviewRenderSession? previous = session;
                session = replacement;
                activeDocumentPath = request.DocumentPath;
                activeSource = request.SourceText;
                activeWidth = request.Width;
                activeHeight = request.Height;
                previous?.Dispose();
            }
        }
        else
        {
            PreviewRenderSession active = session
                ?? throw new InvalidOperationException("The preview has not rendered a document yet.");
            switch (request.Kind)
            {
                case PreviewRequestKind.Click:
                    active.Click((float)request.X, (float)request.Y);
                    break;
                case PreviewRequestKind.Text:
                    active.SendText(request.Text);
                    break;
                case PreviewRequestKind.Key:
                    if (!Enum.TryParse(request.Key, ignoreCase: false, out InputKey key))
                    {
                        throw new InvalidOperationException($"Preview key '{request.Key}' is not supported.");
                    }

                    active.PressKey(key, (AutomationModifiers)request.Modifiers);
                    break;
                case PreviewRequestKind.PointerMove:
                    active.MovePointer((float)request.X, (float)request.Y);
                    return Acknowledge(request);
                case PreviewRequestKind.PointerButton:
                    if (!Enum.TryParse(request.Button, ignoreCase: false, out InputMouseButton button))
                    {
                        throw new InvalidOperationException($"Preview mouse button '{request.Button}' is not supported.");
                    }

                    active.SetPointerButton((float)request.X, (float)request.Y, button, request.IsDown);
                    return Acknowledge(request);
                case PreviewRequestKind.PointerWheel:
                    active.ScrollPointer((float)request.X, (float)request.Y, request.WheelDelta);
                    return Acknowledge(request);
                case PreviewRequestKind.PointerLeave:
                    active.LeavePointer();
                    return Acknowledge(request);
                case PreviewRequestKind.KeyState:
                    if (!Enum.TryParse(request.Key, ignoreCase: false, out InputKey stateKey))
                    {
                        throw new InvalidOperationException($"Preview key '{request.Key}' is not supported.");
                    }

                    active.SetKeyState(stateKey, request.IsDown);
                    return Acknowledge(request);
                case PreviewRequestKind.ResetInput:
                    active.ResetInput();
                    return Acknowledge(request);
            }
        }

        (byte[] image, int width, int height, int stride, TimeSpan renderTime) = session!.Capture();
        return new PreviewResponse
        {
            Kind = PreviewResponseKind.Frame,
            RequestId = request.RequestId,
            Image = image,
            Width = width,
            Height = height,
            Stride = stride,
            CompileMilliseconds = compileTime.TotalMilliseconds,
            RenderMilliseconds = renderTime.TotalMilliseconds
        };
    }

    private static PreviewResponse Acknowledge(PreviewRequest request)
    {
        return new PreviewResponse
        {
            Kind = PreviewResponseKind.Acknowledged,
            RequestId = request.RequestId
        };
    }
}
