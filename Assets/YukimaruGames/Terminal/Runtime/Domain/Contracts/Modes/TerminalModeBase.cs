using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes.Null;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// <see cref="ITerminalMode"/> の既定実装を提供する基底クラス.
    /// </summary>
    /// <remarks>
    /// <p>
    /// C#の default interface member はIL2CPP環境での安定性が未知数のため使用せず、
    /// 既定値の提供は抽象基底クラスに一本化する。<see cref="ITerminalMode"/> を直接実装する
    /// 自由も残るが、大半の実装者はこのクラスを継承することを推奨する.
    /// </p>
    /// </remarks>
    public abstract class TerminalModeBase : ITerminalMode
    {
        /// <summary>
        /// 既定のプロンプト文字列.
        /// </summary>
        protected const string DefaultPrompt = ">";

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <inheritdoc/>
        public virtual string Prompt => DefaultPrompt;

        /// <inheritdoc/>
        public virtual string ContinuationPrompt => Prompt;

        /// <inheritdoc/>
        public virtual ICommandHistory History => NullCommandHistory.Instance;

        /// <inheritdoc/>
        public virtual ICommandAutocomplete Autocomplete => NullCommandAutocomplete.Instance;

        /// <inheritdoc/>
        public virtual bool AllowsConcurrentSpinner => false;

        /// <inheritdoc/>
        public virtual ValueTask OnEnterAsync(IModeContext context, CancellationToken cancellationToken) => default;

        /// <inheritdoc/>
        public abstract ValueTask<ModeResult> HandleAsync(ModeInput input, IModeContext context, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public virtual InterruptDisposition OnInterrupt(bool isCommandRunning) => InterruptDisposition.NotHandled;

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="ModeExitReason.EnterFailed"/> を含め、部分初期化状態でも安全に動作すること.
        /// </remarks>
        public virtual ValueTask OnExitAsync(ModeExitReason reason) => default;
    }
}
