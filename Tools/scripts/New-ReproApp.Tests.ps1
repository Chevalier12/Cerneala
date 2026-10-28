#requires -Version 7.0
# Tests for the ReproHarness.cs that New-ReproApp.ps1 generates: an exception from the scenario must end check mode with
# exit code 3 and an "ERROR: <exception>" report, and must not kill the process of an interactive run (issue #1081).
# Generates an app outside the repository, replaces its scenario with one that throws where "--throw <mode>" says,
# builds it once against this checkout (Release) and runs it once per mode.
# Run: pwsh -NoProfile -File Tools/scripts/New-ReproApp.Tests.ps1 (exit 0 when every check passes).
$ErrorActionPreference = 'Stop'

$root = (& git -C $PSScriptRoot rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0 -or -not $root) { throw 'Run this test from inside a Cerneala checkout.' }
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$work = Join-Path $tempRoot "New-ReproApp.Tests-$PID"
$app = Join-Path $work 'Issue1081'

# The scenario under test. "--throw <mode>" picks where it throws: setup (ReproSetUp), first (the first invariant check,
# called from Start), later (the third check, on a frame without Servo input), changed (the change detector, on a later
# frame), step (a check-mode step), none (never). Without an exception, bug and notreached give exit codes 1 and 2.
$scenario = @'
using Cerneala.UI.Controls;
using Cerneala.UI.Invalidation;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Issue1081;

public partial class MainWindow : Window, IReproScenario
{
    private readonly string mode = ReadMode();
    private ReproHarness harness = null!;
    private bool initialized;
    private int checks;
    private int changes;

    private static string ReadMode()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--throw");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : "none";
    }

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        // "--verdict-file <path>": the test reads what the verdict line shows in an interactive run.
        string[] commandLine = Environment.GetCommandLineArgs();
        int verdictFile = Array.IndexOf(commandLine, "--verdict-file");
        if (verdictFile >= 0 && verdictFile + 1 < commandLine.Length)
        {
            FrameRendered += (_, _) => File.WriteAllText(commandLine[verdictFile + 1], VerdictLine.Text);
        }

        harness = new ReproHarness(
            this,
            this,
            new ReproView(VerdictLine, FirstFailureLine, TotalsLine, NowLine, ScaleLine, StepLine, StepsText, ExpectedText));
        harness.Start();
    }

    public void ReproSetUp()
    {
        if (mode == "setup")
        {
            throw new InvalidOperationException("setup failed");
        }

        ServoApi.SetId(SampleButton, "sample-button");
    }

    public string ReproUserSteps => "1. Wait.";

    public string ReproExpected => "The window stays open.";

    public bool ReproCheckedStateChanged()
    {
        // Keep frames coming for a while, so that an interactive run reaches the later checks without input.
        if (++changes < 20)
        {
            Invalidate(InvalidationFlags.Render, "Test: next frame");
        }

        if (mode == "changed" && changes >= 2)
        {
            throw new InvalidOperationException("change detector failed");
        }

        return true;
    }

    public ReproCheckResult ReproCheckInvariant(int issue)
    {
        checks++;
        if (mode == "first" || (mode == "later" && checks >= 3))
        {
            throw new InvalidOperationException("invariant failed to evaluate");
        }

        // Constant texts: a text that changes on every frame keeps the window busy, and Servo would never see it idle.
        bool passed = mode != "bug";
        return new ReproCheckResult(passed, passed ? null : "the invariant is false", "Checked on every frame.", "");
    }

    public string? ReproNotReached() => mode == "notreached" ? "the condition never happened" : null;

    public IReadOnlyList<ReproStep> ReproCheckSteps(ReproHarness harness) =>
    [
        new ReproStep("wait for 10 checks", async servo =>
        {
            if (mode == "step")
            {
                throw new InvalidOperationException("step failed");
            }

            await harness.WaitForStateAsync(servo, () => checks >= 10, "10 checks");
        })
    ];
}
'@

