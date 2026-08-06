using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モードスタックの読み取り専用ビュー(診断用).
    /// </summary>
    /// <remarks>
    /// スタックの変更権限(Push/Pop)を持つ実体(非公開)とは別の、読み取り専用インターフェイス。
    /// <c>terminal.stack</c> のような診断コマンドから利用される想定.
    /// </remarks>
    public interface IModeStackInspector
    {
        /// <summary>
        /// 現在の深さ(最下段の<c>NormalMode</c>を含む).
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// 現在のスタック内容のスナップショットを取得する.
        /// </summary>
        IReadOnlyList<ModeStackFrameInfo> Snapshot();
    }
}
