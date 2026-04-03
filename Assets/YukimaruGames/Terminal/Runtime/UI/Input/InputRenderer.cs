using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Core;
using YukimaruGames.Terminal.UI.Log;

namespace YukimaruGames.Terminal.UI.Input
{
    public sealed class InputRenderer : IInputRenderer, IPreRenderer
    {
        private readonly IScrollMutator _scrollMutator;
        private readonly IGUIStyleProvider _styleProvider;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly ICursorFlashSpeedProvider _cursorFlashSpeedProvider;

        private bool _isCurrentlyFocused;
        private bool _isMoveCursorToEnd;
        private string _inputField;
        private Focus _focus = Focus.None;
        private EventType _evt;
        private bool _isImeComposing;

        private int _id;
        private const string ControlName = "COMMAND_INPUT_CONTROL";

        public event Action<string> OnInputTextChanged;
        public event Action<Focus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        private Focus Focus
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
            ICursorFlashSpeedProvider cursorFlashSpeedProvider)
        {
            _scrollMutator = scrollMutator;
            _styleProvider = styleProvider;
            _colorPaletteProvider = colorPaletteProvider;
            _cursorFlashSpeedProvider = cursorFlashSpeedProvider;
        }

        void IPreRenderer.PreRender()
        {
            var evt = Event.current;
            if (UsedInputEvent(evt.type))
            {
                // Tabキー入力されると他のTextFieldにフォーカスが移ってしまうためフォーカスをコントロールする.
                if (evt.keyCode is KeyCode.Tab) GUI.FocusControl(ControlName);

                // Enterキーが入力されSubmitされると履歴のTextFieldにフォーカスが移ってしまうためフォーカスを補正する.
                if (evt.keyCode is KeyCode.Return) GUI.FocusControl(ControlName);

                // 入力テキストの折り返しを考慮しキー入力がされたらスクロール位置を終端へ補正する.
                _scrollMutator.ScrollToEnd();
            }
        }

        public void Render(InputRenderData data)
        {
            _id = GUIUtility.GetControlID(FocusType.Keyboard);
            _evt = Event.current.GetTypeForControl(_id);
            GUI.SetNextControlName(ControlName);
            _isCurrentlyFocused = GUI.GetNameOfFocusedControl() == ControlName;

            var cursorColor = GUI.skin.settings.cursorColor;
            var selectionColor = GUI.skin.settings.selectionColor;
            var cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;

            GUI.skin.settings.cursorColor = _colorPaletteProvider[Constants.ColorPalette.Cursor];
            GUI.skin.settings.selectionColor = _colorPaletteProvider[Constants.ColorPalette.Selection];
            GUI.skin.settings.cursorFlashSpeed = _cursorFlashSpeedProvider.FlashSpeed;

            InputText = GUILayout.TextField(data.InputText, _styleProvider.GetStyle());
            SendImeComposingState();

            GUI.skin.settings.cursorColor = cursorColor;
            GUI.skin.settings.selectionColor = selectionColor;
            GUI.skin.settings.cursorFlashSpeed = cursorFlashSpeed;

            _focus = data.Focus;
            _isMoveCursorToEnd = data.IsMoveCursorToEnd;

            FocusControlIfNeeded();
            CursorToEnd();
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        private void FocusControlIfNeeded()
        {
            if (Focus is Focus.None) return;

            switch (Focus)
            {
                case Focus.Apply:
                    if (!_isCurrentlyFocused)
                    {
                        GUI.FocusControl(ControlName);
                    }

                    break;
                case Focus.Release:
                    if (_isCurrentlyFocused)
                    {
                        GUI.FocusControl(null);
                    }

                    break;
            }

            GUI.changed = true;
            Focus = Focus.None;
        }

        private void CursorToEnd()
        {
            if (!_isCurrentlyFocused || !IsMoveCursorToEndTrigger) return;

            if (!UsedInputEvent(_evt)) return;

            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            textEditor!.MoveTextEnd();
            GUI.changed = true;
            IsMoveCursorToEndTrigger = false;
        }

        private void SendImeComposingState()
        {
            IsImeComposing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
        }

        private bool UsedInputEvent(EventType type) => type switch
        {
            EventType.KeyDown or EventType.KeyUp => true,
            EventType.MouseDown or EventType.MouseUp => true,
            EventType.MouseMove or EventType.MouseDrag => true,
            EventType.Used => true,
            _ => false
        };
    }
}