$failures = @()
try {
    pwsh -NoProfile -File (Join-Path $PSScriptRoot 'New-ReproApp.ps1') -Issue 1081 -Destination $app -CernealaRoot $root | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'New-ReproApp.ps1 failed.' }
    [IO.File]::WriteAllText((Join-Path $app 'MainWindow.crn.cs'), $scenario, [Text.UTF8Encoding]::new($false))

    $build = & dotnet build (Join-Path $app 'Issue1081.csproj') -c Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        $build | Select-String 'error' | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
        throw 'dotnet build of the generated app failed.'
    }
    $exe = Join-Path $app 'bin\Release\net8.0-windows\Issue1081.exe'

    function Test-CheckMode([string] $Mode, [int] $ExpectedExit, [string] $ExpectedStart) {
        $report = Join-Path $work "report-$Mode.txt"
        $process = Start-Process -FilePath $exe -ArgumentList '--throw', $Mode, '--check', "`"$report`"" -PassThru
        if (-not $process.WaitForExit(120000)) {
            try { $process.Kill($true) } catch { }
            $script:failures += "check mode, --throw $Mode : timed out after 120 s"
            return
        }

        $text = if (Test-Path -LiteralPath $report) { [IO.File]::ReadAllText($report) } else { '(no report file)' }
        $first = ($text -split "`r?`n")[0]
        Write-Host "check mode, --throw $Mode : exit $($process.ExitCode): $first"
        if ($process.ExitCode -ne $ExpectedExit -or -not $text.StartsWith($ExpectedStart, [StringComparison]::Ordinal)) {
            $script:failures += "check mode, --throw $Mode : expected exit $ExpectedExit and a report that starts with '$ExpectedStart', got exit $($process.ExitCode) and '$first'"
        }
    }

    # Without --check the process must stay open and its verdict line must show the error.
    function Test-Interactive([string] $Mode, [string] $ExpectedStart) {
        $verdictFile = Join-Path $work "verdict-$Mode.txt"
        $process = Start-Process -FilePath $exe -ArgumentList '--throw', $Mode, '--verdict-file', "`"$verdictFile`"" -PassThru
        try {
            $verdict = ''
            $watch = [Diagnostics.Stopwatch]::StartNew()
            while (-not $process.WaitForExit(250) -and $watch.Elapsed.TotalSeconds -lt 60) {
                $verdict = if (Test-Path -LiteralPath $verdictFile) { try { [IO.File]::ReadAllText($verdictFile) } catch { '' } } else { '' }
                if ($verdict.StartsWith($ExpectedStart, [StringComparison]::Ordinal)) { break }
            }

            $state = if ($process.HasExited) { "exited with $($process.ExitCode)" } else { 'running' }
            $first = ($verdict -split "`r?`n")[0]
            Write-Host "interactive, --throw $Mode : $state, verdict line '$first'"
            if ($process.HasExited -or -not $verdict.StartsWith($ExpectedStart, [StringComparison]::Ordinal)) {
                $script:failures += "interactive, --throw $Mode : expected a running window whose verdict line starts with '$ExpectedStart', got $state and '$first'"
            }
        }
        finally {
            if (-not $process.HasExited) { $process.Kill($true) }
        }
    }

    Test-CheckMode 'none' 0 'OK: bug #1081 does not reproduce so far.'
    Test-CheckMode 'bug' 1 'BUG: bug #1081 reproduces.'
    Test-CheckMode 'notreached' 2 'NOT REACHED: the condition never happened'
    Test-CheckMode 'setup' 3 'ERROR: System.InvalidOperationException: setup failed'
    Test-CheckMode 'first' 3 'ERROR: System.InvalidOperationException: invariant failed to evaluate'
    Test-CheckMode 'later' 3 'ERROR: System.InvalidOperationException: invariant failed to evaluate'
    Test-CheckMode 'changed' 3 'ERROR: System.InvalidOperationException: change detector failed'
    Test-CheckMode 'step' 3 'ERROR: System.InvalidOperationException: step failed'
    Test-Interactive 'first' 'ERROR: System.InvalidOperationException: invariant failed to evaluate'
    Test-Interactive 'later' 'ERROR: System.InvalidOperationException: invariant failed to evaluate'
}
finally {
    $resolved = [IO.Path]::GetFullPath($work)
    if ((Test-Path -LiteralPath $resolved) -and $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolved) -eq "New-ReproApp.Tests-$PID") {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}
Write-Host 'New-ReproApp.Tests.ps1: all checks passed.'
exit 0
