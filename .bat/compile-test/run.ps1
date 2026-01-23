# --- Unity Compilation Check (Isolated Environment) ---

$ScriptDir = $PSScriptRoot
$MainProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName
$TempProjectDir = Join-Path $ScriptDir "temp"
$LogFile = Join-Path $ScriptDir "unity_compilation.log"
$VersionFilePath = Join-Path $MainProjectRoot "ProjectSettings/ProjectVersion.txt"

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "    Unity Compilation Verifier (Isolated Mode)" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# 1. Get Unity Version
if (-not (Test-Path $VersionFilePath)) {
    Write-Host "[!] Error: ProjectVersion.txt not found." -ForegroundColor Red
    exit 1
}
$VersionContent = Get-Content $VersionFilePath | Out-String
if ($VersionContent -match "m_EditorVersion: ([\d\.\w]+)") {
    $UnityVersion = $Matches[1]
} else {
    Write-Host "[!] Error: Could not parse Unity version." -ForegroundColor Red
    exit 1
}

Write-Host "[*] Configuration:" -ForegroundColor White
Write-Host "    - Project: $MainProjectRoot" -ForegroundColor Gray
Write-Host "    - Version: $UnityVersion" -ForegroundColor Gray
Write-Host "    - Isolation: $TempProjectDir" -ForegroundColor Gray

# 2. Setup Isolated Environment (Junction/Symlink Clone)
if (-not (Test-Path $TempProjectDir)) {
    Write-Host "`n[*] Initializing isolated project directory..." -ForegroundColor White
    New-Item -ItemType Directory -Path $TempProjectDir -Force | Out-Null
}

$DirectoriesToLink = @("Assets", "Packages", "ProjectSettings")
foreach ($DirName in $DirectoriesToLink) {
    $Target = Join-Path $MainProjectRoot $DirName
    $Link = Join-Path $TempProjectDir $DirName
    
    if (-not (Test-Path $Link)) {
        Write-Host "    - Creating link for $DirName..." -ForegroundColor Gray
        try {
            if ($IsWindows -or $env:OS -match "Windows") {
                cmd /c mklink /J "`"$Link`"" "`"$Target`"" | Out-Null
            } else {
                New-Item -ItemType SymbolicLink -Path $Link -Value $Target -ErrorAction Stop | Out-Null
            }
        } catch {
            Write-Host "`n[!] Error: Failed to create link for $DirName." -ForegroundColor Red
            Write-Host "    Consult the documentation regarding Administrator privileges or Developer Mode." -ForegroundColor Yellow
            exit 1
        }
    }
}

# 3. Resolve Unity Path Dynamically
$UnityPath = ""
if ($IsWindows -or $env:OS -match "Windows") {
    $Candidates = @(
        "C:\Program Files\Unity\Hub\Editor\${UnityVersion}\Editor\Unity.exe",
        "C:\Program Files\Unity\Editor\Unity.exe"
    )
    $findUnity = Get-Command unity.exe -ErrorAction SilentlyContinue
    if ($findUnity) { $Candidates = @($findUnity.Source) + $Candidates }
} else {
    $Candidates = @(
        "/Applications/Unity/Hub/Editor/${UnityVersion}/Unity.app/Contents/MacOS/Unity",
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
    Write-Host "`n[!] Error: Unity executable not found ($UnityVersion)." -ForegroundColor Red
    exit 1
}

# 4. Execute Unity Batch Mode
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

Write-Host "`n[*] Starting Unity compilation check..." -ForegroundColor White
Write-Host "    (This may take time on the first run as it builds the Library folder)`n" -ForegroundColor DarkGray

$ArgList = @("-batchmode", "-nographics", "-quit", "-projectPath", $TempProjectDir, "-logFile", $LogFile)
$process = Start-Process -FilePath $UnityPath -ArgumentList $ArgList -Wait -NoNewWindow -PassThru

# 5. Analyze Results
Write-Host "[*] Analyzing results..." -ForegroundColor White

if (Test-Path $LogFile) {
    $errorLines = Select-String -Path $LogFile -Pattern "error CS[0-9]+:"
    $fatalLines = Select-String -Path $LogFile -Pattern "fatal error|Compilation failed|errors during compilation"

    if ($errorLines -or $fatalLines) {
        Write-Host "`n--------------------------------------------------------" -ForegroundColor Red
        Write-Host "    [FAILED] Compilation errors detected!" -ForegroundColor Red
        Write-Host "--------------------------------------------------------`n" -ForegroundColor Red
        
        if ($errorLines) {
            Write-Host "C# Compile Errors:" -ForegroundColor Yellow
            $errorLines | ForEach-Object { Write-Host "  >> $($_.Line.Trim())" -ForegroundColor White }
        }
        
        if ($fatalLines) {
            Write-Host "`nFatal Errors:" -ForegroundColor Magenta
            $fatalLines | ForEach-Object { Write-Host "  !! $($_.Line.Trim())" -ForegroundColor White }
        }
        
        Write-Host "`n[ACTION] Please review these errors and consult the developer." -ForegroundColor Cyan
        exit 1
    } else {
        Write-Host "`n--------------------------------------------------------" -ForegroundColor Green
        Write-Host "    [PASSED] No compilation errors detected." -ForegroundColor Green
        Write-Host "--------------------------------------------------------`n" -ForegroundColor Green
        exit 0
    }
} else {
    Write-Host "`n[!] Error: Log file was not generated. Unity execution failed.`n" -ForegroundColor Red
    exit 1
}
