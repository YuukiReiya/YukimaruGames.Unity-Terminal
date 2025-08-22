using System.Collections.Generic;
using YukimaruGames.Terminal.Application.Model;

namespace YukimaruGames.Terminal.Application
{
    public interface ITerminalService
    {
        #region event

        /// <summary>
        /// ログの更新通知.
        /// </summary>
        /// <remarks>
        /// 削除・追加が同時に行われても変わらず一度だけの呼び出し.
        /// </remarks>
        event System.Action OnLogUpdated;
        
        /// <summary>
        /// ログの追加通知.
        /// </summary>
        event System.Action<LogRenderData[]> OnLogAdded;

        /// <summary>
        /// ログの削除通知.
        /// </summary>
        event System.Action<LogRenderData[]> OnLogRemoved;
        
        #endregion
        
        /// <summary>
        /// コマンドの実行.
        /// </summary>
        /// <param name="str">入力文字列</param>
        void Execute(string str);

        #region Autocomplete

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Service.CommandAutocomplete.Complete"/>
        string[] Autocomplete(string partialWord);
        
        #endregion
        
        #region Log

        /// <summary>
        /// 描画ログ情報.
        /// </summary>
        /// <remarks>
        /// <p>Dto</p>
        /// </remarks>
        IReadOnlyCollection<LogRenderData> Logs { get; }
        
        /// <summary>
        /// ログのバッファーサイズ.
        /// </summary>
        /// <remarks>
        /// 保存しておくログの最大数.
        /// </remarks>
        int LogBufferSize { get; }

        /// <summary>
        /// ログクリア.
        /// </summary>
        void ResetLogs();
        
        /// <summary>
        /// 通常ログの発行.
        /// </summary>
        /// <param name="message">出力文字列</param>
        void Message(string message);
        
        /// <summary>
        /// 警告ログの発行.
        /// </summary>
        /// <param name="message">出力文字列</param>
        void Warning(string message);
        
        /// <summary>
        /// エラーログの発行.
        /// </summary>
        /// <param name="message">出力文字列</param>
        void Error(string message);
        
        /// <summary>
        /// アサートログの発行.
        /// </summary>
        /// <param name="message">出力文字列</param>
        void Assert(string message);
        
        /// <summary>
        /// 例外ログの発行.
        /// </summary>
        /// <param name="message">出力文字列</param>
        void Exception(string message);
        
        /// <summary>
        /// 入力ログの発行.
        /// </summary>
        /// <remarks>
        /// e.g.
        /// 入力文字列の出力.
        /// </remarks>
        /// <param name="message">出力文字列</param>
        void InputMessage(string message);
        
        /// <summary>
        /// システムログの発行.
        /// </summary>
        /// <remarks>
        /// e.g.
        /// コマンド実行システム(Shell)のログ出力...etc
        /// </remarks>
        /// <param name="message">出力文字列</param>
        void SystemMessage(string message);

        #endregion

        #region History

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Service.CommandHistory.Next"/>
        string NextHistory();

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Service.CommandHistory.Previous"/>
        string PrevHistory();

        #endregion
    }
}
