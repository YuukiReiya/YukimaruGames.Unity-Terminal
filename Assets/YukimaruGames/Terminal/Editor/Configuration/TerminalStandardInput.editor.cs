using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

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

        // TerminalAction(None除く)と、InputSystemKey/LegacyInputKey/TerminalActionTriggerTiming/
        // TerminalActionPriorityが共通で使用しているフィールド名サフィックス.
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

        private static GUIStyle _typeStyle;
        private readonly Dictionary<string, bool> _foldouts = new();

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

            EditorGUILayout.Space(5f);

            var keyProp = keyboardType switch
            {
                InputKeyboardType.InputSystem => property.FindPropertyRelative("_inputSystemKey"),
                InputKeyboardType.Legacy => property.FindPropertyRelative("_legacyInputKey"),
                _ => null,
            };
            var timingProp = property.FindPropertyRelative("_triggerTiming");
            var priorityProp = property.FindPropertyRelative("_priority");
            var keySuffix = keyboardType == InputKeyboardType.Legacy ? "KeyCode" : "Key";
            var modifierSuffix = keyboardType == InputKeyboardType.Legacy ? "ModifierKeyCodes" : "ModifierKeys";

            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            foreach (var action in Actions)
            {
                var foldoutKey = property.propertyPath + "." + action.Suffix;
                _foldouts.TryGetValue(foldoutKey, out var expanded);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    expanded = EditorGUILayout.Foldout(expanded, action.Label, true);
                    _foldouts[foldoutKey] = expanded;

                    if (!expanded) continue;

                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (keyProp != null)
                        {
                            DrawRelative(keyProp, "_" + action.Suffix + keySuffix, "Key");
                            DrawRelative(keyProp, "_" + action.Suffix + modifierSuffix, "Modifiers");
                        }

                        DrawRelative(timingProp, "_" + action.Suffix, "Trigger Timing");
                        DrawRelative(priorityProp, "_" + action.Suffix, "Priority");
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawRelative(SerializedProperty parent, string relativeName, string label)
        {
            var prop = parent?.FindPropertyRelative(relativeName);
            if (prop == null) return;
            EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
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
