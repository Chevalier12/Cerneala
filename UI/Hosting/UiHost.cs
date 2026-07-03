using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Hosting;

public sealed class UiHost
{
    private UIRoot? root;
    private UiViewport viewport;
    private bool needsInitialFrame = true;

    public UiHost(UiHostOptions? options = null)
    {
        options ??= new UiHostOptions();
        root = options.Root;
        viewport = options.Viewport;
        InputSource = options.InputSource;
        Backend = options.Backend;
        Clock = options.Clock;
        InputBridge = options.InputBridge ?? new ElementInputBridge();

        if (root is not null)
        {
            ApplyViewport(root, viewport);
        }
    }

    public UIRoot? Root => root;

    public IInputSource? InputSource { get; set; }

    public IUiBackend? Backend { get; set; }

    public IUiClock? Clock { get; set; }

    public ElementInputBridge InputBridge { get; }

    public UiViewport Viewport => viewport;

    public UiFrame? LastFrame { get; private set; }

    public void SetRoot(UIRoot newRoot)
    {
        root = newRoot ?? throw new ArgumentNullException(nameof(newRoot));
        needsInitialFrame = true;
        ApplyViewport(root, viewport);
    }

    public UiFrame Update(UiViewport? viewport = null, TimeSpan? elapsedTime = null)
    {
        _ = RequireRoot();
        IInputSource inputSource = InputSource ?? Backend?.InputSource ?? throw new InvalidOperationException("UiHost requires an input source for Update without an explicit input frame.");
        return Update(inputSource.GetFrame(), viewport, elapsedTime);
    }

    public UiFrame Update(InputFrame inputFrame, UiViewport? viewport = null, TimeSpan? elapsedTime = null)
    {
        ArgumentNullException.ThrowIfNull(inputFrame);

        UIRoot currentRoot = RequireRoot();
        UiViewport currentViewport = viewport ?? this.viewport;
        ApplyViewportIfChanged(currentRoot, currentViewport);
        PrimeInitialFrame(currentRoot);
        InputBridge.Dispatch(currentRoot, inputFrame);

        FrameStats stats = currentRoot.ProcessFrame();
        currentRoot.RetainedRenderer.Render(currentRoot);
        LastFrame = new UiFrame(elapsedTime ?? Clock?.GetElapsedTime() ?? TimeSpan.Zero, this.viewport, inputFrame, stats);
        return LastFrame;
    }

    public void Draw()
    {
        _ = RequireRoot();
        IDrawingBackend backend = Backend?.DrawingBackend ?? throw new InvalidOperationException("UiHost requires a drawing backend for Draw without an explicit backend.");
        Draw(backend);
    }

    public void Draw(IDrawingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        UIRoot currentRoot = RequireRoot();
        currentRoot.RetainedRenderer.Submit(currentRoot, backend);
    }

    private UIRoot RequireRoot()
    {
        return root ?? throw new InvalidOperationException("UiHost requires a retained root.");
    }

    private void ApplyViewportIfChanged(UIRoot currentRoot, UiViewport nextViewport)
    {
        if (nextViewport == viewport)
        {
            return;
        }

        viewport = nextViewport;
        ApplyViewport(currentRoot, viewport);
        currentRoot.Invalidate(InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Viewport changed");
    }

    private static void ApplyViewport(UIRoot root, UiViewport viewport)
    {
        root.SetViewport(viewport.Width, viewport.Height, viewport.Scale);
    }

    private void PrimeInitialFrame(UIRoot currentRoot)
    {
        if (!needsInitialFrame)
        {
            return;
        }

        currentRoot.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Initial host frame");
        needsInitialFrame = false;
    }
}
