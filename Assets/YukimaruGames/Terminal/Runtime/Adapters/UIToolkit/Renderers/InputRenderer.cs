#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Renderers
{
    /// <summary>
    /// UIToolkit(<see cref="TextField"/>)による入力欄の描画と、入力イベントの通知を行う.
    /// </summary>
    public sealed class InputRenderer : IInputRenderer, IInputProvider, IDisposable
    {
        private readonly TextField _textField;
        private readonly IScrollMutator _scrollMutator;
        private readonly CursorView _cursorView;

        private bool _isCurrentlyFocused;
        private bool _isImeComposing;
        private bool _isSyncingFocus;

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        public InputRenderer(TextField textField, IScrollMutator scrollMutator, CursorView cursorView)
        {
            _textField = textField;
            _scrollMutator = scrollMutator;
            _cursorView = cursorView;

            if (_textField == null) return;

            _textField.RegisterValueChangedCallback(OnValueChanged);
            _textField.RegisterCallback<FocusInEvent>(OnFocusIn);
            _textField.RegisterCallback<FocusOutEvent>(OnFocusOut);

            // Escape/TabはTextFieldのネイティブなデフォルト動作(Escape=編集内容を直前の値へ
            // ロールバック、Tab=次のフォーカス可能要素へ移動)と、ターミナル側の独自バインド
            // (既定でEscape=ウィンドウを閉じる、Tab=オートコンプリート)が衝突する。
            // TrickleDown(capture)フェーズで先取りしてStopPropagationし、ネイティブの
            // デフォルト動作(KeyboardTextEditorEventHandler等)に到達させない。
            // ターミナル側のバインド自体はUpdate()駆動の別経路(IKeyboardInputHandler)で
            // 判定されるため、ここで止めても機能しなくなることはない。
            _textField.RegisterCallback<KeyDownEvent>(OnKeyDownCapture, TrickleDown.TrickleDown);

            // ランタイムパネルではEnter/Escapeキーは(Editor上のKeyDownEventだけでなく)
            // UIToolkitのナビゲーション入力としてもNavigationSubmitEvent/NavigationCancelEvent
            // で独立に配信される。KeyDownEvent側の対策(Escape/Tabのみ止める)だけではこちらの
            // 経路をすり抜け、実機でのみ(Editorでの合成KeyDownEventテストでは再現しない)
            // コマンド実行のたびにフォーカスが実質的に失われる(focusedElement上は継続と
            // 判定されるが実際のキー入力を受け付けなくなる)不具合として顕在化した(#122)。
            // ターミナル側のExecute/Close判定はUpdate()駆動の別経路で行うため、ここでも
            // 同様に先取りして無効化する.
            _textField.RegisterCallback<NavigationSubmitEvent>(OnNavigationEventCapture, TrickleDown.TrickleDown);
            _textField.RegisterCallback<NavigationCancelEvent>(OnNavigationEventCapture, TrickleDown.TrickleDown);
        }

        public void Render(InputRenderData data)
        {
            if (_textField == null) return;

            if (!string.Equals(_textField.value, data.InputText, StringComparison.Ordinal))
            {
                // SetValueWithoutNotify()はTextField.valueは書き換えるが、既にネイティブの編集
                // セッションがアタッチされている(=フィールドが既にフォーカス中の)場合、そのセッションが
                // 内部に保持しているテキストバッファ(TextEditingUtilitiesの内部状態)は追従しない。
                // このズレたバッファへ次の実キー入力が届くと、GeneratePreviewString()が古いバッファ長
                // を基準にString.Insertし、ArgumentOutOfRangeExceptionで失敗する不具合が実機ログ
                // (Editor.log)から確認された(#122)。Blur→Focusで編集セッションを強制的に張り直す
                // ことで回避しているが、以前は「SetValueWithoutNotify → Blur → Focus」の順序だった
                // ため、Blur()がネイティブの編集セッション側の(書き換え前の)古いバッファをvalueへ
                // 書き戻してしまい、コマンド実行後に入力欄がクリアされない不具合を引き起こしていた
                // (#122)。Blurを先に行い未フォーカスの状態で値を書き換え、その後Focusで新しい値を
                // 基準にした編集セッションを開かせる順序に修正する。このBlur/Focusは論理的な
                // フォーカス状態を変えるためのものではないため、OnFocusIn/OnFocusOut側の
                // OnFocusControlChanged通知は抑制する.
                if (_isCurrentlyFocused)
                {
                    _isSyncingFocus = true;
                    _textField.Blur();
                    _textField.SetValueWithoutNotify(data.InputText);
                    _textField.Focus();
                    _isSyncingFocus = false;
                }
                else
                {
                    _textField.SetValueWithoutNotify(data.InputText);
                }
            }

            ApplyFocus(data.Focus);

            if (data.IsMoveCursorToEnd)
            {
                MoveCursorToEnd();
                OnMoveCursorToEndTriggerChanged?.Invoke(false);
            }

            PollImeComposingState();
        }

        private void ApplyFocus(WindowFocus focus)
        {
            switch (focus)
            {
                case WindowFocus.Apply:
                    if (!_isCurrentlyFocused) _textField.Focus();
                    break;
                case WindowFocus.Release:
                    if (_isCurrentlyFocused) _textField.Blur();
                    break;
            }
        }

        private void MoveCursorToEnd()
        {
            // 以前は「フォーカス中のみ」ガードしていたが、_isCurrentlyFocused(FocusIn/Outイベント
            // ベースの自前追跡)は「focusedElementが変化しない=FocusOutが飛ばない」ケース
            // (コマンド実行のたびにSetValueWithoutNotifyで値だけクリアする等)で、実際には
            // ネイティブの編集セッションが再アタッチされていないのに"フォーカス中"と誤判定し、
            // このガードでカーソル同期自体がスキップされ続けて症状が自己再生産される不具合が
            // あった(#122)。フォーカス有無では判定せず、要素がパネルに存在する場合のみ
            // (破棄処理中の呼び出し等を避けるための最小限のガード)常に同期する.
            if (_textField.panel == null) return;

            var end = _textField.text?.Length ?? 0;
            _textField.cursorIndex = end;
            _textField.selectIndex = end;
        }

        private void PollImeComposingState()
        {
            var composing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
            if (_isImeComposing == composing) return;

            _isImeComposing = composing;
            OnImeComposingStateChanged?.Invoke(composing);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            OnInputTextChanged?.Invoke(evt.newValue);
            _scrollMutator?.ScrollToEnd();
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            _isCurrentlyFocused = true;
            if (_isSyncingFocus) return;
            OnFocusControlChanged?.Invoke(WindowFocus.Apply);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            _isCurrentlyFocused = false;
            if (_isSyncingFocus) return;
            OnFocusControlChanged?.Invoke(WindowFocus.Release);
        }

        private void OnKeyDownCapture(KeyDownEvent evt)
        {
            // Return/KeypadEnterは一時ここでも止めていたが、TextFieldのネイティブな
            // デフォルト動作(KeyboardTextEditorEventHandler)まで到達させないと、
            // 編集セッション(IME合成状態・カーソル/選択インデックスの内部管理)が
            // 正しく確定されないまま残り、次のコマンド実行後にfocusedElement上は
            // フォーカス継続と判定されるのに実際にはキー入力を受け付けなくなる
            // (キャレット非表示・入力不可)不具合が実機検証で確認された(#122)。
            // Escapeが引き起こすクラッシュ(#122で修正済み)はロールバック処理
            // (直前の値へのReplaceSelection/DeleteSelection)経由で、Returnのコード
            // パスとは無関係なため、Return/KeypadEnterはネイティブ処理に委ねてよい。
            if (evt.keyCode is KeyCode.Escape or KeyCode.Tab)
            {
                evt.StopPropagation();
            }
        }

        private void OnNavigationEventCapture(NavigationSubmitEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnNavigationEventCapture(NavigationCancelEvent evt)
        {
            evt.StopPropagation();
        }

        void IDisposable.Dispose()
        {
            if (_textField != null)
            {
                _textField.UnregisterValueChangedCallback(OnValueChanged);
                _textField.UnregisterCallback<FocusInEvent>(OnFocusIn);
                _textField.UnregisterCallback<FocusOutEvent>(OnFocusOut);
                _textField.UnregisterCallback<KeyDownEvent>(OnKeyDownCapture, TrickleDown.TrickleDown);
                _textField.UnregisterCallback<NavigationSubmitEvent>(OnNavigationEventCapture, TrickleDown.TrickleDown);
                _textField.UnregisterCallback<NavigationCancelEvent>(OnNavigationEventCapture, TrickleDown.TrickleDown);
            }

            OnInputTextChanged = null;
            OnFocusControlChanged = null;
            OnMoveCursorToEndTriggerChanged = null;
            OnImeComposingStateChanged = null;
        }
    }
}
#endif
