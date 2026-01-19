using System.Collections.Generic;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Runtime.Shared;
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
        
        IFontMutator FontMutator { get; }
        IPixelTexture2DRepository PixelTexture2DRepository { get; }
        IColorPaletteMutator ColorPaletteMutator { get; }
        IGUIStyleMutator LogStyleMutator { get; }
        IGUIStyleMutator InputStyleMutator { get; }
        IGUIStyleMutator PromptStyleMutator { get; }
        IGUIStyleMutator ExecuteButtonStyleMutator { get; }
        IGUIStyleMutator WindowControlButtonStyleMutator { get; }
        IGUIStyleMutator LogCopyButtonStyleMutator { get; }
        ITerminalWindowAnimatorDataMutator WindowAnimatorDataMutator { get; }
        ICursorFlashMutator CursorFlashMutator { get; }
        ITerminalButtonVisibleMutator ButtonVisibleMutator { get; }
        
        ITerminalView View { get; }
        ITerminalEventListener EventListener { get; }
        ITerminalService Service { get; }
        IKeyboardInputHandler KeyboardInput { get; }
        
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

        /// <summary>
        /// 設定の再適用
        /// </summary>
        void Apply();
    }
}
