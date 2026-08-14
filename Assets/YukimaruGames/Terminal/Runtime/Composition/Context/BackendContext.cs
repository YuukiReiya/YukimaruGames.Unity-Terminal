using System.Collections.Generic;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// バックエンド(UIの有無を問わない出力先)の構築結果をとりまとめたContext.
    /// </summary>
    /// <remarks>
    /// <see cref="InstallerBase"/>の唯一の抽象点<c>BuildBackend</c>の戻り値。
    /// 描画を持つバックエンドは<see cref="GraphicalInstallerBase"/>が
    /// RenderingContextとCoordinatorから合成し、外部ターミナル等の描画を持たない
    /// バックエンドはセッション等をそのまま詰めて返す.
    /// </remarks>
    public struct BackendContext
    {
        /// <summary>
        /// 構成データ。<see cref="ScopeBuilder"/>がここから更新対象・破棄対象を振り分ける.
        /// </summary>
        public IReadOnlyList<object> Components { get; set; }

        /// <summary>
        /// <c>OnGUI</c>から描画するGUI実装。描画を持たないバックエンドでは<c>null</c>.
        /// </summary>
        public ITerminalGUI GUI { get; set; }

        /// <summary>
        /// 利用者へ公開するView。ゲーム内ウィンドウを持たない場合は
        /// <see cref="NullTerminalView"/>を設定する.
        /// </summary>
        public ITerminalView View { get; set; }
    }
}
