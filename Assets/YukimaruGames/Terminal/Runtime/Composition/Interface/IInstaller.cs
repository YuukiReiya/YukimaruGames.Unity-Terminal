using System.Threading.Tasks;

namespace YukimaruGames.Terminal.Composition
{
    public interface IInstaller
    {
        /// <summary>
        /// アプリケーションを構築し、Scopeを返す.
        /// </summary>
        TerminalRuntimeScope Install();

        /// <summary>
        /// アプリケーションを破棄する.
        /// </summary>
        /// <remarks>
        /// Unityの<c>OnDestroy</c>等、同期経路からの呼び出しを想定したフォールバック。
        /// モードの<c>OnExitAsync</c>のような非同期の後始末は完走を保証しない(ログのみ)。
        /// 完走を保証したい場合は <see cref="UninstallAsync"/> を使うこと.
        /// </remarks>
        void Uninstall(TerminalRuntimeScope scope);

        /// <summary>
        /// アプリケーションを非同期に破棄する.
        /// </summary>
        /// <remarks>
        /// 利用者が能動的なシャットダウンフローで呼ぶことを想定した経路。
        /// モードの<c>OnExitAsync</c>連鎖を含め、後始末の完走を待つ.
        /// </remarks>
        ValueTask UninstallAsync(TerminalRuntimeScope scope);

        /// <summary>
        /// アプリケーションの設定を再解決（再適用）する.
        /// </summary>
        void Resolve(TerminalRuntimeScope scope);
    }
}
