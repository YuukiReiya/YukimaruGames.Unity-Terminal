using System.Collections.Generic;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// Command-Query Separation (CQS) 
    /// </summary>
    public interface ITerminalInstaller
    {
        /// <summary>
        /// <see cref="UnityEngine.MonoBehaviour"/>のUpdateで呼び出される想定のインスタンス
        /// </summary>
        IReadOnlyList<IUpdatable> Updatables { get; }
        
        /// <summary>
        /// <see cref="UnityEngine.MonoBehaviour"/>のOnGUIで呼び出される想定のインスタンス
        /// </summary>
        IReadOnlyList<IRenderer> Renderers { get; }
        
        IFontConfigurator FontConfigurator { get; }
        IPixelTexture2DRepository PixelTexture2DRepository { get; }
        IColorPaletteConfigurator ColorPaletteConfigurator { get; }
        IGUIStyleConfigurator LogStyleConfigurator { get; }
        IGUIStyleConfigurator InputStyleConfigurator { get; }
        IGUIStyleConfigurator PromptStyleConfigurator { get; }
        IGUIStyleConfigurator ExecuteButtonStyleConfigurator { get; }
        IGUIStyleConfigurator WindowControlButtonStyleConfigurator { get; }
        IGUIStyleConfigurator LogCopyButtonStyleConfigurator { get; }
        ITerminalWindowAnimatorDataConfigurator WindowAnimatorDataConfigurator { get; }
        ICursorFlashConfigurator CursorFlashConfigurator { get; }
        ITerminalButtonVisibleConfigurator ButtonVisibleConfigurator { get; }
        
        ITerminalView View { get; }
        ITerminalEventListener EventListener { get; }
        ITerminalService Service { get; }
        
        /// <summary>
        /// インストール済みか
        /// </summary>
        bool Installed { get; }
        
        /// <summary>
        /// インストール
        /// </summary>
        void Install();
        
        /// <summary>
        /// アンインストール
        /// </summary>
        void Uninstall();
    }
}
