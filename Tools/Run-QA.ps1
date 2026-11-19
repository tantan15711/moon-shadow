[CmdletBinding()]
param(
    [ValidateSet('Campaign', 'MotionReview', 'TouchReview')]
    [string]$Mode = 'Campaign',
    [string]$Editor = $env:UNITY_EDITOR,
    [ValidateRange(30, 900)]
    [int]$TimeoutSeconds = 240
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetsRoot = Join-Path $projectRoot 'Assets\_Project'
$sourceRoot = Join-Path $PSScriptRoot 'QA'
$logRoot = Join-Path $projectRoot '_Logs'

if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'))) {
    throw 'Run this script from a complete Moon Shadow checkout.'
}
if (-not $Editor) {
    $Editor = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe'
}
if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) {
    throw 'Pass -Editor with the Unity 6000.6.9f1 executable path, or set UNITY_EDITOR.'
}

$runtimeNames = @('MoonDemoSmoke.cs', 'MoonMotionReview.cs', 'MoonTouchReview.cs', 'MoonPerformanceReview.cs')
$copies = @($runtimeNames | ForEach-Object {
    @{ Source = Join-Path $sourceRoot $_; Target = Join-Path $assetsRoot ('Scripts\' + $_) }
}) + @(@{ Source = Join-Path $sourceRoot 'BuildSmoke.cs'; Target = Join-Path $assetsRoot 'Editor\BuildSmoke.cs' })

foreach ($copy in $copies) {
    if (-not (Test-Path -LiteralPath $copy.Source -PathType Leaf)) {
        throw "Missing QA source: $($copy.Source)"
    }
    if ((Test-Path -LiteralPath $copy.Target) -or (Test-Path -LiteralPath ($copy.Target + '.meta'))) {
        throw "Temporary QA source already exists: $($copy.Target)"
    }
}
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$buildLog = Join-Path $logRoot 'qa-build.log'
$playerLog = Join-Path $logRoot ($Mode.ToLowerInvariant() + '.log')
$flags = switch ($Mode) {
    'MotionReview' { '-moonMotionReview' }
    'TouchReview' { '-moonTouchTest -moonMobileTest' }
    default { '-moonSmoke' }
}
$expectedMarker = switch ($Mode) {
    'MotionReview' { 'MOON_MOTION_REVIEW_PASSED' }
    'TouchReview' { 'MOON_TOUCH_REVIEW_PASSED' }
    default { 'MOON_CAMPAIGN_PASSED' }
}
$ownedFiles = [System.Collections.Generic.List[string]]::new()
try {
    foreach ($copy in $copies) {
        Copy-Item -LiteralPath $copy.Source -Destination $copy.Target
        $ownedFiles.Add($copy.Target)
    }
    $buildArgs = '-batchmode -nographics -quit -projectPath "{0}" -buildTarget StandaloneWindows64 -executeMethod MoonShadow.Editor.BuildSmoke.Build -logFile "{1}"' -f $projectRoot, $buildLog
    $build = Start-Process -FilePath $Editor -ArgumentList $buildArgs -WindowStyle Hidden -PassThru -Wait
    if ($build.ExitCode -ne 0) {
        throw "QA build exited with $($build.ExitCode). See $buildLog"
    }
    $playerPath = Join-Path $logRoot 'QAPlayer\Moon Shadow QA.exe'
    $playerArgs = '{0} -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile "{1}"' -f $flags, $playerLog
    $player = Start-Process -FilePath $playerPath -ArgumentList $playerArgs -WindowStyle Hidden -PassThru
    if (-not $player.WaitForExit($TimeoutSeconds * 1000)) {
        $player.Kill()
        $player.WaitForExit()
        throw "QA player timed out. See $playerLog"
    }
    if ($player.ExitCode -ne 0) {
        throw "QA player exited with $($player.ExitCode). See $playerLog"
    }
    if (-not (Select-String -LiteralPath $playerLog -SimpleMatch $expectedMarker -Quiet)) {
        throw "QA finished without the success marker $expectedMarker. See $playerLog"
    }
    Get-Content -LiteralPath $playerLog -Tail 24
}
finally {
    foreach ($file in $ownedFiles) {
        foreach ($path in @($file, $file + '.meta')) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path
            }
        }
    }
}
