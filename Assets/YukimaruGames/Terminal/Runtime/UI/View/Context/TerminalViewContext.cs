using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{
    public class TerminalViewContext
    {
        // Renderers
        public ITerminalWindowRenderer WindowRenderer { get; set; }
        public ITerminalLogRenderer LogRenderer { get; set; }
        public ITerminalInputRenderer InputRenderer { get; set; }
        public ITerminalPromptRenderer PromptRenderer { get; set; }
        public ITerminalExecuteButtonRenderer ExecuteButtonRenderer { get; set; }
        public ITerminalButtonRenderer ButtonRenderer { get; set; }
        public ITerminalLogCopyButtonRenderer LogCopyButtonRenderer { get; set; }
        
        // Data Providers
        public ITerminalWindowRenderDataProvider WindowRenderDataProvider { get; set; }
        public ITerminalLogRenderDataProvider LogRenderDataProvider { get; set; }
        public ITerminalInputRenderDataProvider InputRenderDataProvider { get; set; }
        public ITerminalExecuteButtonRenderDataProvider ExecuteButtonRenderDataProvider { get; set; }
        public ITerminalButtonRenderDataProvider ButtonRenderDataProvider { get; set; }
        
        // Configurator
        public IScrollConfigurator ScrollConfigurator { get; set; } 
    }
}
