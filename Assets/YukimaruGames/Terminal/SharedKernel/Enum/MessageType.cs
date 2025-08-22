namespace YukimaruGames.Terminal.SharedKernel
{
    /// <summary>
    /// コマンドログの出力タイプ.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// エラーログ.
        /// </summary>
        /// <remarks>
        /// UnityEngine.LogType.Error
        /// </remarks>
        Error = 1,
        
        /// <summary>
        /// アサートログ.
        /// </summary>
        /// <remarks>
        /// UnityEngine.LogType.Assert
        /// </remarks>
        Assert = 2,
        
        /// <summary>
        /// 警告ログ. 
        /// </summary>
        /// <remarks>
        /// UnityEngine.LogType.Warning
        /// </remarks>
        Warning = 3,
        
        /// <summary>
        /// 通常ログ.
        /// </summary>
        /// <remarks>
        /// UnityEngine.LogType.Log
        /// </remarks>
        Message = 4,
        
        /// <summary>
        /// 例外ログ.
        /// </summary>
        /// <remarks>
        /// UnityEngine.LogType.Exception
        /// </remarks>
        Exception = 5,
        
        /// <summary>
        /// 入力系.
        /// </summary>
        Entry = 6,
        
        /// <summary>
        /// システム系.
        /// </summary>
        System = 7,
    }
}
