#requires -Version 7.0
<#
Shared helpers of Publish-ReproApps.ps1 (dot-source this file).

  Test-ReproPublicUrl: checks that a public download URL answers (see ReproApps.Tests.ps1).
#>

function Test-ReproPublicUrl {
    <#
    Checks one public URL and prints one line: "<status> <url> [<method>] <content type>[ <first bytes>]", or
    "FAILED <url>: <message>". Returns $true when the URL answered 200 or 206 and, with -Gif, the served file starts
    with the GIF signature GIF87a or GIF89a.

    Without -Gif: a HEAD request (a ranged GET of byte 0 when HEAD is refused).
    With -Gif: a ranged GET of bytes 0-5. The Content-Type is not checked, because GitHub serves release GIFs as
    application/octet-stream although their asset content_type is image/gif (issue #413).
    #>
    param(
        [Parameter(Mandatory)][string] $Url,
        [switch] $Gif
    )

    try {
        if ($Gif) {
            $via = 'GET bytes=0-5'
            $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 5 -Headers @{ Range = 'bytes=0-5' }
        }
        else {
            $via = 'HEAD'
            try {
                $response = Invoke-WebRequest -Uri $Url -Method Head -MaximumRedirection 5
            }
            catch {
                $via = 'GET bytes=0-0'
                $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 5 -Headers @{ Range = 'bytes=0-0' }
            }
        }
        $contentType = "$($response.Headers['Content-Type'])"
        $okStatus = $response.StatusCode -in 200, 206
        $line = "{0} {1} [{2}] {3}" -f $response.StatusCode, $Url, $via, $contentType
        $okFile = $true
        if ($Gif) {
            $head = [byte[]]::new(6)
            $stream = $response.RawContentStream
            $stream.Position = 0
            $read = $stream.Read($head, 0, 6)
            $signature = [Text.Encoding]::ASCII.GetString($head, 0, $read)
            $okFile = $signature -ceq 'GIF87a' -or $signature -ceq 'GIF89a'
            $line += " first bytes '$($signature -replace '[^\x20-\x7E]', '.')'$(if (-not $okFile) { ', not a GIF' })"
        }
        Write-Host $line
        return ($okStatus -and $okFile)
    }
    catch {
        Write-Host "FAILED ${Url}: $($_.Exception.Message)"
        return $false
    }
}
