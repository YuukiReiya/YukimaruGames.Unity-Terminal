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
        private enum Tab { View, Animation }
        private Tab _tab = Tab.View;

        private static GUIStyle _toolbarStyle;
        private static readonly Lazy<GUIStyle> _popupStyle = new(() => new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        });

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;
            
            InitStyles();

            label = EditorGUI.BeginProperty(position, label, property);
            
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 1. タブ切り替え
                _tab = (Tab)GUILayout.Toolbar((int)_tab, Enum.GetNames(typeof(Tab)), _toolbarStyle);
                EditorGUILayout.Space(6f);

                // 2. カテゴリ別描画
                switch (_tab)
                {
                    case Tab.View:
                        RenderViewCategory(property);
                        break;
                    case Tab.Animation:
                        RenderAnimationCategory(property);
                        break;
                }
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

        private void RenderAnimationCategory(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Window Style", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(2f);
                DrawPopup(property.FindPropertyRelative("_bootupWindowState"), typeof(TerminalState), "Bootup State");
                DrawPopup(property.FindPropertyRelative("_anchor"), typeof(TerminalAnchor), "Anchor");
                DrawPopup(property.FindPropertyRelative("_windowStyle"), typeof(TerminalWindowStyle), "Style");
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Slider(property.FindPropertyRelative("_duration"), 0f, 3f);
                EditorGUILayout.Slider(property.FindPropertyRelative("_compactScale"), 0.1f, 1f);
            }
        }

        private void DrawPopup(SerializedProperty prop, Type enumType, string label)
        {
            prop.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent(label),
                prop.enumValueIndex,
                Array.ConvertAll(Enum.GetNames(enumType), s => new GUIContent(s)),
                _popupStyle.Value);
        }

        private void InitStyles()
        {
            if (_toolbarStyle != null) return;
            _toolbarStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                fontStyle = FontStyle.Bold
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // GUILayoutを使用しているため、自動レイアウトに任せる。
            // ただし、Assertion対策として最小限の1行分は返しておく。
            return 0f; 
        }
    }
}