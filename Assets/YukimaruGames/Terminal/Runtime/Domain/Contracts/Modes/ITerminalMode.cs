using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// ターミナルの「モード」(通常状態を含む)を表す契約.
    /// </summary>
    /// <remarks>
    /// 「通常状態も1つのモード」として統一する設計のため、通常のコマンド実行を担う実装
    /// (<c>NormalMode</c>)もこのインターフェイスを実装する。モードはスタックの変更権限
    /// (Push/Pop)を持たず、遷移は必ず <see cref="IModeContext.Transitions"/> 経由で要求する.
    /// </remarks>
    public interface ITerminalMode
    {
        /// <summary>
        /// モードの識別子. 診断表示や <c>ModeId</c> 解決に用いる.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 通常時のプロンプト文字列.
        /// </summary>
        string Prompt { get; }

        /// <summary>
        /// 継続入力待ち時のプロンプト文字列.
        /// </summary>
        string ContinuationPrompt { get; }

        /// <summary>
        /// このモード専用の入力履歴. <c>null</c> は許容しない
        /// (無効化したい場合は既定実装として提供される Null Object を返すこと).
        /// </summary>
        ICommandHistory History { get; }

        /// <summary>
        /// このモード専用の自動補完. <c>null</c> は許容しない
        /// (無効化したい場合は既定実装として提供される Null Object を返すこと).
        /// </summary>
        ICommandAutocomplete Autocomplete { get; }

        /// <summary>
        /// コマンド実行中、プロンプトとローディングスピナーの併記描画を許容するか.
        /// </summary>
        /// <remarks>
        /// 既定値は false(排他描画)。プロンプトとスピナーが連結してユーザー入力のように
        /// 見えてしまう不具合対策として、既定では排他描画を維持する。併記を選ぶ場合、
        /// 見た目の曖昧さを避ける工夫(短いプロンプト文字列にする等)はモード実装側の責務.
        /// </remarks>
        bool AllowsConcurrentSpinner { get; }

        /// <summary>
        /// このモードへの入場時に一度だけ呼ばれる.
        /// </summary>
        /// <remarks>
        /// 失敗(例外)した場合、Pushは取り消され元のモードに留まる。ただしその場合も
        /// <see cref="OnExitAsync"/> が <see cref="ModeExitReason.EnterFailed"/> で必ず対で
        /// 呼ばれるため、実装は「1行も初期化が完了していない」状態に対しても安全であること.
        /// </remarks>
        ValueTask OnEnterAsync(IModeContext context, CancellationToken cancellationToken);

        /// <summary>
        /// 1行(または継続入力込みの確定済みテキスト)の評価.
        /// </summary>
        ValueTask<ModeResult> HandleAsync(in ModeInput input, IModeContext context, CancellationToken cancellationToken);

        /// <summary>
        /// 割り込み(Ctrl+C相当)の同期的な問い合わせ.
        /// </summary>
        /// <param name="isCommandRunning">
        /// 常に false. 実行中の割り込みはディスパッチャがCTSキャンセルのみで処理し、
        /// このメソッドへは到達しない.
        /// </param>
        InterruptDisposition OnInterrupt(bool isCommandRunning);

        /// <summary>
        /// このモードから退場する際に呼ばれる.
        /// </summary>
        /// <remarks>
        /// 例外を投げても、ディスパッチャ側の処理(PopAll等)は中断されない(ログのみで継続).
        /// </remarks>
        ValueTask OnExitAsync(ModeExitReason reason);
    }
}
