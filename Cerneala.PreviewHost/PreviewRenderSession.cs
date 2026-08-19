using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Cerneala.UI;
using Cerneala.UI.Automation;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;

namespace Cerneala.PreviewHost;

internal sealed class PreviewRenderSession : IDisposable
{
    internal const float RenderScale = 0.9f;

    private readonly PreviewLoadContext loadContext;
    private readonly DesignPreviewSession session;
    private readonly UIElement root;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private byte[]? captureBuffer;
    private TimeSpan previousPump;
    private bool disposed;

    private PreviewRenderSession(
        PreviewLoadContext loadContext,
        DesignPreviewSession session,
        UIElement root)
    {
        this.loadContext = loadContext;
        this.session = session;
        this.root = root;
    }

    public static PreviewRenderSession Create(PreviewCompilation compilation, int width, int height)
    {
        Environment.CurrentDirectory = compilation.ProjectDirectory;
        PreviewLoadContext loadContext = new(compilation.ReferencePaths);
        try
        {
            using MemoryStream image = new(compilation.AssemblyImage, writable: false);
            Assembly assembly = loadContext.LoadFromStream(image);
            Type targetType = assembly.GetType(compilation.TargetTypeName, throwOnError: false, ignoreCase: false)
                ?? assembly.GetTypes().FirstOrDefault(type =>
                    type.Name == compilation.TargetTypeName.Split('.').Last() &&
                    typeof(UIElement).IsAssignableFrom(type))
                ?? throw new InvalidOperationException($"Preview type '{compilation.TargetTypeName}' was not found in the compiled assembly.");
            if (!typeof(UIElement).IsAssignableFrom(targetType))
            {
                throw new InvalidOperationException($"Preview type '{targetType.FullName}' is not a UIElement.");
            }

            Type? applicationType = assembly.GetTypes().FirstOrDefault(type =>
                !type.IsAbstract && typeof(Application).IsAssignableFrom(type));
            Application application = applicationType is null
                ? new Application()
                : (Application)(Activator.CreateInstance(applicationType)
                    ?? throw new InvalidOperationException($"Application '{applicationType.FullName}' could not be created."));
            UIElement? root = null;
            DesignPreviewSession runtimeSession = DesignPreviewSession.Create(
                application,
                () => root = (UIElement)(Activator.CreateInstance(targetType)
                    ?? throw new InvalidOperationException($"Preview type '{targetType.FullName}' could not be created.")),
                width,
                height,
                RenderScale);
            return new PreviewRenderSession(
                loadContext,
                runtimeSession,
                root ?? throw new InvalidOperationException($"Preview type '{targetType.FullName}' did not create a root element."));
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    public (byte[] Image, int Width, int Height, int Stride, TimeSpan RenderTime) Capture()
    {
        ThrowIfDisposed();
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan now = clock.Elapsed;
        session.Pump(now - previousPump);
        previousPump = now;
        WindowPreviewFrame frame = session.CaptureFrame(captureBuffer);
        captureBuffer = frame.Pixels;
        stopwatch.Stop();
        return (frame.Pixels, frame.PixelWidth, frame.PixelHeight, frame.Stride, stopwatch.Elapsed);
    }

    public PreviewMarkupUpdateResult TryApplyMarkup(string currentSource, string updatedSource)
    {
        ThrowIfDisposed();
        return PreviewMarkupHotReload.TryApply(root, currentSource, updatedSource);
    }

    public void Click(float x, float y)
    {
        ThrowIfDisposed();
        session.Click(x, y);
    }

    public void MovePointer(float x, float y)
    {
        ThrowIfDisposed();
        session.MovePointer(x, y);
    }

    public void SetPointerButton(float x, float y, InputMouseButton button, bool isDown)
    {
        ThrowIfDisposed();
        session.SetPointerButton(x, y, button, isDown);
    }

    public void ScrollPointer(float x, float y, int wheelDelta)
    {
        ThrowIfDisposed();
        session.ScrollPointer(x, y, wheelDelta);
    }

    public void LeavePointer()
    {
        ThrowIfDisposed();
        session.LeavePointer();
    }

    public void SendText(string text)
    {
        ThrowIfDisposed();
        session.SendText(text);
    }

    public void PressKey(InputKey key, AutomationModifiers modifiers)
    {
        ThrowIfDisposed();
        session.PressKey(key, modifiers);
    }

    public void SetKeyState(InputKey key, bool isDown)
    {
        ThrowIfDisposed();
        session.SetKeyState(key, isDown);
    }

    public void ResetInput()
    {
        ThrowIfDisposed();
        session.ResetInput();
    }

    public void SaveScreenshot(string path)
    {
        ThrowIfDisposed();
        session.SaveScreenshot(path);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        session.Dispose();
        loadContext.Unload();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class PreviewLoadContext : AssemblyLoadContext
    {
        private readonly IReadOnlyDictionary<string, string> referencePaths;

        public PreviewLoadContext(IReadOnlyDictionary<string, string> referencePaths)
            : base("Cerneala.LivePreview." + Guid.NewGuid().ToString("N"), isCollectible: true)
        {
            this.referencePaths = referencePaths;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            Assembly? shared = Default.Assemblies.FirstOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            if (shared is not null)
            {
                return shared;
            }

            if (assemblyName.Name is not null &&
                referencePaths.TryGetValue(assemblyName.Name, out string? path) &&
                File.Exists(path) &&
                !IsReferenceAssemblyPath(path))
            {
                return LoadFromAssemblyPath(Path.GetFullPath(path));
            }

            return null;
        }

        private static bool IsReferenceAssemblyPath(string path)
        {
            string normalized = path.Replace('/', '\\');
            return normalized.Contains("\\packs\\Microsoft.NETCore.App.Ref\\", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("\\Reference Assemblies\\", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("\\ref\\net", StringComparison.OrdinalIgnoreCase);
        }
    }
}
