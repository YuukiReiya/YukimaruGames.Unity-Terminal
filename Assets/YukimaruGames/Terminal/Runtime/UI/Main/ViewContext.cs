using YukimaruGames.Terminal.UI.Input;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Log;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.UI.Main
{
    public sealed class ViewContext
    {
        // Renderers
        public IWindowRenderer WindowRenderer { get; set; }
        public ILogRenderer LogRenderer { get; set; }
        public IInputRenderer InputRenderer { get; set; }
        public IPromptRenderer PromptRenderer { get; set; }
        public ISubmitRenderer SubmitRenderer { get; set; }
        public ILauncherRenderer LauncherRenderer { get; set; }
        public IClipboardRenderer ClipboardRenderer { get; set; }
        
        // Data Providers
        public IWindowRenderDataProvider WindowRenderDataProvider { get; set; }
        public ILogRenderDataProvider LogRenderDataProvider { get; set; }
        public IInputRenderDataProvider InputRenderDataProvider { get; set; }
        public ISubmitRenderDataProvider SubmitRenderDataProvider { get; set; }
        public ILauncherRenderDataProvider LauncherRenderDataProvider { get; set; }
        
        // Accessor
        public IScrollAccessor ScrollAccessor { get; set; } 
    }
}
