using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Coordinators
{
    public interface ITerminalGUI
    {
        /// <summary>
        /// 画面サイズの変更イベント.
        /// </summary>
        event Action<Vector2Int> OnScreenSizeChanged;

        /// <summary>
        /// ログのコピーがトリガーされた.
        /// </summary>
        event Action<string> OnLogCopiedTriggered;
        
        /// <summary>
        /// 描画前処理.
        /// </summary>
        event Action OnPreRender;
        
        /// <summary>
        /// 描画後処理.
        /// </summary>
        event Action OnPostRender;

        /// <summary>
        /// 描画.
        /// </summary>
        void Render();
    }
}
