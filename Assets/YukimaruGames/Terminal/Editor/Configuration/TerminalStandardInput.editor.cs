using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using YukimaruGames.Terminal.Composition;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(TerminalStandardInput))]
    public sealed class TerminalStandardInputDrawer : PropertyDrawer
    {
        private struct ActionField
        {
            public TerminalAction Action;
            public string Suffix;
            public string Label;
        }

        // TerminalAction(None除く)と、InputSystemKey/LegacyInputKey/TerminalActionTriggerTimingが
        // 共通で使用しているフィールド名サフィックス.
        private static readonly ActionField[] Actions =
        {
            new() { Action = TerminalAction.Open, Suffix = "open", Label = "Open" },
            new() { Action = TerminalAction.Close, Suffix = "close", Label = "Close" },
            new() { Action = TerminalAction.Execute, Suffix = "execute", Label = "Execute" },
            new() { Action = TerminalAction.Cancel, Suffix = "cancel", Label = "Cancel" },
            new() { Action = TerminalAction.PreviousHistory, Suffix = "previousHistory", Label = "Previous History" },
            new() { Action = TerminalAction.NextHistory, Suffix = "nextHistory", Label = "Next History" },
            new() { Action = TerminalAction.Autocomplete, Suffix = "autocomplete", Label = "Autocomplete" },
            new() { Action = TerminalAction.Focus, Suffix = "focus", Label = "Focus" },
        };

        private const float StepperWidth = 24f;
        private const float CircleSize = 16f;

        private static GUIStyle _typeStyle;
        private static GUIStyle _stepNumberStyle;
        private static GUIStyle _modifierValueStyle;
        private static GUIStyle _modifierRemoveStyle;
        private readonly Dictionary<string, ReorderableList> _actionLists = new();

        // ReorderableListのコールバックはlist生成時に一度だけ作られキャッシュされるため、
        // 描画対象のSerializedPropertyをローカル変数としてクロージャに直接キャプチャしてはならない
        // (次回OnGUI時にSerializedObjectがDisposedになった古い参照を握り続けてしまう)。
        // 代わりにOnGUIの先頭でこれらのインスタンスフィールドを都度更新し、コールバック内では
        // 常に最新の値を読むフィールド経由でアクセスする.
        private SerializedProperty _activeKeyProp;
        private SerializedProperty _activeTimingProp;
        private string _activeKeySuffix;
        private string _activeModifierSuffix;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;

            InitStyles();

            label = EditorGUI.BeginProperty(position, label, property);

            var keyboardTypeProp = property.FindPropertyRelative("_inputKeyboardType");
            var keyboardType = (InputKeyboardType)keyboardTypeProp.intValue;

            EditorGUILayout.LabelField("Keyboard Type", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (InputKeyboardType type in Enum.GetValues(typeof(InputKeyboardType)))
                {
                    var isSelected = GUILayout.Toggle(keyboardType == type, type.ToString(), _typeStyle);
                    if (isSelected && keyboardType != type)
                    {
                        keyboardTypeProp.intValue = (int)type;
                    }
                }
            }

            EditorGUILayout.Space(6f);

            _activeKeyProp = keyboardType switch
            {
                InputKeyboardType.InputSystem => property.FindPropertyRelative("_inputSystemKey"),
                InputKeyboardType.Legacy => property.FindPropertyRelative("_legacyInputKey"),
                _ => null,
            };
            _activeTimingProp = property.FindPropertyRelative("_triggerTiming");
            _activeKeySuffix = keyboardType == InputKeyboardType.Legacy ? "KeyCode" : "Key";
            _activeModifierSuffix = keyboardType == InputKeyboardType.Legacy ? "ModifierKeyCodes" : "ModifierKeys";

            var priorityProp = property.FindPropertyRelative("_priority");
            var orderProp = priorityProp?.FindPropertyRelative("_order");

            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("優先度: 上ほど高い(ドラッグで並び替え)", EditorStyles.miniLabel);

            if (orderProp != null)
            {
                GetOrCreateActionList(orderProp).DoLayoutList();
            }

            EditorGUI.EndProperty();
        }

        private ActionField GetActionField(TerminalAction action)
        {
            foreach (var field in Actions)
            {
                if (field.Action == action) return field;
            }
            return default;
        }

        private ReorderableList GetOrCreateActionList(SerializedProperty orderProp)
        {
            if (_actionLists.TryGetValue(orderProp.propertyPath, out var cached))
            {
                cached.serializedProperty = orderProp;
                return cached;
            }

            var list = new ReorderableList(orderProp.serializedObject, orderProp, true, false, false, false);
            list.elementHeightCallback = index => GetElementHeight(list, index);
            list.drawElementCallback = (rect, index, _, _) => DrawElement(list, rect, index);

            _actionLists[orderProp.propertyPath] = list;
            return list;
        }

        private float GetElementHeight(ReorderableList list, int index)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var action = (TerminalAction)element.intValue;
            var field = GetActionField(action);

            var height = EditorGUIUtility.singleLineHeight + 6f;

            var modifiersProp = _activeKeyProp?.FindPropertyRelative("_" + field.Suffix + _activeModifierSuffix);
            if (modifiersProp != null)
            {
                height += EditorGUIUtility.singleLineHeight + 2f; // 要約テキスト行
                height += GetKeyTableHeight(modifiersProp) + 2f;

                var error = GetModifierValidationError(modifiersProp);
                if (error != null)
                {
                    height += GetHelpBoxHeight(error, ModifierValueWidth + ModifierRemoveWidth) + 2f;
                }
            }

            return height;
        }

        private void DrawElement(ReorderableList list, Rect rect, int index)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var action = (TerminalAction)element.intValue;
            var field = GetActionField(action);

            var keyProp = _activeKeyProp?.FindPropertyRelative("_" + field.Suffix + _activeKeySuffix);
            var modifiersProp = _activeKeyProp?.FindPropertyRelative("_" + field.Suffix + _activeModifierSuffix);
            var timingProp = _activeTimingProp?.FindPropertyRelative("_" + field.Suffix);

            DrawStepper(rect, index, list.count);

            var contentX = rect.x + StepperWidth;
            var contentWidth = rect.width - StepperWidth;
            var lineRect = new Rect(contentX, rect.y + 3f, contentWidth, EditorGUIUtility.singleLineHeight);

            const float labelWidth = 100f;
            const float spacing = 4f;
            const float segmentWidth = 150f;
            var segmentRect = new Rect(lineRect.xMax - segmentWidth, lineRect.y, segmentWidth, lineRect.height);
            var labelRect = new Rect(lineRect.x, lineRect.y, labelWidth, lineRect.height);

            EditorGUI.LabelField(labelRect, field.Label);
            if (timingProp != null) DrawTimingSegment(segmentRect, timingProp);

            if (modifiersProp != null)
            {
                var summaryRect = new Rect(contentX, lineRect.yMax + 2f, contentWidth, EditorGUIUtility.singleLineHeight);
                var summary = GetCombinedKeySummary(keyProp, modifiersProp);
                EditorGUI.LabelField(summaryRect, summary, EditorStyles.miniLabel);

                var tableRect = new Rect(contentX, summaryRect.yMax + 2f, contentWidth, 0f);
                var tableBottomY = DrawKeyTable(tableRect, keyProp, modifiersProp);

                var error = GetModifierValidationError(modifiersProp);
                if (error != null)
                {
                    var tableWidth = ModifierValueWidth + ModifierRemoveWidth;
                    var errorHeight = GetHelpBoxHeight(error, tableWidth);
                    var errorRect = new Rect(contentX, tableBottomY + 2f, tableWidth, errorHeight);
                    EditorGUI.HelpBox(errorRect, error, MessageType.Error);
                }
            }
        }

        // Modifiers配列内に同じキーが重複していないか検証する(意味のない設定を検知する).
        private static string GetModifierValidationError(SerializedProperty arrayProp)
        {
            for (var i = 0; i < arrayProp.arraySize; ++i)
            {
                var a = arrayProp.GetArrayElementAtIndex(i).enumValueIndex;
                for (var j = i + 1; j < arrayProp.arraySize; ++j)
                {
                    if (arrayProp.GetArrayElementAtIndex(j).enumValueIndex == a)
                    {
                        var name = GetEnumDisplayName(arrayProp.GetArrayElementAtIndex(i));
                        return $"「{name}」が重複して設定されています。同じキーは1つだけ設定してください。";
                    }
                }
            }
            return null;
        }

        private static float GetHelpBoxHeight(string text, float width)
        {
            var style = GUI.skin.GetStyle("helpbox");
            return Mathf.Max(style.CalcHeight(new GUIContent(text), width), EditorGUIUtility.singleLineHeight * 2f);
        }

        private static string GetCombinedKeySummary(SerializedProperty keyProp, SerializedProperty modifiersProp)
        {
            var keyName = keyProp != null ? GetEnumDisplayName(keyProp) : "?";

            if (modifiersProp == null || modifiersProp.arraySize == 0) return keyName;

            var parts = new List<string>(modifiersProp.arraySize + 1);
            for (var i = 0; i < modifiersProp.arraySize; ++i)
            {
                parts.Add(GetEnumDisplayName(modifiersProp.GetArrayElementAtIndex(i)));
            }
            parts.Add(keyName);
            return string.Join(" + ", parts);
        }

        private const float ModifierRowHeight = 18f;
        private const float ModifierHeaderHeight = 18f;
        private const float ModifierValueWidth = 200f;
        private const float ModifierRemoveWidth = 32f;

        // プライマリKey(専用キー) + Modifiers(修飾キー)をまとめて1つの罫線付きの表
        // (ヘッダー行 + Key行(削除不可) + 修飾キー行(削除可) + 追加行)として描画する。
        // 専用キーもテーブルの行として扱うことで、上の要約行のような固定幅の窮屈さを回避できる。
        // 列の折り返し計算が不要になり、計測(GetElementHeight)と描画(DrawElement)が
        // 常に同じ行数になることを幅に依存せず保証できる.
        private static float GetKeyTableHeight(SerializedProperty modifiersProp)
        {
            var dataRows = 1 + modifiersProp.arraySize + 1; // Key行 + 各修飾キー行 + 追加行
            return ModifierHeaderHeight + dataRows * ModifierRowHeight;
        }

        private static float DrawKeyTable(Rect rect, SerializedProperty keyProp, SerializedProperty modifiersProp)
        {
            var tableWidth = ModifierValueWidth + ModifierRemoveWidth;
            var dataRowCount = 1 + modifiersProp.arraySize + 1; // Key行 + 各修飾キー行 + 追加行
            var tableHeight = ModifierHeaderHeight + dataRowCount * ModifierRowHeight;
            var tableRect = new Rect(rect.x, rect.y, tableWidth, tableHeight);

            if (Event.current.type == EventType.Repaint)
            {
                DrawTableChrome(tableRect, dataRowCount);
            }

            var headerRect = new Rect(tableRect.x, tableRect.y, tableWidth, ModifierHeaderHeight);
            EditorGUI.LabelField(
                new Rect(headerRect.x + 4f, headerRect.y, ModifierValueWidth - 4f, headerRect.height),
                "Key", EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + ModifierValueWidth, headerRect.y, ModifierRemoveWidth, headerRect.height),
                "Del", EditorStyles.boldLabel);

            var y = tableRect.y + ModifierHeaderHeight;

            // Key行(専用キー。削除不可。KeyPickerPopupContentで検索/実キー押下のどちらでも選べる).
            if (keyProp != null)
            {
                var keyValueRect = new Rect(tableRect.x + 2f, y + 1f, ModifierValueWidth - 4f, ModifierRowHeight - 2f);
                if (GUI.Button(keyValueRect, GetEnumDisplayName(keyProp), _modifierValueStyle))
                {
                    PopupWindow.Show(keyValueRect, new KeyPickerPopupContent(keyProp));
                }
            }
            y += ModifierRowHeight;

            // 修飾キー行(削除可).
            for (var i = 0; i < modifiersProp.arraySize; ++i)
            {
                var elementProp = modifiersProp.GetArrayElementAtIndex(i);
                var valueRect = new Rect(tableRect.x + 2f, y + 1f, ModifierValueWidth - 4f, ModifierRowHeight - 2f);
                var removeRect = new Rect(tableRect.x + ModifierValueWidth + 1f, y + 1f, ModifierRemoveWidth - 3f, ModifierRowHeight - 2f);
                var rowIndex = i;

                if (GUI.Button(valueRect, GetEnumDisplayName(elementProp), _modifierValueStyle))
                {
                    PopupWindow.Show(valueRect, new KeyPickerPopupContent(modifiersProp.GetArrayElementAtIndex(rowIndex)));
                }
                if (GUI.Button(removeRect, "×", _modifierRemoveStyle))
                {
                    // NOTE: 配列サイズの変更(行数が変わる=要素の高さが変わる)を伴う操作の直後は、
                    // ExitGUI()でこのフレームのGUI処理を即座に中断し、次フレームで必ず新しい
                    // Layoutパスからやり直させる。そうしないと、既にLayoutパスで確定していた
                    // ReorderableListの後続要素の描画位置と、Repaintパスで実際に変化した高さが
                    // 食い違い、次の要素のデザインに重なって見える不具合が発生する
                    // (実際に発生した不具合)。ここは(ポップアップ内と異なり)通常のInspector描画
                    // コンテキストなのでExitGUIは安全.
                    modifiersProp.DeleteArrayElementAtIndex(i);
                    modifiersProp.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                y += ModifierRowHeight;
            }

            var addRect = new Rect(tableRect.x + 2f, y + 1f, tableWidth - 4f, ModifierRowHeight - 2f);
            if (GUI.Button(addRect, "+ Add", EditorStyles.label))
            {
                var newIndex = modifiersProp.arraySize;
                modifiersProp.InsertArrayElementAtIndex(newIndex);
                modifiersProp.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }

            return tableRect.yMax;
        }

        private static void DrawTableChrome(Rect tableRect, int dataRowCount)
        {
            var borderColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.55f, 0.55f, 0.55f);
            var headerColor = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f) : new Color(0.72f, 0.72f, 0.72f);
            var rowColorA = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.82f, 0.82f, 0.82f);
            var rowColorB = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.78f, 0.78f, 0.78f);
            var addRowColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.85f, 0.85f, 0.85f);

            // ヘッダー行の背景
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, tableRect.width, ModifierHeaderHeight), headerColor);

            // データ行の背景(交互。最後の1行(追加行)だけ専用の色にする).
            var y = tableRect.y + ModifierHeaderHeight;
            for (var i = 0; i < dataRowCount; ++i)
            {
                var isAddRow = i == dataRowCount - 1;
                var rowColor = isAddRow ? addRowColor : (i % 2 == 0 ? rowColorA : rowColorB);
                EditorGUI.DrawRect(new Rect(tableRect.x, y, tableRect.width, ModifierRowHeight), rowColor);
                y += ModifierRowHeight;
            }

            // 横罫線(ヘッダー下 + 各行の下 + 外枠上下)
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, tableRect.width, 1f), borderColor);
            var lineY = tableRect.y + ModifierHeaderHeight;
            for (var i = 0; i <= dataRowCount; ++i)
            {
                EditorGUI.DrawRect(new Rect(tableRect.x, lineY, tableRect.width, 1f), borderColor);
                lineY += ModifierRowHeight;
            }
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.yMax - 1f, tableRect.width, 1f), borderColor);

            // 縦罫線(外枠左右 + Modifier/削除列の間)
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, 1f, tableRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(tableRect.x + ModifierValueWidth, tableRect.y, 1f, tableRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(tableRect.xMax - 1f, tableRect.y, 1f, tableRect.height), borderColor);
        }

        private static string GetEnumDisplayName(SerializedProperty enumProp)
        {
            var names = enumProp.enumDisplayNames;
            var index = enumProp.enumValueIndex;
            return index >= 0 && index < names.Length ? names[index] : "?";
        }

        private static void DrawStepper(Rect rect, int index, int count)
        {
            if (Event.current.type != EventType.Repaint) return;

            var lineX = rect.x + StepperWidth * 0.5f;
            var lineColor = EditorGUIUtility.isProSkin ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.6f, 0.6f, 0.6f);

            if (index > 0)
            {
                EditorGUI.DrawRect(new Rect(lineX - 1f, rect.y, 2f, CircleSize * 0.5f + 4f), lineColor);
            }
            if (index < count - 1)
            {
                var bottomStart = rect.y + CircleSize * 0.5f + 4f;
                EditorGUI.DrawRect(new Rect(lineX - 1f, bottomStart, 2f, rect.height - (bottomStart - rect.y)), lineColor);
            }

            var circleRect = new Rect(rect.x + (StepperWidth - CircleSize) * 0.5f, rect.y + 4f, CircleSize, CircleSize);
            var circleColor = EditorGUIUtility.isProSkin ? new Color(0.29f, 0.56f, 0.76f) : new Color(0.20f, 0.45f, 0.65f);
            EditorGUI.DrawRect(circleRect, circleColor);
            GUI.Label(circleRect, (index + 1).ToString(), _stepNumberStyle);
        }

        private static void DrawTimingSegment(Rect rect, SerializedProperty timingProp)
        {
            var current = (TerminalActionTriggerTiming.Timing)timingProp.intValue;
            var pressedRect = new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height);
            var releasedRect = new Rect(pressedRect.xMax, rect.y, rect.width - pressedRect.width, rect.height);

            var pressedOn = GUI.Toggle(pressedRect, current == TerminalActionTriggerTiming.Timing.Pressed, "Pressed", EditorStyles.miniButtonLeft);
            var releasedOn = GUI.Toggle(releasedRect, current == TerminalActionTriggerTiming.Timing.Released, "Released", EditorStyles.miniButtonRight);

            if (pressedOn && current != TerminalActionTriggerTiming.Timing.Pressed)
            {
                timingProp.intValue = (int)TerminalActionTriggerTiming.Timing.Pressed;
            }
            else if (releasedOn && current != TerminalActionTriggerTiming.Timing.Released)
            {
                timingProp.intValue = (int)TerminalActionTriggerTiming.Timing.Released;
            }
        }

        private static void InitStyles()
        {
            if (_typeStyle == null)
            {
                _typeStyle = new GUIStyle(EditorStyles.radioButton) { alignment = TextAnchor.MiddleCenter };
            }
            if (_stepNumberStyle == null)
            {
                _stepNumberStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };
            }
            if (_modifierValueStyle == null)
            {
                _modifierValueStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };
            }
            if (_modifierRemoveStyle == null)
            {
                _modifierRemoveStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f; // GUILayoutを使う場合は0を返して隙間を詰めさせることが多いです
        }
    }
}
