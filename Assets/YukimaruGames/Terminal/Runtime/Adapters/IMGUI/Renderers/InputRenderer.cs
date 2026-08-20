using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Adapters.IMGUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Renderers
{
    /// <summary>
    /// IMGUIによる入力欄の描画と、入力イベントの通知を行う.
    /// </summary>
    public sealed class InputRenderer : IInputRenderer, IInputProvider, IPreRenderer
    {
        private readonly IScrollMutator _scrollMutator;
        private readonly IGUIStyleProvider _styleProvider;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly CursorView _cursorView;

        private bool _isCurrentlyFocused;
        private bool _isMoveCursorToEnd;

        /// <summary>
        /// このフレームでキー入力を受けて入力欄へフォーカスを戻したか.
        /// </summary>
        /// <remarks>
        /// IMGUIのTextFieldはフォーカスを得た瞬間に<b>テキスト全体を選択する</b>。
        /// フォーカスを戻す処理を入れている都合上、そのままではTabやEnterのたびに
        /// 入力中の文字列が選択状態になり、次の1文字で全消えする(#16)。
        /// 選択を解除してキャレットを末尾へ戻すため、描画側へ持ち越す.
        /// </remarks>
        private bool _refocusedByKeyInput;
        private string _inputField;
        private WindowFocus _focus = WindowFocus.None;
        private EventType _evt;
        private bool _isImeComposing;

        private int _id;
        private const string ControlName = "COMMAND_INPUT_CONTROL";

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        private WindowFocus Focus
        {
            get => _focus;
            set
            {
                if (_focus == value) return;

                _focus = value;
                OnFocusControlChanged?.Invoke(value);
            }
        }

        private bool IsMoveCursorToEndTrigger
        {
            get => _isMoveCursorToEnd;
            set
            {
                if (_isMoveCursorToEnd == value) return;

                _isMoveCursorToEnd = value;
                OnMoveCursorToEndTriggerChanged?.Invoke(value);
            }
        }

        private bool IsImeComposing
        {
            set
            {
                if (_isImeComposing == value) return;
                _isImeComposing = value;
                OnImeComposingStateChanged?.Invoke(value);
            }
        }

        public string InputText
        {
            get => _inputField;
            private set
            {
                if (_inputField == value) return;

                _inputField = value;
                OnInputTextChanged?.Invoke(value);
            }
        }

        public InputRenderer(
            IScrollMutator scrollMutator,
            IGUIStyleProvider styleProvider,
            IColorPaletteProvider colorPaletteProvider,
            CursorView cursorView)
        {
            _scrollMutator = scrollMutator;
            _styleProvider = styleProvider;
            _colorPaletteProvider = colorPaletteProvider;
            _cursorView = cursorView;
        }

        void IPreRenderer.PreRender()
        {
            var evt = Event.current;
            if (UsedInputEvent(evt.type))
            {
                // Tabキー入力されると他のTextFieldにフォーカスが移ってしまうためフォーカスをコントロールする.
                if (evt.keyCode is KeyCode.Tab) RefocusControl();

                // Enterキーが入力されSubmitされると履歴のTextFieldにフォーカスが移ってしまうためフォーカスを補正する.
                if (evt.keyCode is KeyCode.Return) RefocusControl();

                // 入力テキストの折り返しを考慮しキー入力がされたらスクロール位置を終端へ補正する.
                _scrollMutator.ScrollToEnd();
            }
        }

        /// <summary>
        /// 入力欄へフォーカスを戻し、選択状態の解除を予約する.
        /// </summary>
        /// <remarks>
        /// フォーカスを戻すだけだと、IMGUIの仕様で入力中の文字列が全選択された状態になる。
        /// 描画後にキャレットを末尾へ移して選択を解除する(#16).
        /// </remarks>
        private void RefocusControl()
        {
            UnityEngine.GUI.FocusControl(ControlName);
            _refocusedByKeyInput = true;
        }

        public void Render(InputRenderData data)
        {
            _id = GUIUtility.GetControlID(FocusType.Keyboard);
            _evt = Event.current.GetTypeForControl(_id);
            UnityEngine.GUI.SetNextControlName(ControlName);
            _isCurrentlyFocused = UnityEngine.GUI.GetNameOfFocusedControl() == ControlName;

            var cursorColor = UnityEngine.GUI.skin.settings.cursorColor;
            var selectionColor = UnityEngine.GUI.skin.settings.selectionColor;
            var cursorFlashSpeed = UnityEngine.GUI.skin.settings.cursorFlashSpeed;

            var nextCursorColor = _colorPaletteProvider[Definitions.ThemeLabel.Cursor];
            if (_cursorView is { IsVisible: false })
            {
                nextCursorColor.a = 0f;
            }

            try
            {
                UnityEngine.GUI.skin.settings.cursorColor = nextCursorColor;
                UnityEngine.GUI.skin.settings.selectionColor = _colorPaletteProvider[Definitions.ThemeLabel.Selection];
                // カーソルの点滅はCursorPresenter/CursorViewが管理するため、ネイティブの点滅は無効化する.
                UnityEngine.GUI.skin.settings.cursorFlashSpeed = 0f;

                InputText = GUILayout.TextField(data.InputText, _styleProvider.GetStyle());
                SendImeComposingState();
            }
            finally
            {
                UnityEngine.GUI.skin.settings.cursorColor = cursorColor;
                UnityEngine.GUI.skin.settings.selectionColor = selectionColor;
                UnityEngine.GUI.skin.settings.cursorFlashSpeed = cursorFlashSpeed;
            }

            _focus = data.Focus;

            // キー入力でフォーカスを戻した場合も、選択解除のためにキャレットを末尾へ動かす。
            // dataの値で上書きすると、その要求が消えてしまう.
            _isMoveCursorToEnd = data.IsMoveCursorToEnd || _refocusedByKeyInput;

            FocusControlIfNeeded();
            CursorToEnd();
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        private void FocusControlIfNeeded()
        {
            if (Focus is WindowFocus.None) return;

            switch (Focus)
            {
                case WindowFocus.Apply:
                    if (!_isCurrentlyFocused)
                    {
                        UnityEngine.GUI.FocusControl(ControlName);
                    }

                    break;
                case WindowFocus.Release:
                    if (_isCurrentlyFocused)
                    {
                        UnityEngine.GUI.FocusControl(null);
                    }

                    break;
            }

            UnityEngine.GUI.changed = true;
            Focus = WindowFocus.None;
        }

        private void CursorToEnd()
        {
            if (!_isCurrentlyFocused || !IsMoveCursorToEndTrigger) return;

            if (!UsedInputEvent(_evt)) return;

            // MoveTextEndはキャレットと選択の開始位置を揃えるため、これで全選択が解除される.
            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            textEditor!.MoveTextEnd();
            UnityEngine.GUI.changed = true;
            IsMoveCursorToEndTrigger = false;
            _refocusedByKeyInput = false;
        }

        private void SendImeComposingState()
        {
            IsImeComposing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
        }

        private static bool UsedInputEvent(EventType type) => type switch
        {
            EventType.KeyDown or EventType.KeyUp => true,
            EventType.MouseDown or EventType.MouseUp => true,
            EventType.MouseMove or EventType.MouseDrag => true,
            EventType.Used => true,
            _ => false
        };
    }
}