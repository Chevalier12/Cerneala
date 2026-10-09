[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
try {
    # Pin the host's module: inherited PS7 module paths can shadow PS5's hash function.
    Import-Module (Join-Path $PSHOME 'Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop
    $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $toolRoot = Join-Path $repositoryRoot 'Tools/RoslynMcp'
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $toolRoot 'manifest.json') | ConvertFrom-Json
    $installation = Join-Path $env:LOCALAPPDATA "CernealaTools/mcpRoslyn/$($manifest.commit)-$($manifest.variant)"
    $receiptPath = Join-Path $installation 'installation.json'
    if (-not (Test-Path -LiteralPath $receiptPath)) { throw 'Run Tools/scripts/Install-RoslynMcp.ps1 first.' }
    $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
    $exe = Join-Path $installation 'publish/mcpRoslyn.exe'
    $patchHash = (Get-FileHash -LiteralPath (Join-Path $toolRoot 'roslyn-5.9.patch') -Algorithm SHA256).Hash
    if ($receipt.commit -ne $manifest.commit -or $receipt.variant -ne $manifest.variant -or
        $receipt.patchHash -ne $patchHash -or
        (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne $receipt.executableHash) {
        throw 'Installed Roslyn MCP does not match the pinned receipt. Nothing launched.'
    }
    foreach ($relative in @('BuildHost-netcore', 'BuildHost-net472')) {
        if (-not (Test-Path -LiteralPath (Join-Path $installation "publish/$relative"))) { throw "Missing published sidecar: $relative" }
    }
    # The single-file executable bundles hostfxr, but MSBuildLocator P/Invokes it by
    # name and fails with DllNotFoundException. Expose the installed SDK host's
    # newest hostfxr folder on PATH so the native search can resolve it.
    $dotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $dotnet) { throw 'dotnet was not found on PATH. Nothing launched.' }
    $fxrRoot = Join-Path (Split-Path -Parent $dotnet.Source) 'host/fxr'
    $fxr = Get-ChildItem -LiteralPath $fxrRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'hostfxr.dll') } |
        Sort-Object { [version]($_.Name -replace '-.*$', '') } | Select-Object -Last 1
    if (-not $fxr) { throw "No hostfxr.dll under $fxrRoot. Nothing launched." }
    $env:PATH = "$($fxr.FullName);$env:PATH"
    # Clients may terminate this launcher instead of closing stdio; Windows does
    # not end child processes with their parent, so mcpRoslyn (~1 GB) would stay
    # orphaned. Join a kill-on-close job: the handle closes when this process
    # exits for any reason, and Windows then ends mcpRoslyn and its BuildHosts.
    try {
        Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class CernealaKillOnCloseJob
{
    [StructLayout(LayoutKind.Sequential)]
    struct BasicLimits
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct ExtendedLimits
    {
        public BasicLimits Basic;
        public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateJobObject(IntPtr attributes, IntPtr name);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref ExtendedLimits info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    public static void Enter()
    {
        IntPtr job = CreateJobObject(IntPtr.Zero, IntPtr.Zero);
        if (job == IntPtr.Zero) throw new Win32Exception();
        ExtendedLimits info = new ExtendedLimits();
        info.Basic.LimitFlags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if (!SetInformationJobObject(job, 9, ref info, (uint)Marshal.SizeOf(typeof(ExtendedLimits)))) throw new Win32Exception();
        if (!AssignProcessToJobObject(job, GetCurrentProcess())) throw new Win32Exception();
        // The job handle is intentionally never closed; process exit closes it.
    }
}
'@
        [CernealaKillOnCloseJob]::Enter()
    }
    catch {
        [Console]::Error.WriteLine("Roslyn MCP launcher: kill-on-close job unavailable ($($_.Exception.Message)); mcpRoslyn may outlive a terminated launcher.")
    }
    Set-Location -LiteralPath $repositoryRoot
    # Native invocation forwards the host's redirected stdio; bare Process.Start
    # with CreateNoWindow loses it under this Windows PowerShell host.
    # Match the server's UTF-8 JSON; no build/download/banner on protocol stdout.
    $utf8 = [Text.UTF8Encoding]::new($false)
    [Console]::InputEncoding = $utf8
    [Console]::OutputEncoding = $utf8
    $OutputEncoding = $utf8
    & $exe --solution (Join-Path $repositoryRoot 'Cerneala.slnx')
    exit $LASTEXITCODE
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
