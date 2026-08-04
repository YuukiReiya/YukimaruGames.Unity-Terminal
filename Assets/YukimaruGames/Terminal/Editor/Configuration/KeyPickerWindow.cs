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

        // Unityの物理修飾キー単体の押下(NSEventのflagsChanged相当)は、KeyDown/KeyUpイベントとして
        // 届かないことがある(実機で確認済み)。そのためEvent.current.modifiersを毎フレーム比較して
        // 立ち上がりエッジを検出する方式で補う。ただしEventModifiersはLeft/Rightを区別できないため、
        // 両方の候補が存在するキー種別は選択肢を提示してユーザーに選んでもらう.
        private static readonly (EventModifiers Flag, string Generic)[] ModifierFlags =
        {
            (EventModifiers.Shift, "Shift"),
            (EventModifiers.Control, "Ctrl"),
            (EventModifiers.Alt, "Alt"),
            (EventModifiers.Command, "Command"),
        };

        private const string SearchFieldControlName = "KeyPickerSearchField";

        private const float WindowWidth = 260f;
        private const float WindowHeight = 320f;
        private const float CaptureButtonHeight = 24f;
        private const float SpaceSmall = 2f;
        private const float SpaceMedium = 4f;

        private readonly SerializedProperty _targetProp;
        private readonly string[] _displayNames;
        private string _search = "";
        private Vector2 _scroll;
        private string _captureStatus = "";
        private EventModifiers _prevModifiers = EventModifiers.None;
        private List<int> _ambiguousCandidates;

        public KeyPickerPopupContent(SerializedProperty targetProp)
        {
            _targetProp = targetProp;
            _displayNames = targetProp.enumDisplayNames;
        }

        /// <inheritdoc />
        public override Vector2 GetWindowSize() => new(WindowWidth, WindowHeight);

        /// <inheritdoc />
        public override void OnOpen()
        {
            // 修飾キー単体の押下はマウス移動等のイベントを伴わないと検知できないため、
            // ウィンドウが開いている間は強制的に再描画し続けてポーリングする.
            EditorApplication.update += RequestContinuousRepaint;
        }

        /// <inheritdoc />
        public override void OnClose()
        {
            EditorApplication.update -= RequestContinuousRepaint;
        }

        private void RequestContinuousRepaint()
        {
            // NOTE: 破棄済みのUnityオブジェクトは参照としてはnullでなくなる(pseudo-null)ため、
            // ?.ではなく==nullでの判定が必要.
            if (editorWindow != null)
            {
                editorWindow.Repaint();
            }
        }

        public override void OnGUI(Rect rect)
        {
            HandleKeyCapture();

            using (new GUILayout.AreaScope(rect))
            {
                EditorGUILayout.Space(SpaceMedium);
                var content = string.IsNullOrEmpty(_captureStatus) ? "⌨ Press any key..." : $"⌨ {_captureStatus}";
                // NOTE: 検索欄に一度フォーカスすると、クリックだけではフォーカスが外れず
                // キー押下検出モードに戻れなくなっていた(実際に発生した不具合)。
                // このエリア自体をクリック可能にし、検索欄のフォーカスを明示的に外す.
                if (GUILayout.Button(content, EditorStyles.helpBox, GUILayout.Height(CaptureButtonHeight)))
                {
                    GUI.FocusControl(null);
                    _ambiguousCandidates = null;
                }

                if (_ambiguousCandidates is { Count: > 0 })
                {
                    EditorGUILayout.BeginHorizontal();
                    foreach (var candidate in _ambiguousCandidates)
                    {
                        if (GUILayout.Button(_displayNames[candidate]))
                        {
                            Apply(candidate);
                            return;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(SpaceMedium);
                EditorGUILayout.LabelField("or search:", EditorStyles.miniLabel);
                GUI.SetNextControlName(SearchFieldControlName);
                _search = EditorGUILayout.TextField(_search);

                EditorGUILayout.Space(SpaceSmall);
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

            // Escapeはキー検出モードでは通常のキーとして選択できるようにしたいため、
            // 「Escapeでウィンドウを閉じる」動作は検索フィールドにフォーカスがある間だけに限定する.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape
                && GUI.GetNameOfFocusedControl() == SearchFieldControlName)
            {
                editorWindow.Close();
                Event.current.Use();
            }
        }

        private void HandleKeyCapture()
        {
            var e = Event.current;

            // 修飾キー単体の押下エッジ検出は、検索フィールドでのCtrl+A等のテキスト編集ショートカットと
            // 衝突しないよう、フォーカス判定より先に「今回新たに立った修飾ビット」を記録しておく
            // (フォーカス中でも_prevModifiersの更新自体は継続しないと、フォーカスを外した直後に
            // 実際には押されていないキーが誤検出されてしまう).
            var justPressedModifiers = e.modifiers & ~_prevModifiers;
            _prevModifiers = e.modifiers;

            // 検索フィールドが入力フォーカスを持っている間は、通常のテキスト入力を優先し
            // キー押下検出を無効化する(そうしないと検索文字を打つたびにキーとして確定されてしまう).
            if (GUI.GetNameOfFocusedControl() == SearchFieldControlName) return;

            // NOTE: 修飾キー単体(Shift/Ctrl/Alt/Cmdなど)は、macOSのCocoaキーイベント処理の都合で
            // KeyDown/KeyUpイベントとして届かないことがある(実機で確認済み)ため、通常のキーコード方式
            // に加えて、Event.current.modifiersの立ち上がりエッジでも検出する.
            if ((e.type == EventType.KeyDown || e.type == EventType.KeyUp) && e.keyCode != KeyCode.None)
            {
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
                return;
            }

            if (justPressedModifiers != EventModifiers.None)
            {
                TryCaptureModifier(justPressedModifiers);
            }
        }

        private void TryCaptureModifier(EventModifiers justPressed)
        {
            foreach (var (flag, generic) in ModifierFlags)
            {
                if ((justPressed & flag) == 0) continue;

                var candidates = new List<int>();
                for (var i = 0; i < _displayNames.Length; ++i)
                {
                    var name = _displayNames[i];
                    if (name == "Left " + generic || name == "Right " + generic || name == generic)
                    {
                        candidates.Add(i);
                    }
                }

                if (candidates.Count == 1)
                {
                    Apply(candidates[0]);
                    return;
                }

                if (candidates.Count > 1)
                {
                    // Left/Right両方の候補があり、EventModifiersだけではどちらが押されたか判別できない.
                    // ユーザーに選んでもらう.
                    _ambiguousCandidates = candidates;
                    _captureStatus = $"{generic}: Left/Rightのどちらか選んでください";
                    editorWindow.Repaint();
                    return;
                }
            }
        }

        private void Apply(int enumValueIndex)
        {
            _targetProp.enumValueIndex = enumValueIndex;
            _targetProp.serializedObject.ApplyModifiedProperties();
            editorWindow.Close();
        }
    }
}
