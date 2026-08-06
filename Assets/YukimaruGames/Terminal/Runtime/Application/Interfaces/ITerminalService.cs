using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Models;

namespace YukimaruGames.Terminal.Application.Interfaces
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
        event System.Action<LogEntry[]> OnLogAdded;

        /// <summary>
        /// ログの削除通知.
        /// </summary>
        event System.Action<LogEntry[]> OnLogRemoved;
        
        #endregion

        /// <summary>
        /// コマンド実行中有無
        /// </summary>
        bool IsExecuting { get; }

        /// <summary>
        /// 現在の実効プロンプト文字列(継続入力中は継続用のプロンプト).
        /// </summary>
        /// <remarks>
        /// モードスタックの状態を毎フレーム反映するpull型のプロパティ。
        /// 呼び出し側(Renderer等)は値の変化検知を自前で行うこと.
        /// </remarks>
        string Prompt { get; }

        /// <summary>
        /// 現在のモードが、コマンド実行中のプロンプトとスピナーの併記描画を許容するか.
        /// </summary>
        bool AllowsConcurrentSpinner { get; }

        /// <summary>
        /// コマンドの実行.
        /// </summary>
        /// <param name="str">入力文字列</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        ValueTask ExecuteAsync(string str, CancellationToken cancellationToken);

        /// <summary>
        /// Ctrl+C相当の割り込み. 実行中ならコマンドをキャンセルするのみ(モードは変更しない)。
        /// 非実行中(モード入力待ち)なら現在モードへ割り込みを問い合わせ、
        /// 応答に応じてモードから抜ける.
        /// </summary>
        void Interrupt();

        #region Autocomplete

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.ICommandAutocomplete"/>
        string[] Autocomplete(string partialWord);
        
        #endregion
        
        #region Log

        /// <summary>
        /// 描画ログ情報.
        /// </summary>
        /// <remarks>
        /// <p>Dto</p>
        /// </remarks>
        IReadOnlyCollection<LogEntry> Logs { get; }
        
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

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.ICommandHistory.Next"/>
        string NextHistory();

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.ICommandHistory.Previous"/>
        string PrevHistory();

        #endregion
    }
}
