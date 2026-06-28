using System;
using System.Threading;
using System.Threading.Tasks;

namespace YukimaruGames.Terminal.Application.Interfaces
{
    /// <summary>
    /// コマンド実行ユースケースのインターフェイス.
    /// </summary>
    public interface IExecuteCommandUseCase
    {
        /// <summary>
        /// 実行中フラグ
        /// </summary>
        bool IsExecuting { get; }

        /// <summary>
        /// 入力されたコマンド文字列を解析し、検証から実行に至るまでの一連の処理パイプラインを実行。
        /// </summary>
        /// <param name="str">解析対象となるコマンドラインの文字列。</param>
        /// <param name="cancellationToken">非同期処理のキャンセルを通知するトークン。</param>
        /// <returns>
        /// コマンドのパイプライン処理が完了したことを表す <see cref="ValueTask"/>。
        /// 登録されたハンドラーが同期処理である場合は、タスクのアロケーションを発生させず即座に完了します。
        /// </returns>
        /// <remarks>
        /// 呼び出し側（ServiceやUI）は、実行対象のコマンドが同期処理か非同期処理かを意識する必要はありません。<br/>
        /// このメソッド内部で適切な実行コンテキストへの振り分けと安全な排他制御が行われ、実処理の完遂が保証されます。
        /// </remarks>
        ValueTask ExecutePipelineAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken);
        
        /// <summary>
        /// 実行中コマンドのキャンセル.
        /// </summary>
        void CancelCommandIfNeeded();
    }
}