#requires -Version 7.0
# Tests for Test-ReproPublicUrl in ReproApps.Common.ps1, against a local HTTP server (no network, no gh).
# Run: pwsh -NoProfile -File Tools/scripts/ReproApps.Tests.ps1 (exit 0 when every check passes).
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'ReproApps.Common.ps1')

# The local server answers like the GitHub release download endpoint did on 2026-10-18 (issue #413): a redirect, then
# Content-Type application/octet-stream for a valid GIF, 206 with the requested bytes for a ranged GET.
$gif89 = [byte[]]([Text.Encoding]::ASCII.GetBytes('GIF89a') + (1..64 | ForEach-Object { [byte]$_ }))
$gif87 = [byte[]]([Text.Encoding]::ASCII.GetBytes('GIF87a') + (1..64 | ForEach-Object { [byte]$_ }))
$zip = [byte[]](@(0x50, 0x4B, 0x03, 0x04) + (1..64 | ForEach-Object { [byte]$_ }))
$routes = @{
    '/download/ok.gif'      = @{ Redirect = '/assets/ok.gif' }
    '/assets/ok.gif'        = @{ Type = 'application/octet-stream'; Body = $gif89 }
    '/assets/ok87.gif'      = @{ Type = 'image/gif'; Body = $gif87 }
    '/assets/zip-named.gif' = @{ Type = 'application/octet-stream'; Body = $zip }
    '/assets/text.gif'      = @{ Type = 'image/gif'; Body = [Text.Encoding]::ASCII.GetBytes('not a gif at all') }
    '/assets/empty.gif'     = @{ Type = 'application/octet-stream'; Body = [byte[]]@() }
    '/assets/app.zip'       = @{ Type = 'application/octet-stream'; Body = $zip }
    '/assets/no-head.zip'   = @{ Type = 'application/octet-stream'; Body = $zip; RefuseHead = $true }
}

$probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$probe.Start()
$port = ([Net.IPEndPoint]$probe.LocalEndpoint).Port
$probe.Stop()
$base = "http://localhost:$port"
$listener = [Net.HttpListener]::new()
$listener.Prefixes.Add("$base/")
$listener.Start()

$server = Start-ThreadJob -ArgumentList $listener, $routes -ScriptBlock {
    param($listener, $routes)
    while ($listener.IsListening) {
        try { $context = $listener.GetContext() } catch { break }
        $request = $context.Request
        $response = $context.Response
        $route = $routes[$request.Url.AbsolutePath]
        if (-not $route) {
            $response.StatusCode = 404
        }
        elseif ($route.Redirect) {
            $response.StatusCode = 302
            $response.RedirectLocation = $route.Redirect
        }
        elseif ($route.RefuseHead -and $request.HttpMethod -eq 'HEAD') {
            $response.StatusCode = 405
        }
        else {
            $body = $route.Body
            $range = $request.Headers['Range']
            if ($range -match '^bytes=(\d+)-(\d+)$' -and $body.Length -gt 0) {
                $from = [int]$Matches[1]
                $to = [Math]::Min([int]$Matches[2], $body.Length - 1)
                $response.StatusCode = 206
                $response.Headers['Content-Range'] = "bytes $from-$to/$($body.Length)"
                $body = [byte[]]$body[$from..$to]
            }
            $response.ContentType = $route.Type
            $response.ContentLength64 = $body.Length
            if ($request.HttpMethod -ne 'HEAD' -and $body.Length -gt 0) { $response.OutputStream.Write($body, 0, $body.Length) }
        }
        $response.Close()
    }
}

$failures = @()
function Test-Case([string] $Name, [string] $Path, [switch] $Gif, [bool] $Expected) {
    $actual = [bool](Test-ReproPublicUrl -Url "$base$Path" -Gif:$Gif)
    if ($actual -ne $Expected) { $script:failures += "$Name : expected $Expected, got $actual ($Path)" }
}

try {
    Test-Case 'GIF89a served as application/octet-stream after a redirect passes' '/download/ok.gif' -Gif $true
    Test-Case 'GIF87a served as image/gif passes' '/assets/ok87.gif' -Gif $true
    Test-Case 'missing GIF (404) fails' '/assets/missing.gif' -Gif $false
    Test-Case 'zip bytes under a .gif name fail' '/assets/zip-named.gif' -Gif $false
    Test-Case 'text served as image/gif fails' '/assets/text.gif' -Gif $false
    Test-Case 'empty file fails' '/assets/empty.gif' -Gif $false
    Test-Case 'zip download passes' '/assets/app.zip' $true
    Test-Case 'zip download that refuses HEAD passes through a ranged GET' '/assets/no-head.zip' $true
    Test-Case 'missing zip (404) fails' '/assets/missing.zip' $false
}
finally {
    $listener.Stop()
    $listener.Close()
    Remove-Job -Job $server -Force
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}
Write-Host 'ReproApps.Tests.ps1: all checks passed.'
exit 0
