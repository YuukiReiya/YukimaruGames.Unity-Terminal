using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Adapters.GUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.GUI.Renderers
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
                if (evt.keyCode is KeyCode.Tab) UnityEngine.GUI.FocusControl(ControlName);

                // Enterキーが入力されSubmitされると履歴のTextFieldにフォーカスが移ってしまうためフォーカスを補正する.
                if (evt.keyCode is KeyCode.Return) UnityEngine.GUI.FocusControl(ControlName);

                // 入力テキストの折り返しを考慮しキー入力がされたらスクロール位置を終端へ補正する.
                _scrollMutator.ScrollToEnd();
            }
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
            _isMoveCursorToEnd = data.IsMoveCursorToEnd;

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

            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            textEditor!.MoveTextEnd();
            UnityEngine.GUI.changed = true;
            IsMoveCursorToEndTrigger = false;
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