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
            public string Suffix;
            public string Label;
        }

        // TerminalAction(None除く)と、InputSystemKey/LegacyInputKey/TerminalActionTriggerTimingが
        // 共通で使用しているフィールド名サフィックス.
        private static readonly ActionField[] Actions =
        {
            new() { Suffix = "open", Label = "Open" },
            new() { Suffix = "close", Label = "Close" },
            new() { Suffix = "execute", Label = "Execute" },
            new() { Suffix = "cancel", Label = "Cancel" },
            new() { Suffix = "previousHistory", Label = "Previous History" },
            new() { Suffix = "nextHistory", Label = "Next History" },
            new() { Suffix = "autocomplete", Label = "Autocomplete" },
            new() { Suffix = "focus", Label = "Focus" },
        };

        private const string TriggerTimingHelp =
            "Pressed: キーを押した瞬間に発火します。Released: キーを離した瞬間に発火します。\n" +
            "Open/Closeは既定でReleasedです(Pressedにすると、開閉に同じキーを割り当てた場合に" +
            "押した瞬間へ即座に反応してしまい、意図しない連続発火につながりやすいため)。";

        private const string PriorityHelp =
            "ドラッグして順序を入れ替えると、その順序がそのまま優先度になります(上ほど優先度が高い)。\n" +
            "複数のアクションの条件が同一フレームで同時に成立した場合、リストの上にあるアクションだけが発火します。";

        private static GUIStyle _typeStyle;
        private readonly Dictionary<string, ReorderableList> _priorityLists = new();

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

            var keyProp = keyboardType switch
            {
                InputKeyboardType.InputSystem => property.FindPropertyRelative("_inputSystemKey"),
                InputKeyboardType.Legacy => property.FindPropertyRelative("_legacyInputKey"),
                _ => null,
            };
            var keySuffix = keyboardType == InputKeyboardType.Legacy ? "KeyCode" : "Key";
            var modifierSuffix = keyboardType == InputKeyboardType.Legacy ? "ModifierKeyCodes" : "ModifierKeys";

            if (keyProp != null)
            {
                EditorGUILayout.LabelField("Keys", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var action in Actions)
                    {
                        DrawKeyRow(keyProp, action, keySuffix, modifierSuffix);
                    }
                }
                EditorGUILayout.Space(6f);
            }

            var timingProp = property.FindPropertyRelative("_triggerTiming");
            EditorGUILayout.LabelField("Trigger Timing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(TriggerTimingHelp, MessageType.Info);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var action in Actions)
                {
                    DrawTimingRow(timingProp, action);
                }
            }

            EditorGUILayout.Space(6f);

            var priorityProp = property.FindPropertyRelative("_priority");
            EditorGUILayout.LabelField("Priority", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(PriorityHelp, MessageType.Info);
            var orderProp = priorityProp?.FindPropertyRelative("_order");
            if (orderProp != null)
            {
                GetOrCreatePriorityList(orderProp).DoLayoutList();
            }

            EditorGUI.EndProperty();
        }

        private static void DrawKeyRow(SerializedProperty keyProp, ActionField action, string keySuffix, string modifierSuffix)
        {
            var key = keyProp.FindPropertyRelative("_" + action.Suffix + keySuffix);
            var modifiers = keyProp.FindPropertyRelative("_" + action.Suffix + modifierSuffix);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(action.Label, GUILayout.Width(110));
                if (key != null) EditorGUILayout.PropertyField(key, GUIContent.none);
            }
            if (modifiers != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(modifiers, new GUIContent("Modifiers"), true);
                }
            }
        }

        private static void DrawTimingRow(SerializedProperty timingProp, ActionField action)
        {
            var prop = timingProp?.FindPropertyRelative("_" + action.Suffix);
            if (prop == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(action.Label, GUILayout.Width(110));
                EditorGUILayout.PropertyField(prop, GUIContent.none);
            }
        }

        private ReorderableList GetOrCreatePriorityList(SerializedProperty orderProp)
        {
            if (_priorityLists.TryGetValue(orderProp.propertyPath, out var cached))
            {
                cached.serializedProperty = orderProp;
                return cached;
            }

            var list = new ReorderableList(orderProp.serializedObject, orderProp, true, false, false, false)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 2f,
            };
            // NOTE: クロージャで直接orderPropを参照すると、次回OnGUI時にSerializedObjectが
            // Disposedになった古いプロパティを参照し続けてしまう。必ずlist.serializedPropertyの
            // (毎回最新に更新される)方を経由して参照すること.
            list.drawElementCallback = (rect, index, _, _) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                var action = (TerminalAction)element.intValue;
                rect.y += 1f;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, $"{index + 1}. {ObjectNames.NicifyVariableName(action.ToString())}");
            };

            _priorityLists[orderProp.propertyPath] = list;
            return list;
        }

        private static void InitStyles()
        {
            if (_typeStyle != null) return;
            _typeStyle = new GUIStyle(EditorStyles.radioButton) { alignment = TextAnchor.MiddleCenter };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f; // GUILayoutを使う場合は0を返して隙間を詰めさせることが多いです
        }
    }
}
