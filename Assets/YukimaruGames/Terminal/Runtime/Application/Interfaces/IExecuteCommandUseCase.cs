using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Application.Interfaces
{
    /// <summary>
    /// コマンド実行ユースケースのインターフェイス.
    /// </summary>
    /// <remarks>
    /// モードスタックの唯一の所有者. 「通常状態も1つのモード」として統一する設計のため、
    /// 現在モードの読み取り専用ビュー(Prompt/履歴/補完)もここに集約する
    /// (Facadeである <see cref="ITerminalService"/> はこれへ委譲するだけに留める).
    /// </remarks>
    public interface IExecuteCommandUseCase : IDisposable, IAsyncDisposable, IModeStackInspector
    {
        /// <summary>
        /// グローバル(staticコマンド)から利用可能な出力窓口.
        /// </summary>
        IModeOutput Output { get; }

        /// <summary>
        /// グローバル(staticコマンド)から利用可能なモード遷移要求の窓口.
        /// </summary>
        /// <remarks>
        /// <c>python</c>のような「モードへ入場するコマンド」はこれを注入されて使う.
        /// </remarks>
        IModeTransitionRequestSink Transitions { get; }

        /// <summary>
        /// 実行中フラグ
        /// </summary>
        bool IsExecuting { get; }

        /// <summary>
        /// 継続入力(複数行)待ちかどうか.
        /// </summary>
        bool IsAwaitingContinuation { get; }

        /// <summary>
        /// 現在の実効プロンプト文字列(継続入力中は <see cref="ITerminalMode.ContinuationPrompt"/>).
        /// </summary>
        string Prompt { get; }

        /// <summary>
        /// 現在のモードが、コマンド実行中のプロンプトとスピナーの併記描画を許容するか.
        /// </summary>
        bool AllowsConcurrentSpinner { get; }

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
        /// Ctrl+C相当の割り込み. 実行中ならコマンドをキャンセルするのみ(モードは変更しない)。
        /// 非実行中(モード入力待ち)なら現在モードへ割り込みを問い合わせ、
        /// 応答に応じてモードから抜ける.
        /// </summary>
        /// <remarks>
        /// 非実行中の割り込みは内部で非同期に処理される(モードからのPopを伴いうるため)。
        /// このメソッド自体はその完了を待たずに返る(fire-and-forget)。呼び出し側は
        /// UIスレッドからの同期呼び出しを想定しており、完了通知が必要な場合は
        /// <see cref="IModeStackInspector.Snapshot"/> 等で事後に状態を確認すること.
        /// </remarks>
        void Interrupt();

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories.ICommandHistory.Next"/>
        string NextHistory();

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories.ICommandHistory.Previous"/>
        string PrevHistory();

        /// <inheritdoc cref="YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services.ICommandAutocomplete.Complete"/>
        string[] Autocomplete(string partialWord);
    }
}
