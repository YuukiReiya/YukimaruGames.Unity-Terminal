using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Runtime;
using YukimaruGames.Terminal.UI.View.Model;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(TerminalStandardTheme))]
    public sealed class TerminalStandardThemeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;
            
            label = EditorGUI.BeginProperty(position, label, property);
            
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(4f);
                RenderViewCategory(property);
                EditorGUILayout.Space(4f);
            }

            EditorGUI.EndProperty();
        }

        private void RenderViewCategory(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Font", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("_font"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("_fontSize"), new GUIContent("Size"));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            
            // 背景・プロンプト・入力
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_backgroundColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_promptColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_inputColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_caretColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_selectionColor"));

            // カーソル速度（リセットボタン付き）
            using (new EditorGUILayout.HorizontalScope())
            {
                var flashSpeedProp = property.FindPropertyRelative("_cursorFlashSpeed");
                EditorGUILayout.Slider(flashSpeedProp, 0f, 3f, new GUIContent("Cursor Flash Speed"));
                if (GUILayout.Button("RESET", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    flashSpeedProp.floatValue = 1.886792f;
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Log Colors", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_messageColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_entryColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_warningColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_errorColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_assertColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_exceptionColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_systemColor"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Buttons", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_executeButtonColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_buttonColor"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_copyButtonColor"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f; 
        }
    }
}