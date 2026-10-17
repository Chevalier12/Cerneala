# Pester tests for the pure functions of PrReview.Common.ps1 (no gh, no git).
# Run: Invoke-Pester -Path Tools/scripts/pr-review/PrReview.Tests.ps1
# Assertions use throw-based helpers so the file runs on Pester 3.4 (installed with Windows PowerShell) and on Pester 5.

$commonPath = Join-Path $PSScriptRoot 'PrReview.Common.ps1'
. $commonPath

function Assert-True {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param($Expected, $Actual, [string] $What)
    if ($Expected -cne $Actual) { throw "$What : expected '$Expected' but got '$Actual'." }
}

function New-Comment {
    param([string] $Body, [string] $CreatedAt, $Id = 'c')
    [pscustomobject]@{ id = $Id; url = "https://example.test/$Id"; body = $Body; createdAt = $CreatedAt }
}

$headDate = '2026-10-10T10:00:00Z'
$after = '2026-10-10T11:00:00Z'
$sha = '73de8e08664c6094b17b6f4e9042e6302aefab3b'
$otherSha = 'a844dca6a844dca6a844dca6a844dca6a844dca6'

Describe 'Get-PrValidationComment' {
    BeforeAll { . $commonPath }

    foreach ($case in @(
            @{ Body = 'Validated.'; Extra = '' },
            @{ Body = "  Validated.`r`n"; Extra = '' },
            @{ Body = 'Validated. Escape key test rows'; Extra = 'Escape key test rows' },
            @{ Body = "Validated.`nCheck the rows touch"; Extra = 'Check the rows touch' },
            @{ Body = "Validated.`t  tabs  "; Extra = 'tabs' })) {
        It "accepts '$($case.Body.Trim() -replace '\s+', ' ')'" {
            $result = Get-PrValidationComment -Comments @(New-Comment $case.Body $after) -HeadCommittedDate $headDate
            Assert-True ($null -ne $result) 'validation was null.'
            Assert-Equal $case.Extra $result.extraInstructions 'extraInstructions'
        }
    }

    foreach ($body in @('validated.', 'Validated', 'Validated.Escape', 'Validated: rows touch', 'LGTM. Validated.', 'VALIDATED.', 'Not Validated.', '')) {
        It "rejects '$body'" {
            $result = Get-PrValidationComment -Comments @(New-Comment $body $after) -HeadCommittedDate $headDate
            Assert-True ($null -eq $result) "'$body' was accepted."
        }
    }

    It 'rejects a comment older than or equal to the head commit' {
        $older = Get-PrValidationComment -Comments @(New-Comment 'Validated.' '2026-10-10T09:59:59Z') -HeadCommittedDate $headDate
        $equal = Get-PrValidationComment -Comments @(New-Comment 'Validated.' $headDate) -HeadCommittedDate $headDate
        Assert-True ($null -eq $older) 'older comment was accepted.'
        Assert-True ($null -eq $equal) 'comment at the same time as the head commit was accepted.'
    }

    It 'compares instants across time zone offsets' {
        $result = Get-PrValidationComment -Comments @(New-Comment 'Validated.' '2026-10-10T13:30:00+02:00') -HeadCommittedDate '2026-10-10T11:00:00Z'
        Assert-True ($null -ne $result) '13:30+02:00 (11:30Z) should be newer than 11:00Z.'
    }

    It 'lets the newest valid comment win and ignores invalid newer ones' {
        $comments = @(
            (New-Comment 'Validated. first' '2026-10-10T11:00:00Z' 'one'),
            (New-Comment 'Validated. third' '2026-10-10T13:00:00Z' 'three'),
            (New-Comment 'Validated. second' '2026-10-10T12:00:00Z' 'two'),
            (New-Comment 'validated.' '2026-10-10T14:00:00Z' 'bad'),
            (New-Comment 'Validated. before head' '2026-10-10T08:00:00Z' 'old'))
        $result = Get-PrValidationComment -Comments $comments -HeadCommittedDate $headDate
        Assert-Equal 'three' $result.id 'id'
        Assert-Equal 'third' $result.extraInstructions 'extraInstructions'
        Assert-Equal '2026-10-10T13:00:00Z' $result.createdAt 'createdAt'
    }

    It 'accepts DateTime values as ConvertFrom-Json produces them' {
        $json = '[{"id":"x","url":"u","body":"Validated.","createdAt":"2026-10-10T11:00:00Z"}]' | ConvertFrom-Json
        $result = Get-PrValidationComment -Comments $json -HeadCommittedDate '2026-10-10T10:00:00Z'
        Assert-True ($null -ne $result) 'DateTime comment was rejected.'
    }

    It 'returns null for no comments or an unknown head date' {
        Assert-True ($null -eq (Get-PrValidationComment -Comments @() -HeadCommittedDate $headDate)) 'empty comments.'
        Assert-True ($null -eq (Get-PrValidationComment -Comments $null -HeadCommittedDate $headDate)) 'null comments.'
        Assert-True ($null -eq (Get-PrValidationComment -Comments @(New-Comment 'Validated.' $after) -HeadCommittedDate $null)) 'null head date.'
    }
}

