using YukimaruGames.Terminal.UI.Input;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Log;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalViewContext
    {
        // Renderers
        public IWindowRenderer WindowRenderer { get; set; }
        public ILogRenderer LogRenderer { get; set; }
        public IInputRenderer InputRenderer { get; set; }
        public ITerminalPromptRenderer PromptRenderer { get; set; }
        public ITerminalExecuteButtonRenderer ExecuteButtonRenderer { get; set; }
        public ILauncherRenderer LauncherRenderer { get; set; }
        public ITerminalLogCopyButtonRenderer LogCopyButtonRenderer { get; set; }
        
        // Data Providers
        public IWindowRenderDataProvider WindowRenderDataProvider { get; set; }
        public ILogRenderDataProvider LogRenderDataProvider { get; set; }
        public ITerminalInputRenderDataProvider InputRenderDataProvider { get; set; }
        public ITerminalExecuteButtonRenderDataProvider ExecuteButtonRenderDataProvider { get; set; }
        public ILauncherRenderDataProvider LauncherRenderDataProvider { get; set; }
        
        // Configurator
        public IScrollConfigurator ScrollConfigurator { get; set; } 
    }
}
