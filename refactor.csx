using System.IO;
using System.Text.RegularExpressions;

var directory = "Assets/YukimaruGames/Terminal/Runtime";
var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

foreach (var file in files)
{
    var content = File.ReadAllText(file);
    var newContent = content
        .Replace("YukimaruGames.Terminal.Application.Dto", "YukimaruGames.Terminal.Application.Commands")
        .Replace("YukimaruGames.Terminal.Application.Mapper", "YukimaruGames.Terminal.Application.Commands")
        .Replace("YukimaruGames.Terminal.Infrastructure.Discovery", "YukimaruGames.Terminal.Infrastructure.Commands")
        .Replace("YukimaruGames.Terminal.Infrastructure.Factory", "YukimaruGames.Terminal.Infrastructure.Commands")
        .Replace("YukimaruGames.Terminal.Infrastructure.Context", "YukimaruGames.Terminal.Infrastructure.UI")
        .Replace("YukimaruGames.Terminal.Infrastructure.Provider", "YukimaruGames.Terminal.Infrastructure.UI")
        .Replace("YukimaruGames.Terminal.Infrastructure.Handle", "YukimaruGames.Terminal.Infrastructure.UI")
        .Replace("YukimaruGames.Terminal.Infrastructure.Repository", "YukimaruGames.Terminal.Infrastructure.UI");
    
    // Also, update namespace YukimaruGames.Terminal.Application to YukimaruGames.Terminal.Application.Core
    // But only for the two files that moved: ITerminalService.cs and TerminalService.cs
    if (file.EndsWith("ITerminalService.cs") || file.EndsWith("TerminalService.cs"))
    {
        newContent = newContent.Replace("namespace YukimaruGames.Terminal.Application\r\n{", "namespace YukimaruGames.Terminal.Application.Core\r\n{");
        newContent = newContent.Replace("namespace YukimaruGames.Terminal.Application\n{", "namespace YukimaruGames.Terminal.Application.Core\n{");
    }

    if (content != newContent)
    {
        File.WriteAllText(file, newContent);
    }
}
