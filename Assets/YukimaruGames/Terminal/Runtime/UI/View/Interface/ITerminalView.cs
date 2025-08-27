using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalView
    {
        /// <summary>
        /// 画面サイズの変更イベント.
        /// </summary>
        event Action<Vector2Int> OnScreenSizeChanged;
        
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
