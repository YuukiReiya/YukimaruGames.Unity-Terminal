using System;
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
        private IInstaller _installer = new TerminalIMGUIInstaller();

        private TerminalRuntimeScope _scope;

        /// <summary>
        /// 0: 未シャットダウン, 1: シャットダウン済み(または進行中)。
        /// <c>Awake</c>より先に<c>ShutdownAsync</c>/<c>OnDestroy</c>が走った場合に
        /// 以後の<c>Install</c>を抑止するためのフラグ(破棄権の管理は<c>_scope</c>自体で行う).
        /// </summary>
        private int _shutdownState;

        private void Awake()
        {
            _installer ??= new TerminalNullInstaller();

            if (Volatile.Read(ref _shutdownState) != 0)
            {
                // Awakeより先にシャットダウンされていた: 以後構築しない.
                return;
            }

            var scope = _installer.Install();

            // Install中(同期処理だが将来非同期化される可能性も考慮)にシャットダウンされていないか再確認.
            if (Volatile.Read(ref _shutdownState) != 0)
            {
                _installer.Uninstall(scope);
                return;
            }

            _scope = scope;
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
            Volatile.Write(ref _shutdownState, 1);

            // 破棄権はこの1回のExchangeでのみ獲得される(Awake前後どちらの順序でも、
            // scopeが生成されていれば必ず1回だけ、どちらか片方の経路で破棄される).
            var scope = Interlocked.Exchange(ref _scope, null);
            if (scope == null || _installer == null) return;

            await _installer.UninstallAsync(scope);
        }

        private void OnDestroy()
        {
            Volatile.Write(ref _shutdownState, 1);

            var scope = Interlocked.Exchange(ref _scope, null);
            if (scope == null) return;

            // Unityのライフサイクル制約(OnDestroyは同期voidメソッド)による同期フォールバック.
            // _installerはSerializeReferenceでユーザー実装が入りうるため、
            // OnDestroyから例外を飛ばしてシーン破棄そのものを止めないよう防御する.
            try
            {
                _installer?.Uninstall(scope);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }
}
