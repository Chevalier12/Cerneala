namespace Cerneala.VisualStudio.Preview;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cerneala.Preview;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

internal sealed class CernealaPreviewSession : IDisposable
{
    private readonly IWpfTextView textView;
    private readonly ITextDocumentFactoryService documentFactory;
    private readonly CernealaPreviewHostClient client = new();
    private readonly DispatcherTimer debounceTimer;
    private readonly DispatcherTimer animationTimer;
    private readonly CancellationTokenSource lifetime = new();
    private readonly CernealaPreviewInputQueue inputQueue = new();
    private WriteableBitmap? frameBuffer;
    private long animationFrameStartedTimestamp;
    private bool requestInFlight;
    private bool renderPending;
    private bool animationActive;
    private bool disposed;

    public CernealaPreviewSession(
        IWpfTextView textView,
        ITextDocumentFactoryService documentFactory)
    {
        this.textView = textView;
        this.documentFactory = documentFactory;
        debounceTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(300),
            DispatcherPriority.Background,
            OnDebounce,
            textView.VisualElement.Dispatcher)
        {
            IsEnabled = false
        };
        animationTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(1000d / RefreshRateLimit),
            DispatcherPriority.Render,
            OnAnimationTick,
            textView.VisualElement.Dispatcher)
        {
            IsEnabled = false
        };

        textView.TextBuffer.Changed += OnBufferChanged;
        textView.Closed += OnTextViewClosed;
    }

    public event EventHandler? Changed;

    public PreviewViewMode Mode { get; private set; } = PreviewViewMode.Split;

    public PreviewSplitOrientation Orientation { get; private set; } = PreviewSplitOrientation.Horizontal;

    public int ViewportWidth { get; private set; } = 1200;

    public int ViewportHeight { get; private set; } = 700;

    public double ZoomFactor { get; private set; } = 1;

    public bool FitToSurface { get; private set; } = true;

    public int RefreshRateLimit { get; private set; } = 60;

    public double HorizontalExtent { get; private set; }

    public double VerticalExtent { get; private set; }

    public BitmapSource? Frame { get; private set; }

    public string Status { get; private set; } = "Live Preview ready";

    public string? Error { get; private set; }

    public bool IsLoading { get; private set; }

    public void Start() => QueueRender(immediate: true);

    public void SetMode(PreviewViewMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        if (mode == PreviewViewMode.Code)
        {
            StopAnimation();
            Status = "Live Preview paused";
        }
        else
        {
            QueueRender(immediate: true);
        }

        RaiseChanged();
    }

    public void SetOrientation(PreviewSplitOrientation orientation)
    {
        if (Orientation == orientation)
        {
            return;
        }

        Orientation = orientation;
        RaiseChanged();
    }

    public void SetViewportSize(int width, int height, bool immediate = false)
    {
        width = Math.Max(320, Math.Min(4096, width));
        height = Math.Max(180, Math.Min(4096, height));
        if (ViewportWidth == width && ViewportHeight == height)
        {
            if (immediate)
            {
                QueueRender(immediate: true);
            }
            return;
        }

        ViewportWidth = width;
        ViewportHeight = height;
        RaiseChanged();
        QueueRender(immediate);
    }

    public void SetZoom(double zoomFactor)
    {
        zoomFactor = Math.Max(0.125, Math.Min(8, zoomFactor));
        if (!FitToSurface && Math.Abs(ZoomFactor - zoomFactor) < 0.001)
        {
            return;
        }

        FitToSurface = false;
        ZoomFactor = zoomFactor;
        RaiseChanged();
    }

    public void SetFitToSurface()
    {
        if (FitToSurface)
        {
            RaiseChanged();
            return;
        }

        FitToSurface = true;
        RaiseChanged();
    }

    public void SetRefreshRateLimit(int framesPerSecond)
    {
        framesPerSecond = Math.Max(1, Math.Min(240, framesPerSecond));
        if (RefreshRateLimit == framesPerSecond)
        {
            return;
        }

        RefreshRateLimit = framesPerSecond;
        if (animationActive)
        {
            animationFrameStartedTimestamp = Stopwatch.GetTimestamp();
            animationTimer.Stop();
            ScheduleAnimationFrame();
        }
        RaiseChanged();
    }

    public void SetHorizontalExtent(double extent)
    {
        HorizontalExtent = extent;
        RaiseChanged();
    }

    public void SetVerticalExtent(double extent)
    {
        VerticalExtent = extent;
        RaiseChanged();
    }

    public void Refresh() => QueueRender(immediate: true);

    public void Click(double x, double y) => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.Click,
        X = x,
        Y = y
    });

    public void MovePointer(double x, double y) => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.PointerMove,
        X = x,
        Y = y
    });

    public void SetPointerButton(double x, double y, string button, bool isDown) =>
        QueueInput(new PreviewRequest
        {
            Kind = PreviewRequestKind.PointerButton,
            X = x,
            Y = y,
            Button = button,
            IsDown = isDown
        });

    public void ScrollPointer(double x, double y, int wheelDelta) => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.PointerWheel,
        X = x,
        Y = y,
        WheelDelta = wheelDelta
    });

    public void LeavePointer() => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.PointerLeave
    });

    public void SendText(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            QueueInput(new PreviewRequest
            {
                Kind = PreviewRequestKind.Text,
                Text = text
            });
        }
    }

    public void PressKey(string key, int modifiers) => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.Key,
        Key = key,
        Modifiers = modifiers
    });

    public void SetKeyState(string key, bool isDown) => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.KeyState,
        Key = key,
        IsDown = isDown
    });

    public void ResetInput() => QueueInput(new PreviewRequest
    {
        Kind = PreviewRequestKind.ResetInput
    });

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        debounceTimer.Stop();
        StopAnimation();
        inputQueue.Clear();
        lifetime.Cancel();
        lifetime.Dispose();
        textView.TextBuffer.Changed -= OnBufferChanged;
        textView.Closed -= OnTextViewClosed;
        client.Dispose();
    }

    private void OnBufferChanged(object sender, TextContentChangedEventArgs args)
    {
        if (Mode != PreviewViewMode.Code)
        {
            QueueRender(immediate: false);
        }
    }

    private void QueueRender(bool immediate)
    {
        if (disposed || Mode == PreviewViewMode.Code)
        {
            return;
        }

        renderPending = true;
        debounceTimer.Stop();
        if (immediate)
        {
            _ = RenderPendingAsync();
        }
        else
        {
            debounceTimer.Start();
            Status = "Waiting for edits...";
            RaiseChanged();
        }
    }

    private void OnDebounce(object? sender, EventArgs args)
    {
        debounceTimer.Stop();
        _ = RenderPendingAsync();
    }

    private async Task RenderPendingAsync()
    {
        if (requestInFlight || !renderPending || disposed)
        {
            return;
        }

        if (!documentFactory.TryGetTextDocument(textView.TextBuffer, out ITextDocument document))
        {
            ShowError("The .crn document path is unavailable to Live Preview.");
            return;
        }

        renderPending = false;
        bool hasRenderedFrame = Frame is not null;
        if (!hasRenderedFrame)
        {
            StopAnimation();
        }

        requestInFlight = true;
        if (!hasRenderedFrame)
        {
            IsLoading = true;
            Error = null;
            Status = "Compiling preview...";
            RaiseChanged();
        }

        try
        {
            PreviewResponse response = await client.RenderAsync(
                document.FilePath,
                textView.TextSnapshot.GetText(),
                ViewportWidth,
                ViewportHeight,
                lifetime.Token);
            ApplyResponse(response, updateStatus: !hasRenderedFrame);
            if (hasRenderedFrame)
            {
                RaiseChanged();
            }

            StartAnimation();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            CompleteRequest();
        }
    }

    private void OnAnimationTick(object? sender, EventArgs args)
    {
        animationTimer.Stop();
        if (animationActive)
        {
            CaptureAnimationFrameAsync().FileAndForget("Cerneala/LivePreviewAnimation");
        }
    }

    private async Task CaptureAnimationFrameAsync()
    {
        if (requestInFlight || !animationActive || disposed || Mode == PreviewViewMode.Code)
        {
            return;
        }

        animationFrameStartedTimestamp = Stopwatch.GetTimestamp();
        requestInFlight = true;
        try
        {
            PreviewResponse response = await client.CaptureAsync(lifetime.Token);
            ApplyResponse(response, updateStatus: false);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            CompleteRequest();
        }
    }

    private void QueueInput(PreviewRequest request)
    {
        if (disposed || Mode == PreviewViewMode.Code)
        {
            return;
        }

        inputQueue.Enqueue(request);
        if (!requestInFlight && !renderPending)
        {
            DrainInputAsync().FileAndForget("Cerneala/LivePreviewInput");
        }
    }

    private async Task DrainInputAsync()
    {
        if (requestInFlight || disposed || renderPending ||
            !inputQueue.TryDequeue(out PreviewRequest? request) || request == null)
        {
            return;
        }

        requestInFlight = true;
        try
        {
            PreviewResponse response = await client.SendInputAsync(request, lifetime.Token);
            if (response.Kind == PreviewResponseKind.Frame)
            {
                ApplyResponse(response, updateStatus: false);
            }
            else if (response.Kind == PreviewResponseKind.Error)
            {
                ShowError(response.Error);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            CompleteRequest();
        }
    }

    private void CompleteRequest()
    {
        requestInFlight = false;
        if (renderPending && !disposed)
        {
            _ = RenderPendingAsync();
        }
        else if (inputQueue.Count > 0 && !disposed)
        {
            DrainInputAsync().FileAndForget("Cerneala/LivePreviewInput");
        }
        else
        {
            ScheduleAnimationFrame();
        }
    }

    private void StartAnimation()
    {
        animationActive = true;
        animationFrameStartedTimestamp = Stopwatch.GetTimestamp();
        ScheduleAnimationFrame();
    }

    private void StopAnimation()
    {
        animationActive = false;
        animationTimer.Stop();
    }

    private void ScheduleAnimationFrame()
    {
        if (!animationActive || requestInFlight || renderPending ||
            inputQueue.Count > 0 || disposed || Mode == PreviewViewMode.Code)
        {
            return;
        }

        long now = Stopwatch.GetTimestamp();
        long intervalTicks = Math.Max(1, Stopwatch.Frequency / RefreshRateLimit);
        long remainingTicks = animationFrameStartedTimestamp + intervalTicks - now;
        if (remainingTicks <= 0)
        {
            CaptureAnimationFrameAsync().FileAndForget("Cerneala/LivePreviewAnimation");
            return;
        }

        animationTimer.Interval = TimeSpan.FromSeconds(
            remainingTicks / (double)Stopwatch.Frequency);
        animationTimer.Start();
    }

    private void ApplyResponse(PreviewResponse response, bool updateStatus = true)
    {
        if (response.Kind == PreviewResponseKind.Error)
        {
            ShowError(response.Error);
            return;
        }

        if (frameBuffer is null ||
            frameBuffer.PixelWidth != response.Width ||
            frameBuffer.PixelHeight != response.Height)
        {
            frameBuffer = new WriteableBitmap(
                response.Width,
                response.Height,
                96,
                96,
                PixelFormats.Bgra32,
                palette: null);
        }

        frameBuffer.WritePixels(
            new Int32Rect(0, 0, response.Width, response.Height),
            response.Image,
            response.Stride,
            0);
        Frame = frameBuffer;
        IsLoading = false;
        Error = null;
        if (updateStatus)
        {
            Status = response.CompileMilliseconds > 0
                ? $"Ready  |  compile {response.CompileMilliseconds:F0} ms  |  render {response.RenderMilliseconds:F0} ms"
                : $"Ready  |  render {response.RenderMilliseconds:F0} ms";
            RaiseChanged();
        }
    }

    private void ShowError(string message)
    {
        StopAnimation();
        IsLoading = false;
        Error = message;
        Status = "Preview error";
        RaiseChanged();
    }

    private void OnTextViewClosed(object sender, EventArgs args) => Dispose();

    private void RaiseChanged()
    {
        if (!disposed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}

internal enum PreviewViewMode
{
    Design,
    Split,
    Code
}

internal enum PreviewSplitOrientation
{
    Horizontal,
    Vertical
}