Describe 'Test-PrReviewAnswered' {
    BeforeAll { . $commonPath }

    $validation = [pscustomobject]@{ createdAt = '2026-10-10T11:00:00Z' }

    It 'is true for a newer merged marker with the same sha' {
        $comments = @(New-Comment "<!-- cerneala-pr-merged head=$sha -->`nMerged." '2026-10-10T12:00:00Z')
        Assert-True (Test-PrReviewAnswered -Comments $comments -Validation $validation -Sha $sha) 'merged marker not found.'
    }

    It 'is true for a newer not-merged marker, even after leading blank lines' {
        $comments = @(New-Comment "`n  <!-- cerneala-pr-not-merged head=$sha --> not merged" '2026-10-10T12:00:00Z')
        Assert-True (Test-PrReviewAnswered -Comments $comments -Validation $validation -Sha $sha) 'not-merged marker not found.'
    }

    It 'is false for a marker older than the validation' {
        $comments = @(New-Comment "<!-- cerneala-pr-merged head=$sha -->" '2026-10-10T10:30:00Z')
        Assert-True (-not (Test-PrReviewAnswered -Comments $comments -Validation $validation -Sha $sha)) 'old marker counted.'
    }

    It 'is false for a marker of another head sha' {
        $comments = @(New-Comment "<!-- cerneala-pr-merged head=$otherSha -->" '2026-10-10T12:00:00Z')
        Assert-True (-not (Test-PrReviewAnswered -Comments $comments -Validation $validation -Sha $sha)) 'other sha counted.'
    }

    It 'is false when the marker is not on the first line' {
        $comments = @(New-Comment "Result:`n<!-- cerneala-pr-merged head=$sha -->" '2026-10-10T12:00:00Z')
        Assert-True (-not (Test-PrReviewAnswered -Comments $comments -Validation $validation -Sha $sha)) 'second-line marker counted.'
    }

    It 'is false without a validation or without comments' {
        $comments = @(New-Comment "<!-- cerneala-pr-merged head=$sha -->" '2026-10-10T12:00:00Z')
        Assert-True (-not (Test-PrReviewAnswered -Comments $comments -Validation $null -Sha $sha)) 'null validation.'
        Assert-True (-not (Test-PrReviewAnswered -Comments @() -Validation $validation -Sha $sha)) 'no comments.'
    }
}

Describe 'Get-PrStopReason' {
    BeforeAll { . $commonPath }

    $validation = [pscustomobject]@{ id = 'v' }
    function New-Pr { param($State = 'OPEN', $IsDraft = $false, $Base = 'master', $Head = $sha)
        [pscustomobject]@{ state = $State; isDraft = $IsDraft; baseRefName = $Base; headRefOid = $Head } }

    It 'is null for an open, non-draft PR on master with the expected head and a validation' {
        Assert-True ($null -eq (Get-PrStopReason -Pr (New-Pr) -Sha $sha -Validation $validation)) 'expected no stop reason.'
    }
    It 'reports the state' { Assert-Equal 'not OPEN (MERGED)' (Get-PrStopReason -Pr (New-Pr -State 'MERGED') -Sha $sha -Validation $validation) 'reason' }
    It 'reports a draft' { Assert-Equal 'draft' (Get-PrStopReason -Pr (New-Pr -IsDraft $true) -Sha $sha -Validation $validation) 'reason' }
    It 'reports a wrong base' { Assert-Equal 'base is dev, not master' (Get-PrStopReason -Pr (New-Pr -Base 'dev') -Sha $sha -Validation $validation) 'reason' }
    It 'reports a moved head' {
        Assert-Equal "head is $otherSha, not $sha" (Get-PrStopReason -Pr (New-Pr -Head $otherSha) -Sha $sha -Validation $validation) 'reason'
    }
    It 'reports a missing validation' {
        Assert-Equal 'no valid Validated. comment newer than the head commit' (Get-PrStopReason -Pr (New-Pr) -Sha $sha -Validation $null) 'reason'
    }
    It 'reports the first applicable reason' {
        Assert-Equal 'not OPEN (CLOSED)' (Get-PrStopReason -Pr (New-Pr -State 'CLOSED' -IsDraft $true) -Sha $sha -Validation $null) 'reason'
    }
}

Describe 'Get-ProbeLabel' {
    BeforeAll { . $commonPath }

    It 'labels a merge tree' { Assert-Equal 'merge' (Get-ProbeLabel -Tree 'C:\Users\x\AppData\Local\Temp\crn\pr385\merge') 'label' }
    It 'labels a master tree' { Assert-Equal 'master' (Get-ProbeLabel -Tree 'C:\t\crn\pr385\master') 'label' }
    It 'tolerates a trailing separator and slashes' {
        Assert-Equal 'merge' (Get-ProbeLabel -Tree 'C:\t\crn\pr385\merge\') 'backslash'
        Assert-Equal 'master' (Get-ProbeLabel -Tree 'C:/t/crn/pr385/master/') 'slash'
    }
    It 'uses the leaf folder name otherwise' {
        Assert-Equal 'session' (Get-ProbeLabel -Tree 'C:\t\crn\session') 'leaf'
        Assert-Equal 'mymerge' (Get-ProbeLabel -Tree 'C:\t\crn\mymerge') 'suffix is not the label'
    }
}

Describe 'Test-PrPathUnder' {
    BeforeAll { . $commonPath }

    It 'accepts children and rejects the parent itself and siblings with the same prefix' {
        Assert-True (Test-PrPathUnder -Path 'C:\t\crn\pr1\merge' -Parent 'C:\t\crn\pr1') 'child.'
        Assert-True (-not (Test-PrPathUnder -Path 'C:\t\crn\pr1' -Parent 'C:\t\crn\pr1')) 'parent itself.'
        Assert-True (-not (Test-PrPathUnder -Path 'C:\t\crn\pr10\merge' -Parent 'C:\t\crn\pr1')) 'prefix sibling.'
        Assert-True (-not (Test-PrPathUnder -Path 'C:\t\crn\pr1\..\other' -Parent 'C:\t\crn\pr1')) 'dot-dot escape.'
    }
}
