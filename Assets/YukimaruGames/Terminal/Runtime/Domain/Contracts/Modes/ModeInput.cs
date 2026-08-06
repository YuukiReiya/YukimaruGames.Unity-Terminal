using System;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// <see cref="ITerminalMode.HandleAsync"/> に渡される入力.
    /// </summary>
    public readonly struct ModeInput
    {
        /// <summary>
        /// 評価対象のテキスト.
        /// </summary>
        /// <remarks>
        /// 複数行の継続入力中は、ディスパッチャが蓄積した継続分を含む確定済みテキストが渡される。
        /// モード実装は自前で継続行バッファを持つ必要がない。
        /// </remarks>
        public ReadOnlyMemory<char> Text { get; }

        /// <summary>
        /// 直前の呼び出しが <see cref="ModeResult.NeedMoreInput"/> を返した継続入力かどうか.
        /// </summary>
        public bool IsContinuation { get; }

        public ModeInput(ReadOnlyMemory<char> text, bool isContinuation)
        {
            Text = text;
            IsContinuation = isContinuation;
        }
    }
}
