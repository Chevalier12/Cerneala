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
