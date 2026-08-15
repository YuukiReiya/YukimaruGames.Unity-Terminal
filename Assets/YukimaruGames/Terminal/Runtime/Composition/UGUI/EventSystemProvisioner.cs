#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="EventSystem"/>が無ければ生成し、その寿命を持つ.
    /// </summary>
    /// <remarks>
    /// uGUIは<see cref="EventSystem"/>が無いとボタンのクリックも<see cref="InputField"/>の
    /// フォーカスも一切機能しないため、無ければ用意する必要がある。
    /// <para>
    /// ただし判定を<c>Install()</c>(=<c>Awake</c>)で行ってはならない。
    /// <see cref="EventSystem.current"/>は静的リストの先頭を返すだけで、リストへの登録は
    /// <see cref="EventSystem"/>の<c>OnEnable</c>でしか行われない。Unityが保証するのは
    /// 同一コンポーネント内での<c>Awake</c>→<c>OnEnable</c>の順序だけで、別GameObjectとの
    /// 前後関係は保証されないため、シーン上に<see cref="EventSystem"/>があっても
    /// <c>Awake</c>時点では<c>null</c>が返りうる。その結果を信じると重複生成になり、
    /// uGUIが毎フレーム警告を出し、入力モジュールが2つ走って挙動が不定になる(#152)。
    /// </para>
    /// <para>
    /// そのため解決は<see cref="IStartable"/>(全<c>OnEnable</c>完了後に1回)で行う。
    /// 起動直後の1フレームだけ<see cref="EventSystem"/>が存在しない状態になるが、
    /// ターミナルは開くキーを押してから開くため実害はない.
    /// </para>
    /// </remarks>
    internal sealed class EventSystemProvisioner : IStartable, IDisposable
    {
        private const string EventSystemGameObjectName = "Terminal UGUI EventSystem";

        private readonly InputKeyboardType _keyboardType;
        private readonly bool _createIfMissing;

        private RuntimeGeneratedAsset _generated;
        private bool _started;

        /// <param name="keyboardType">
        /// 自前生成する場合に、どの入力モジュールを付けるかの判断材料.
        /// </param>
        /// <param name="createIfMissing">
        /// <see cref="EventSystem"/>が見つからない場合に生成するか。
        /// <c>false</c>なら生成せず警告のみに留める(意図的に置いていない構成を壊さないため).
        /// </param>
        internal EventSystemProvisioner(InputKeyboardType keyboardType, bool createIfMissing)
        {
            _keyboardType = keyboardType;
            _createIfMissing = createIfMissing;
        }

        /// <inheritdoc/>
        void IStartable.Start()
        {
            if (_started) return;
            _started = true;

            // 全OnEnable完了後なので、ここでのcurrentは信頼できる.
            if (EventSystem.current != null) return;

            if (!_createIfMissing)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] No EventSystem found and automatic creation is disabled. " +
                    "Buttons and the input field of the uGUI backend will not respond.");
                return;
            }

            _generated = CreateEventSystem(_keyboardType);
        }

        private static RuntimeGeneratedAsset CreateEventSystem(InputKeyboardType keyboardType)
        {
            var eventSystemGameObject = new GameObject(EventSystemGameObjectName, typeof(EventSystem));

            // Active Input HandlingがInput System専用の環境でStandaloneInputModuleを使うと
            // 実行時例外になるため、解決済みの入力方式に合わせて選び分ける.
#if ENABLE_INPUT_SYSTEM
            if (keyboardType == InputKeyboardType.InputSystem)
            {
                eventSystemGameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                AddLegacyInputModule(eventSystemGameObject);
            }
#else
            AddLegacyInputModule(eventSystemGameObject);
#endif

            return new RuntimeGeneratedAsset(eventSystemGameObject);
        }

        /// <summary>
        /// レガシー入力用のモジュールを付ける.
        /// </summary>
        /// <remarks>
        /// Active Input HandlingがInput System専用の環境では<c>StandaloneInputModule</c>が
        /// 実行時例外になるため付けられない。その場合はInput System用のモジュールへ
        /// フォールバックする。ここで何も付けずに抜けると、EventSystemに入力モジュールが
        /// 存在しない状態になり、操作が一切効かないまま警告も出ず原因の特定が難しくなる.
        /// </remarks>
        private static void AddLegacyInputModule(GameObject eventSystemGameObject)
        {
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            eventSystemGameObject.AddComponent<StandaloneInputModule>();
#else
            eventSystemGameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
        }

        void IDisposable.Dispose()
        {
            // 自前生成した分だけを破棄する(既存のEventSystemには触らない).
            _generated?.Dispose();
            _generated = null;
        }
    }
}
#endif
