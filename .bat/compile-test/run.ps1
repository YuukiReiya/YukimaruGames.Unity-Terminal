# --- Unity Compilation Check (Portable) ---

# 1. Get Version Information
$ProjectRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$VersionFilePath = Join-Path $ProjectRoot "ProjectSettings/ProjectVersion.txt"
$LogFile = Join-Path $PSScriptRoot "unity_compilation.log"

if (-not (Test-Path $VersionFilePath)) {
    Write-Host "[ERROR] ProjectVersion.txt not found at $VersionFilePath" -ForegroundColor Red
    exit 1
}

$VersionContent = Get-Content $VersionFilePath | Out-String
if ($VersionContent -match "m_EditorVersion: ([\d\.\w]+)") {
    $UnityVersion = $Matches[1]
} else {
    Write-Host "[ERROR] Could not parse Unity version from ProjectVersion.txt" -ForegroundColor Red
    exit 1
}

Write-Host "Project Root:  $ProjectRoot" -ForegroundColor Cyan
Write-Host "Unity Version: $UnityVersion" -ForegroundColor Cyan

# 2. Resolve Unity Path Dynamically
$UnityPath = ""

if ($IsWindows -or $env:OS -match "Windows") {
    # Windows Candidate Paths
    $Candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "C:\Program Files\Unity\Editor\Unity.exe"
    )
    $findUnity = Get-Command unity.exe -ErrorAction SilentlyContinue
    if ($findUnity) { $Candidates = @($findUnity.Source) + $Candidates }
} else {
    # Mac/Linux Candidate Paths
    $Candidates = @(
        "/Applications/Unity/Hub/Editor/$UnityVersion/Unity.app/Contents/MacOS/Unity",
        "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
    )
    $findUnity = Get-Command unity -ErrorAction SilentlyContinue
    if ($findUnity) { $Candidates = @($findUnity.Source) + $Candidates }
}

foreach ($Path in $Candidates) {
    if ($Path -and (Test-Path $Path)) {
        $UnityPath = $Path
        break
    }
}

if (-not $UnityPath) {
    Write-Host "[ERROR] Unity executable not found. Please ensure Unity $UnityVersion is installed." -ForegroundColor Red
    exit 1
}

Write-Host "Unity Path:    $UnityPath" -ForegroundColor Gray
Write-Host "Log File:      $LogFile" -ForegroundColor Gray

# 3. Cleanup Log and Run
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

Write-Host "Initializing Unity in batch mode (this may take a few moments)..." -ForegroundColor Gray

# Use simple array for ArgumentList to avoid quoting issues
$ArgList = @("-batchmode", "-nographics", "-quit", "-projectPath", $ProjectRoot, "-logFile", $LogFile)

$process = Start-Process -FilePath $UnityPath -ArgumentList $ArgList -Wait -NoNewWindow -PassThru

# 4. Analyze Log
Write-Host "Checking logs for compilation errors..." -ForegroundColor Gray

if (Test-Path $LogFile) {
    $errorLines = Select-String -Path $LogFile -Pattern "error CS[0-9]+:"
    $fatalLines = Select-String -Path $LogFile -Pattern "fatal error|Compilation failed|errors during compilation"

    if ($errorLines -or $fatalLines) {
        Write-Host "[FAILED] Compilation errors detected!" -ForegroundColor Red
        
        if ($errorLines) {
            Write-Host "Compile Errors:" -ForegroundColor Yellow
            $errorLines | ForEach-Object { Write-Host $_.Line -ForegroundColor White }
        }
        
        if ($fatalLines) {
            Write-Host "Fatal Errors:" -ForegroundColor Magenta
            $fatalLines | ForEach-Object { Write-Host $_.Line -ForegroundColor White }
        }
        
        Write-Host "Please consult the developer instead of fixing it yourself if you see structural issues." -ForegroundColor Cyan
        exit 1
    } else {
        Write-Host "[PASSED] No compilation errors detected." -ForegroundColor Green
        exit 0
    }
} else {
    Write-Host "[ERROR] Log file was not generated. Unity might have crashed." -ForegroundColor Red
    exit 1
}
