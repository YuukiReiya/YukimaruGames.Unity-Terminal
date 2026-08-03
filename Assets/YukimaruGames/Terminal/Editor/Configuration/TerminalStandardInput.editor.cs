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
                height += EditorGUI.GetPropertyHeight(modifiersProp, true) + 2f;
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

            const float labelWidth = 110f;
            const float spacing = 4f;
            const float segmentWidth = 150f;
            var keyWidth = Mathf.Max(lineRect.width - labelWidth - segmentWidth - spacing * 2f, 40f);

            var labelRect = new Rect(lineRect.x, lineRect.y, labelWidth, lineRect.height);
            var keyRect = new Rect(labelRect.xMax + spacing, lineRect.y, keyWidth, lineRect.height);
            var segmentRect = new Rect(lineRect.xMax - segmentWidth, lineRect.y, segmentWidth, lineRect.height);

            EditorGUI.LabelField(labelRect, field.Label);
            if (keyProp != null) EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
            if (timingProp != null) DrawTimingSegment(segmentRect, timingProp);

            if (modifiersProp != null)
            {
                var modifiersHeight = EditorGUI.GetPropertyHeight(modifiersProp, true);
                var modifiersRect = new Rect(contentX, lineRect.yMax + 2f, contentWidth, modifiersHeight);
                EditorGUI.PropertyField(modifiersRect, modifiersProp, new GUIContent("Modifiers"), true);
            }
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
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f; // GUILayoutを使う場合は0を返して隙間を詰めさせることが多いです
        }
    }
}
