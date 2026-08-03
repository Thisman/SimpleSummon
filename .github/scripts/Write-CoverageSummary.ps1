param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageDirectory
)

$summaryPath = $env:GITHUB_STEP_SUMMARY
if ([string]::IsNullOrWhiteSpace($summaryPath)) {
    Write-Warning 'GITHUB_STEP_SUMMARY is unavailable; skipping the coverage summary.'
    exit 0
}

"## Code coverage" >> $summaryPath

if ([string]::IsNullOrWhiteSpace($CoverageDirectory) -or -not (Test-Path -LiteralPath $CoverageDirectory)) {
    'Coverage report was not produced. Check the Unity test step logs.' >> $summaryPath
    Write-Warning 'Coverage report directory was not produced.'
    exit 0
}

$report = Get-ChildItem -LiteralPath $CoverageDirectory -Filter Summary.xml -File -Recurse |
    Select-Object -First 1

if ($null -eq $report) {
    'Coverage Summary.xml was not found. Download the coverage artifact for diagnostics.' >> $summaryPath
    Write-Warning 'Coverage Summary.xml was not found.'
    exit 0
}

[xml]$document = Get-Content -LiteralPath $report.FullName -Raw
$coverage = $document.Summary.Coverage
if ($null -eq $coverage) {
    'Coverage Summary.xml has an unsupported format. Download the coverage artifact for details.' >> $summaryPath
    Write-Warning 'Coverage Summary.xml has an unsupported format.'
    exit 0
}

$metrics = @(
    [PSCustomObject]@{ Metric = 'Lines'; Value = $coverage.Linecoverage }
    [PSCustomObject]@{ Metric = 'Branches'; Value = $coverage.Branchcoverage }
    [PSCustomObject]@{ Metric = 'Methods'; Value = $coverage.Methodcoverage }
    [PSCustomObject]@{ Metric = 'Fully covered methods'; Value = $coverage.FullMethodcoverage }
) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.Value) }

'| Metric | Coverage |' >> $summaryPath
'| --- | ---: |' >> $summaryPath
foreach ($metric in $metrics) {
    "| $($metric.Metric) | $($metric.Value) |" >> $summaryPath
}

'' >> $summaryPath
'The downloadable `unity-coverage` artifact contains the full HTML and XML reports.' >> $summaryPath
