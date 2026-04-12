namespace YukimaruGames.Terminal.Presentation.Models.Window
{
    /// <summary>
    /// ターミナルの状態.
    /// </summary>
    public enum WindowState : byte
    {
        Open = 1,
        Close = 2,
    }
    
    /// <summary>
    /// アンカー
    /// </summary>
    public enum WindowAnchor : byte
    {
        /// <summary>
        /// 左寄せ.
        /// </summary>
        Left = 1,
            
        /// <summary>
        /// 右寄せ.
        /// </summary>
        Right = 2,
            
        /// <summary>
        /// 上寄せ.
        /// </summary>
        Top = 3,
            
        /// <summary>
        /// 下寄せ.
        /// </summary>
        Bottom = 4,
    }
    
    /// <summary>
    /// 画面サイズ.
    /// </summary>
    public enum WindowStyle : byte
    {
        /// <summary>
        /// コンパクト(画面に対して一定比率).
        /// </summary>
        Compact = 1,
        
        /// <summary>
        /// フルサイズ(画面全体).
        /// </summary>
        Full = 2,
    }
    
    /// <summary>
    /// フォーカス
    /// </summary>
    public enum WindowFocus
    {
        None = 0,
        Apply = 1,
        Release = 2,
    }
}
