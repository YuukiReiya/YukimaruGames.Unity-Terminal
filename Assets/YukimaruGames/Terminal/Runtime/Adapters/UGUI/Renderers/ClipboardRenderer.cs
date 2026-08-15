#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// uGUIにおけるクリップボードコピーの実行実装.
    /// </summary>
    /// <remarks>
    /// uGUIはリテインドモードのため、ログ行ごとの「コピー」<see cref="UnityEngine.UI.Button"/>自体は
    /// 呼び出し元(<see cref="LogRenderer"/>)が保持・生成する。本クラスはそのボタンのクリック時に
    /// 呼び出される「コピー実行」の窓口(<see cref="IClipboardRenderer"/>契約)としての役割に専念する
    /// (UIToolkit版と同じ構成).
    /// </remarks>
    public sealed class ClipboardRenderer : IClipboardRenderer, IDisposable
    {
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;

        /// <inheritdoc/>
        public event Action<string> OnClickButton;

        public ClipboardRenderer(ILauncherVisibleProvider launcherVisibleProvider)
        {
            _launcherVisibleProvider = launcherVisibleProvider;
        }

        /// <inheritdoc/>
        public void Render(string copyText)
        {
            if (_launcherVisibleProvider is { IsVisible: false }) return;

            GUIUtility.systemCopyBuffer = copyText;
            OnClickButton?.Invoke(copyText);
        }

        void IDisposable.Dispose()
        {
            OnClickButton = null;
        }
    }
}
#endif
