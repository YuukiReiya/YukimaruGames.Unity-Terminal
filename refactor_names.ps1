
$dir = "Assets/YukimaruGames/Terminal/Runtime"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.cs" | Where-Object { -not $_.PSIsContainer }

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $newContent = $content.Replace("IScrollConfigurator", "IScrollMutator").Replace("ScrollConfigurator", "ScrollMutator").Replace("scrollConfigurator", "scrollMutator").Replace("IWindowAnimatorDataConfigurator", "IWindowAnimatorDataMutator").Replace("WindowAnimatorDataConfigurator", "WindowAnimatorDataMutator").Replace("animatorDataConfigurator", "animatorDataMutator").Replace("ILauncherVisibleConfigurator", "ILauncherVisibleMutator").Replace("LauncherVisibleConfigurator", "LauncherVisibleMutator").Replace("launcherVisibleConfigurator", "launcherVisibleMutator")
    
    if ($content -cne $newContent) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent)
    }
}

