namespace Cerneala.VisualStudio.Server;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class SystemCernealaServerProcessFactory(ICernealaServerLog log)
    : ICernealaServerProcessFactory
{
    public ICernealaServerProcess Start(string executablePath, string workingDirectory) =>
        new SystemCernealaServerProcess(executablePath, workingDirectory, log);
}

internal sealed class SystemCernealaServerProcess : ICernealaServerProcess
{
    private readonly Process process;
    private readonly TaskCompletionSource<bool> exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SystemCernealaServerProcess(
        string executablePath,
        string workingDirectory,
        ICernealaServerLog log)
    {
        process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };
        process.Exited += HandleExited;
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                log.Error("Cerneala language server stderr: " + args.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("The Cerneala language server process was not started.");
        }

        process.BeginErrorReadLine();
    }

    public event EventHandler? Exited;

    public Stream StandardOutput => process.StandardOutput.BaseStream;

    public Stream StandardInput => process.StandardInput.BaseStream;

    public bool HasExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    public int? ExitCode => HasExited ? process.ExitCode : null;

    public void CloseInput()
    {
        try
        {
            process.StandardInput.Close();
        }
        catch (InvalidOperationException)
        {
        }
    }

#pragma warning disable VSTHRD003 // The process exit task is completed by the OS event callback.
    public async Task<bool> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (HasExited)
        {
            return true;
        }

        Task timeoutTask = Task.Delay(timeout, cancellationToken);
        Task completed = await Task.WhenAny(exited.Task, timeoutTask).ConfigureAwait(false);
        if (completed == timeoutTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }

        await exited.Task.ConfigureAwait(false);
        return true;
    }
#pragma warning restore VSTHRD003

    public void Kill()
    {
        if (!HasExited)
        {
            process.Kill();
        }
    }

    public void Dispose()
    {
        process.Exited -= HandleExited;
        process.Dispose();
    }

    private void HandleExited(object? sender, EventArgs args)
    {
        exited.TrySetResult(true);
        Exited?.Invoke(this, EventArgs.Empty);
    }
}
