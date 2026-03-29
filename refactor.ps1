
$dir = "Assets/YukimaruGames/Terminal/Runtime"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.cs" | Where-Object { -not $_.PSIsContainer }

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $newContent = $content.Replace("YukimaruGames.Terminal.Application.Dto", "YukimaruGames.Terminal.Application.Commands").Replace("YukimaruGames.Terminal.Application.Mapper", "YukimaruGames.Terminal.Application.Commands").Replace("YukimaruGames.Terminal.Infrastructure.Discovery", "YukimaruGames.Terminal.Infrastructure.Commands").Replace("YukimaruGames.Terminal.Infrastructure.Factory", "YukimaruGames.Terminal.Infrastructure.Commands").Replace("YukimaruGames.Terminal.Infrastructure.Context", "YukimaruGames.Terminal.Infrastructure.UI").Replace("YukimaruGames.Terminal.Infrastructure.Provider", "YukimaruGames.Terminal.Infrastructure.UI").Replace("YukimaruGames.Terminal.Infrastructure.Handle", "YukimaruGames.Terminal.Infrastructure.UI").Replace("YukimaruGames.Terminal.Infrastructure.Repository", "YukimaruGames.Terminal.Infrastructure.UI")
    
    if ($file.Name -eq "ITerminalService.cs" -or $file.Name -eq "TerminalService.cs") {
        $newContent = $newContent.Replace("namespace YukimaruGames.Terminal.Application`r`n{", "namespace YukimaruGames.Terminal.Application.Core`r`n{")
        $newContent = $newContent.Replace("namespace YukimaruGames.Terminal.Application`n{", "namespace YukimaruGames.Terminal.Application.Core`n{")
        
    }
    
    $newContent = $newContent.Replace("using YukimaruGames.Terminal.Application;", "using YukimaruGames.Terminal.Application;`r`nusing YukimaruGames.Terminal.Application.Core;")
    
    if ($content -cne $newContent) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent)
    }
}

