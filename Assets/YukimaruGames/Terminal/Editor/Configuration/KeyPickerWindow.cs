using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// 専用Key・修飾キーの両方で共通して使う、キー選択用の汎用ポップアップ.
    /// 実際にキーを押して検出する方式と、検索して一覧から選ぶ方式の両方に対応する.
    /// 対象のenum SerializedPropertyの<see cref="SerializedProperty.enumDisplayNames"/>をそのまま
    /// 選択肢にするため、Key(InputSystem)/KeyCode(Legacy)どちらの型にも依存しない.
    /// </summary>
    /// <remarks>
    /// 当初は独立したEditorWindow(ShowAsDropDown)として実装したが、開いた直後にOnLostFocusが
    /// 発生して即座に閉じてしまい操作できない不具合が実機で確認された(猶予時間を設けても解消せず)。
    /// PopupWindowContent+PopupWindow.Showは、このプロジェクトで既に実績のある(以前の
    /// ModifierChoicePopupで同様の問題が起きなかった)Unity標準の軽量ポップアップ機構のため、
    /// こちらを採用する.
    /// </remarks>
    public sealed class KeyPickerPopupContent : PopupWindowContent
    {
        // Unityの物理キー入力イベント(Event.current.keyCode)はLegacyのKeyCode名で届く。
        // InputSystemのKey側は表示名の付け方が異なる項目があるため、名称の対応表で変換する.
        private static readonly Dictionary<string, string> KeyCodeNameToKeyDisplayName = new()
        {
            { "LeftControl", "Left Ctrl" },
            { "RightControl", "Right Ctrl" },
            { "LeftCommand", "Left Command" },
            { "RightCommand", "Right Command" },
            { "Return", "Enter" },
            { "Alpha0", "Digit 0" }, { "Alpha1", "Digit 1" }, { "Alpha2", "Digit 2" },
            { "Alpha3", "Digit 3" }, { "Alpha4", "Digit 4" }, { "Alpha5", "Digit 5" },
            { "Alpha6", "Digit 6" }, { "Alpha7", "Digit 7" }, { "Alpha8", "Digit 8" },
            { "Alpha9", "Digit 9" },
            { "UpArrow", "Up Arrow" }, { "DownArrow", "Down Arrow" },
            { "LeftArrow", "Left Arrow" }, { "RightArrow", "Right Arrow" },
            { "BackQuote", "Backquote" },
            { "Quote", "Quote" },
            { "LeftBracket", "Left Bracket" },
            { "RightBracket", "Right Bracket" },
            { "KeypadEnter", "Numpad Enter" },
            { "KeypadDivide", "Numpad Divide" },
            { "KeypadMultiply", "Numpad Multiply" },
            { "KeypadPlus", "Numpad Plus" },
            { "KeypadMinus", "Numpad Minus" },
            { "KeypadPeriod", "Numpad Period" },
            { "KeypadEquals", "Numpad Equals" },
            { "Keypad0", "Numpad 0" }, { "Keypad1", "Numpad 1" }, { "Keypad2", "Numpad 2" },
            { "Keypad3", "Numpad 3" }, { "Keypad4", "Numpad 4" }, { "Keypad5", "Numpad 5" },
            { "Keypad6", "Numpad 6" }, { "Keypad7", "Numpad 7" }, { "Keypad8", "Numpad 8" },
            { "Keypad9", "Numpad 9" },
            { "PageUp", "Page Up" }, { "PageDown", "Page Down" },
            { "CapsLock", "Caps Lock" }, { "Numlock", "Num Lock" }, { "ScrollLock", "Scroll Lock" },
            { "Print", "Print Screen" },
        };

        private const string SearchFieldControlName = "KeyPickerSearchField";

        private readonly SerializedProperty _targetProp;
        private readonly string[] _displayNames;
        private string _search = "";
        private Vector2 _scroll;
        private string _captureStatus = "";

        public KeyPickerPopupContent(SerializedProperty targetProp)
        {
            _targetProp = targetProp;
            _displayNames = targetProp.enumDisplayNames;
        }

        public override Vector2 GetWindowSize() => new(260f, 320f);

        public override void OnGUI(Rect rect)
        {
            HandleKeyCapture();

            using (new GUILayout.AreaScope(rect))
            {
                EditorGUILayout.Space(4f);
                var content = string.IsNullOrEmpty(_captureStatus) ? "⌨ Press any key..." : $"⌨ {_captureStatus}";
                // NOTE: 検索欄に一度フォーカスすると、クリックだけではフォーカスが外れず
                // キー押下検出モードに戻れなくなっていた(実際に発生した不具合)。
                // このエリア自体をクリック可能にし、検索欄のフォーカスを明示的に外す.
                if (GUILayout.Button(content, EditorStyles.helpBox, GUILayout.Height(24)))
                {
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("or search:", EditorStyles.miniLabel);
                GUI.SetNextControlName(SearchFieldControlName);
                _search = EditorGUILayout.TextField(_search);

                EditorGUILayout.Space(2f);
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (var i = 0; i < _displayNames.Length; ++i)
                {
                    var name = _displayNames[i];
                    if (!string.IsNullOrEmpty(_search) && name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (GUILayout.Button(name, EditorStyles.label))
                    {
                        Apply(i);
                        return;
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                Event.current.Use();
            }
        }

        private void HandleKeyCapture()
        {
            // 検索フィールドが入力フォーカスを持っている間は、通常のテキスト入力を優先し
            // キー押下検出を無効化する(そうしないと検索文字を打つたびにキーとして確定されてしまう).
            if (GUI.GetNameOfFocusedControl() == SearchFieldControlName) return;

            var e = Event.current;
            if (e.type != EventType.KeyDown || e.keyCode == KeyCode.None || e.keyCode == KeyCode.Escape) return;

            var codeName = e.keyCode.ToString();
            var targetName = KeyCodeNameToKeyDisplayName.TryGetValue(codeName, out var mapped)
                ? mapped
                : ObjectNames.NicifyVariableName(codeName);

            for (var i = 0; i < _displayNames.Length; ++i)
            {
                if (_displayNames[i] == targetName || _displayNames[i] == codeName)
                {
                    e.Use();
                    Apply(i);
                    return;
                }
            }

            _captureStatus = $"「{targetName}」は未対応のキーです";
            editorWindow.Repaint();
        }

        private void Apply(int enumValueIndex)
        {
            _targetProp.enumValueIndex = enumValueIndex;
            _targetProp.serializedObject.ApplyModifiedProperties();
            editorWindow.Close();
        }
    }
}
