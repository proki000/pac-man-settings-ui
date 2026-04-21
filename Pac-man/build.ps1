param(
    [ValidateSet("Windows", "Android", "All")]
    [string]$Target = "All",

    [string]$UnityPath = ""
)

$ErrorActionPreference = "Stop"

$ProjectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogPath = Join-Path $ProjectPath "Logs"
New-Item -ItemType Directory -Path $LogPath -Force | Out-Null

if (-not $UnityPath) {
    $Candidates = @(
        "C:\Program Files\Unity\Hub\Editor\2021.3.24f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\2021.3.24f1c1\Editor\Unity.exe"
    )

    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            $UnityPath = $Candidate
            break
        }
    }
}

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    throw "Unity 2021.3.24f1 was not found. Pass -UnityPath 'C:\Path\To\Unity.exe' or install Unity with Windows Build Support and Android Build Support."
}

$Method = switch ($Target) {
    "Windows" { "PacManBuild.BuildWindows" }
    "Android" { "PacManBuild.BuildAndroidApk" }
    default { "PacManBuild.BuildAll" }
}

$BuildLog = Join-Path $LogPath ("build-" + $Target.ToLowerInvariant() + ".log")
& $UnityPath -batchmode -quit -projectPath $ProjectPath -executeMethod $Method -logFile $BuildLog

if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE. See $BuildLog"
}

Write-Host "Build finished. Outputs are under: $(Join-Path $ProjectPath 'Builds')"
