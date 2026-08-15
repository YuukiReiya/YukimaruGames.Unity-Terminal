#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// uGUIの<see cref="InputField"/>へコマンド入力欄の表示・フォーカス・カーソル位置を同期し、
    /// 入力イベントを通知する.
    /// </summary>
    public sealed class InputRenderer : IInputRenderer, IInputProvider, IDisposable
    {
        private readonly InputField _inputField;
        private readonly IScrollMutator _scrollMutator;
        private readonly CursorView _cursorView;

        /// <summary>
        /// IME変換の終了を通知するまで待つフレーム数.
        /// </summary>
        /// <remarks>
        /// 変換確定のEnterは、IMEが確定を処理した後にターミナル側の入力ハンドラ(Update駆動)にも
        /// 届く。その時点では<c>compositionString</c>が既に空のため「変換中ではない」と判定され、
        /// 確定のつもりで押したEnterがコマンド送信として誤発火する(実機で確認)。
        /// 変換終了の通知を数フレーム遅らせ、確定直後のEnterを変換中として扱う.
        /// </remarks>
        private const int ImeCompositionGraceFrames = 2;

        private bool _isCurrentlyFocused;
        private bool _isImeComposing;
        private bool _isSyncingFocus;
        private int _imeCompositionGrace;

        /// <inheritdoc/>
        public event Action<string> OnInputTextChanged;
        /// <inheritdoc/>
        public event Action<WindowFocus> OnFocusControlChanged;
        /// <inheritdoc/>
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        /// <inheritdoc/>
        public event Action<bool> OnImeComposingStateChanged;

        public InputRenderer(InputField inputField, IScrollMutator scrollMutator, CursorView cursorView)
        {
            _inputField = inputField;
            _scrollMutator = scrollMutator;
            _cursorView = cursorView;

            if (_inputField == null) return;

            _inputField.onValueChanged.AddListener(OnValueChanged);

            // Tabキーの既定動作(EventSystemのナビゲーションで次のSelectableへフォーカス移動)を止める。
            // ターミナルのTabはオートコンプリートで、IKeyboardInputHandler(Update駆動の別経路)が
            // 処理するため、ここでフォーカスを奪われると補完が効かず入力欄からも抜けてしまう
            // (IMGUI版で報告されている#16と同じ症状).
            var navigation = _inputField.navigation;
            navigation.mode = Navigation.Mode.None;
            _inputField.navigation = navigation;
        }

        /// <summary>
        /// 入力欄の表示内容・フォーカス状態・カーソル位置を<paramref name="data"/>の内容に同期する.
        /// </summary>
        public void Render(InputRenderData data)
        {
            if (_inputField == null) return;

            if (!string.Equals(_inputField.text, data.InputText, StringComparison.Ordinal))
            {
                // onValueChangedを発火させずに値だけ差し替える。通知経路経由で書き戻すと
                // 入力→通知→再描画のループになる.
                _inputField.SetTextWithoutNotify(data.InputText);

                // 値を差し替えるとキャレット位置は元のインデックスのまま取り残される。
                // コマンド実行後に空文字へクリアした場合など、実テキスト長を超えた位置に
                // キャレットが残ると以降の入力位置がずれるため、必ず末尾へ同期する(#122の教訓).
                MoveCursorToEnd();
            }

            ApplyFocus(data.Focus);

            if (data.IsMoveCursorToEnd)
            {
                MoveCursorToEnd();
                OnMoveCursorToEndTriggerChanged?.Invoke(false);
            }

            PollFocusState();
            PollImeComposingState();
        }

        private void ApplyFocus(WindowFocus focus)
        {
            switch (focus)
            {
                case WindowFocus.Apply:
                    if (!_inputField.isFocused)
                    {
                        // ActivateInputField()だけでは、同フレーム内でEnterによる
                        // DeactivateInputField()が後から走ると打ち消される。EventSystemの選択も
                        // 明示して、次のUpdateで確実にフォーカスが戻るようにする(実機で確認).
                        var eventSystem = EventSystem.current;
                        if (eventSystem != null && eventSystem.currentSelectedGameObject != _inputField.gameObject)
                        {
                            eventSystem.SetSelectedGameObject(_inputField.gameObject);
                        }

                        _inputField.ActivateInputField();
                    }

                    break;
                case WindowFocus.Release:
                    if (_inputField.isFocused) _inputField.DeactivateInputField();
                    break;
            }
        }

        /// <summary>
        /// キャレットと選択範囲を末尾へ揃える.
        /// </summary>
        /// <remarks>
        /// <see cref="InputField.caretPosition"/>だけでは選択範囲のアンカーが残り、
        /// 次の入力で選択部分が置き換えられてしまうため、アンカー側も同じ位置へ揃える.
        /// </remarks>
        private void MoveCursorToEnd()
        {
            var end = _inputField.text?.Length ?? 0;
            _inputField.caretPosition = end;
            _inputField.selectionAnchorPosition = end;
            _inputField.selectionFocusPosition = end;
        }

        /// <summary>
        /// フォーカス状態の変化を検出して通知する.
        /// </summary>
        /// <remarks>
        /// uGUIの<see cref="InputField"/>はフォーカス獲得のイベントを持たない
        /// (<c>onEndEdit</c>は喪失時のみ)。<see cref="Render"/>が毎フレーム呼ばれることを利用し、
        /// <see cref="InputField.isFocused"/>のポーリングで検出する.
        /// </remarks>
        private void PollFocusState()
        {
            var focused = _inputField.isFocused;
            if (_isCurrentlyFocused == focused) return;

            _isCurrentlyFocused = focused;
            if (_isSyncingFocus) return;

            OnFocusControlChanged?.Invoke(focused ? WindowFocus.Apply : WindowFocus.Release);
        }

        /// <summary>
        /// IME変換中かどうかを検出して通知する.
        /// </summary>
        /// <remarks>
        /// <c>UnityEngine.Input.compositionString</c>のポーリングで判定する
        /// (UIToolkit版の<c>InputRenderer</c>と同じ方式).
        /// </remarks>
        private void PollImeComposingState()
        {
            var composing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);

            if (composing)
            {
                _imeCompositionGrace = ImeCompositionGraceFrames;

                if (_isImeComposing) return;

                _isImeComposing = true;
                OnImeComposingStateChanged?.Invoke(true);
                return;
            }

            if (!_isImeComposing) return;

            // 変換が終わっても数フレームは「変換中」として扱う(確定のEnterによる誤送信を防ぐ).
            if (_imeCompositionGrace > 0)
            {
                --_imeCompositionGrace;
                return;
            }

            _isImeComposing = false;
            OnImeComposingStateChanged?.Invoke(false);
        }

        /// <summary>
        /// 入力欄の値が変化したときの通知.
        /// </summary>
        /// <remarks>
        /// 未フォーカス時の変化は伝播しない。uGUIの<see cref="InputField"/>はEscapeを
        /// 「キャンセル」として扱い、アクティブ化した時点のテキストへ巻き戻したうえで
        /// この通知を発火する。そのまま伝播させると、Escapeでウィンドウを閉じただけで
        /// 入力中の文字列がPresenter側からも消える(実機で確認)。
        /// 利用者の編集は必ずフォーカス中に起きるため、未フォーカス時の通知は無視してよい
        /// (コードからの書き換えは<c>SetTextWithoutNotify</c>を使うのでそもそも通知されない).
        /// </remarks>
        private void OnValueChanged(string value)
        {
            if (!_inputField.isFocused) return;

            OnInputTextChanged?.Invoke(value);
            _scrollMutator?.ScrollToEnd();
        }

        void IDisposable.Dispose()
        {
            if (_inputField != null)
            {
                _inputField.onValueChanged.RemoveListener(OnValueChanged);
            }

            OnInputTextChanged = null;
            OnFocusControlChanged = null;
            OnMoveCursorToEndTriggerChanged = null;
            OnImeComposingStateChanged = null;
        }
    }
}
#endif
