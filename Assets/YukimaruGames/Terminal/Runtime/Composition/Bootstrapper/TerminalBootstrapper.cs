using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    public sealed partial class TerminalBootstrapper : MonoBehaviour
    {
        [Header("Installer")]
        [SerializeReference, SerializeInterface]
        private IInstaller _installer = new TerminalStandardInstaller();

        private TerminalRuntimeScope _scope;

        /// <summary>
        /// 0: 生存中, 1: シャットダウン済み(または進行中).
        /// </summary>
        private int _shutdownState;

        private void Awake()
        {
            if (_installer == null)
            {
                _installer = new TerminalNullInstaller();
            }

            _scope = _installer.Install();
        }

        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying && _scope != null)
            {
                _installer?.Resolve(_scope);
            }
        }

        private void Update()
        {
            _scope?.EntryPoint.Update();
        }

        private void OnGUI()
        {
            _scope?.EntryPoint.OnGUI();
        }

        /// <summary>
        /// 明示的な非同期シャットダウン. モードの<c>OnExitAsync</c>連鎖を含め、
        /// 後始末の完走を待ちたい場合はこちらを呼ぶこと(必ず<c>await</c>すること。
        /// awaitせずfire-and-forgetすると、このコンポーネントが破棄された後も
        /// 継続処理が走り続け、UnityAPI呼び出しで例外になる危険がある)。
        /// </summary>
        /// <remarks>
        /// 呼ばれなかった場合は<see cref="OnDestroy"/>の同期フォールバックが働く
        /// (ただし<see cref="System.IAsyncDisposable"/>のみを実装するコンポーネントの
        /// 後始末は完走を保証しない).
        /// </remarks>
        public async Task ShutdownAsync()
        {
            if (Interlocked.Exchange(ref _shutdownState, 1) == 1) return;

            var scope = _scope;
            _scope = null; // Update/OnGUIからの参照を即座に断つ
            if (scope == null || _installer == null) return;

            await _installer.UninstallAsync(scope);
        }

        private void OnDestroy()
        {
            // ShutdownAsync()が既に呼ばれていれば何もしない(二重破棄防止).
            if (Interlocked.Exchange(ref _shutdownState, 1) == 1) return;

            var scope = _scope;
            _scope = null;
            if (scope == null) return;

            // Unityのライフサイクル制約(OnDestroyは同期voidメソッド)による同期フォールバック.
            _installer?.Uninstall(scope);
        }
    }
}
