namespace Cerneala.VisualStudio.Preview;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cerneala.Preview;

internal sealed class CernealaPreviewHostClient : IDisposable
{
    private readonly string executablePath;
    private readonly SemaphoreSlim requestGate = new(1, 1);
    private readonly StringBuilder standardError = new();
    private byte[]? responseImageBuffer;
    private Process? process;
    private int nextRequestId;
    private bool disposed;

    public CernealaPreviewHostClient()
    {
        Assembly assembly = typeof(CernealaPreviewHostClient).Assembly;
        string installRoot = Path.GetDirectoryName(assembly.Location)
            ?? throw new InvalidOperationException("The Cerneala extension installation directory is unavailable.");
        string version = GetExtensionVersion(assembly);
        executablePath = Path.Combine(
            installRoot,
            "PreviewHost",
            version,
            "Cerneala.PreviewHost.exe");
    }

    public Task<PreviewResponse> RenderAsync(
        string documentPath,
        string sourceText,
        int width,
        int height,
        CancellationToken cancellationToken) =>
        SendAsync(new PreviewRequest
        {
            Kind = PreviewRequestKind.Render,
            DocumentPath = documentPath,
            SourceText = sourceText,
            Width = width,
            Height = height
        }, cancellationToken);

    public Task<PreviewResponse> CaptureAsync(CancellationToken cancellationToken) =>
        SendAsync(new PreviewRequest { Kind = PreviewRequestKind.Capture }, cancellationToken);

    public Task<PreviewResponse> ClickAsync(double x, double y, CancellationToken cancellationToken) =>
        SendAsync(new PreviewRequest
        {
            Kind = PreviewRequestKind.Click,
            X = x,
            Y = y
        }, cancellationToken);

    public Task<PreviewResponse> SendTextAsync(string text, CancellationToken cancellationToken) =>
        SendAsync(new PreviewRequest
        {
            Kind = PreviewRequestKind.Text,
            Text = text
        }, cancellationToken);

    public Task<PreviewResponse> PressKeyAsync(
        string key,
        int modifiers,
        CancellationToken cancellationToken) =>
        SendAsync(new PreviewRequest
        {
            Kind = PreviewRequestKind.Key,
            Key = key,
            Modifiers = modifiers
        }, cancellationToken);

    public Task<PreviewResponse> SendInputAsync(
        PreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SendAsync(request, cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Process? active = process;
        process = null;
        if (active is not null)
        {
            try
            {
                if (!active.HasExited)
                {
                    PreviewProtocol.WriteRequest(active.StandardInput.BaseStream, new PreviewRequest
                    {
                        Kind = PreviewRequestKind.Shutdown,
                        RequestId = Interlocked.Increment(ref nextRequestId)
                    });
                    active.StandardInput.Close();
                    if (!active.WaitForExit(500))
                    {
                        active.Kill();
                    }
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }
            finally
            {
                active.Dispose();
            }
        }
    }

    private async Task<PreviewResponse> SendAsync(
        PreviewRequest request,
        CancellationToken cancellationToken)
    {
        await requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            request.RequestId = Interlocked.Increment(ref nextRequestId);
            return await Task.Run(() => SendCore(request), CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            requestGate.Release();
        }
    }

    private PreviewResponse SendCore(PreviewRequest request)
    {
        Process active = EnsureProcess();
        try
        {
            PreviewProtocol.WriteRequest(active.StandardInput.BaseStream, request);
            PreviewResponse response = PreviewProtocol.ReadResponse(
                active.StandardOutput.BaseStream,
                responseImageBuffer)
                ?? throw new EndOfStreamException("The Cerneala preview host closed its output stream.");
            if (response.Kind == PreviewResponseKind.Frame)
            {
                responseImageBuffer = response.Image;
            }
            if (response.RequestId != request.RequestId)
            {
                throw new InvalidDataException("The Cerneala preview host returned a mismatched response.");
            }

            return response;
        }
        catch
        {
            StopProcess(active);
            throw;
        }
    }

    private Process EnsureProcess()
    {
        if (process is { HasExited: false })
        {
            return process;
        }

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("The bundled Cerneala preview host is missing.", executablePath);
        }

        Process started = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };
        started.ErrorDataReceived += OnErrorDataReceived;
        if (!started.Start())
        {
            started.Dispose();
            throw new InvalidOperationException("The Cerneala preview host could not be started.");
        }

        started.BeginErrorReadLine();
        lock (standardError)
        {
            standardError.Clear();
        }
        process = started;
        return started;
    }

    private void StopProcess(Process active)
    {
        if (ReferenceEquals(process, active))
        {
            process = null;
        }

        try
        {
            if (!active.HasExited)
            {
                active.Kill();
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            active.ErrorDataReceived -= OnErrorDataReceived;
            active.Dispose();
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Data))
        {
            return;
        }

        lock (standardError)
        {
            standardError.AppendLine(args.Data);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CernealaPreviewHostClient));
        }
    }

    private static string GetExtensionVersion(Assembly assembly)
    {
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            int metadata = informational!.IndexOf('+');
            return metadata < 0 ? informational : informational.Substring(0, metadata);
        }

        Version version = assembly.GetName().Version ?? new Version(0, 1, 0);
        return version.Major + "." + version.Minor + "." + version.Build;
    }
}
