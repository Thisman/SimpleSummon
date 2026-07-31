param(
    [Parameter(Mandatory = $true)]
    [string]$BuildDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Commit,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$RunNumber,

    [Parameter(Mandatory = $true)]
    [string]$PreviousRef,

    [Parameter(Mandatory = $true)]
    [string]$Channel
)

$ErrorActionPreference = 'Stop'

$resolvedBuildDirectory = (Resolve-Path -LiteralPath $BuildDirectory).Path
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "SimpleSummon-$Version"
$archiveBaseName = if ($Channel -eq 'stable') {
    "SimpleSummon-Windows-$Version"
} else {
    'SimpleSummon-Windows-latest-main'
}
$archivePath = Join-Path $resolvedOutputDirectory "$archiveBaseName.zip"

if (-not (Test-Path -LiteralPath (Join-Path $resolvedBuildDirectory 'SimpleSummon.exe'))) {
    throw "SimpleSummon.exe was not found in $resolvedBuildDirectory"
}

New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null
if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDirectory | Out-Null

Copy-Item -Path (Join-Path $resolvedBuildDirectory '*') -Destination $stagingDirectory -Recurse
Get-ChildItem -LiteralPath $stagingDirectory -Directory -Filter '*_BurstDebugInformation_DoNotShip' |
    Remove-Item -Recurse -Force

$shortCommit = $Commit.Substring(0, [Math]::Min(7, $Commit.Length))
$builtAt = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$commitUrl = "https://github.com/$Repository/commit/$Commit"
$runUrl = "https://github.com/$Repository/actions/runs/$env:GITHUB_RUN_ID"

$commitLines = @(
    git log "$PreviousRef..$Commit" --pretty=format:'- %h %s (%an)'
)
if ($LASTEXITCODE -ne 0 -or $commitLines.Count -eq 0) {
    $commitLines = @(
        git log -20 $Commit --pretty=format:'- %h %s (%an)'
    )
}
$changeLog = $commitLines -join [Environment]::NewLine

$buildInfo = [ordered]@{
    product = 'SimpleSummon'
    platform = 'Windows x64'
    version = $Version
    channel = $Channel
    commit = $Commit
    shortCommit = $shortCommit
    repository = $Repository
    workflowRun = [int]$RunNumber
    builtAtUtc = $builtAt
    unityVersion = (Get-Content ProjectSettings/ProjectVersion.txt | Select-String 'm_EditorVersion:' | ForEach-Object { $_.Line.Split(':', 2)[1].Trim() })
}
$buildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stagingDirectory 'BUILD_INFO.json') -Encoding utf8
$Version | Set-Content -LiteralPath (Join-Path $stagingDirectory 'VERSION.txt') -Encoding utf8
$changeLog | Set-Content -LiteralPath (Join-Path $stagingDirectory 'CHANGELOG.md') -Encoding utf8

@"
SimpleSummon $Version

1. Распакуйте архив в отдельную папку.
2. Запустите SimpleSummon.exe.
3. Не переносите EXE отдельно: папки SimpleSummon_Data и MonoBleedingEdge нужны игре.

Commit: $Commit
Build: $runUrl
"@ | Set-Content -LiteralPath (Join-Path $stagingDirectory 'КАК ЗАПУСТИТЬ.txt') -Encoding utf8

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal

$checksum = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$archivePath.sha256"
"$checksum  $([System.IO.Path]::GetFileName($archivePath))" |
    Set-Content -LiteralPath $checksumPath -Encoding ascii

@"
## SimpleSummon $Version

Готовая production-сборка для Windows x64.

### Как запустить

1. Скачайте **$([System.IO.Path]::GetFileName($archivePath))** ниже.
2. Полностью распакуйте ZIP.
3. Запустите **SimpleSummon.exe**.

### Сборка

- Версия: $Version
- Канал: $Channel
- Unity: $($buildInfo.unityVersion)
- Commit: [$shortCommit]($commitUrl)
- Workflow run: [#$RunNumber]($runUrl)
- SHA-256: $checksum

### Изменения

$changeLog
"@ | Set-Content -LiteralPath (Join-Path $resolvedOutputDirectory 'RELEASE_NOTES.md') -Encoding utf8

Write-Output "archive=$archivePath"
Write-Output "checksum=$checksumPath"
