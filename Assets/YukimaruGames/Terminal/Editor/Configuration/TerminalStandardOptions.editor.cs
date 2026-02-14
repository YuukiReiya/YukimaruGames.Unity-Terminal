using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Runtime;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(TerminalStandardOptions))]
    public sealed class TerminalStandardOptionsDrawer : PropertyDrawer
    {
        private enum Tab { Input, System } // 分割された責務に合わせる
        private Tab _tab = Tab.Input;

        // スタイル類（PropertyDrawerは静的に持つのが一般的）
        private static GUIStyle _toolbarStyle;
        private static GUIStyle _typeStyle;
        private static readonly GUIContent _visibleContent = new GUIContent("Visible");
        private static readonly GUIContent _reverseContent = new GUIContent("Reverse");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitStyles();

            // PropertyDrawerの基本：一つのRectの中で描画していく
            // ただし、複雑なレイアウトの場合はGUILayout系を使いたいので
            // BeginProperty/EndPropertyで囲みつつ、VerticalScope等を利用します
            label = EditorGUI.BeginProperty(position, label, property);
            
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 1. タブ切り替え（Toolbar）
                _tab = (Tab)GUILayout.Toolbar((int)_tab, Enum.GetNames(typeof(Tab)), _toolbarStyle);
                
                EditorGUILayout.Space(5f);

                // 2. カテゴリ別の描画
                switch (_tab)
                {
                    case Tab.Input:
                        RenderInputCategory(property);
                        break;
                    case Tab.System:
                        RenderSystemCategory(property);
                        break;
                }
            }

            EditorGUI.EndProperty();
        }

        private void RenderInputCategory(SerializedProperty property)
        {
            var keyboardTypeProp = property.FindPropertyRelative("_inputKeyboardType");
            var keyboardType = (InputKeyboardType)keyboardTypeProp.enumValueIndex;

            EditorGUILayout.LabelField("Keyboard Type", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (InputKeyboardType type in Enum.GetValues(typeof(InputKeyboardType)))
                    {
                        // リフレクション的な処理は省き、元のロジックを準用
                        bool isSelected = GUILayout.Toggle(keyboardType == type, type.ToString(), _typeStyle);
                        if (isSelected && keyboardType != type)
                        {
                            keyboardTypeProp.enumValueIndex = (int)type;
                        }
                    }
                }
            }

            EditorGUILayout.Space(5f);

            // キー設定の表示
            if (keyboardType == InputKeyboardType.InputSystem)
            {
                DrawKeyFields(property.FindPropertyRelative("_inputSystemKey"), "Input System Keys");
            }
            else if (keyboardType == InputKeyboardType.Legacy)
            {
                DrawKeyFields(property.FindPropertyRelative("_legacyInputKey"), "Legacy Keys");
            }
        }

        private void DrawKeyFields(SerializedProperty keyProp, string label)
        {
            if (keyProp == null) return;
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            
            // 子プロパティ（_openKeyなど）を全て回して表示
            var endProp = keyProp.GetEndProperty();
            keyProp.NextVisible(true); // 最初の要素へ
            while (!SerializedProperty.EqualContents(keyProp, endProp))
            {
                EditorGUILayout.PropertyField(keyProp, true);
                if (!keyProp.NextVisible(false)) break;
            }
        }

        private void RenderSystemCategory(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Buffer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_bufferSize"));

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Command", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_prompt"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("_bootupCommand"));

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("UI Controls", EditorStyles.boldLabel);
            var visibleProp = property.FindPropertyRelative("_buttonVisible");
            var reverseProp = property.FindPropertyRelative("_buttonReverse");
            visibleProp.boolValue = EditorGUILayout.ToggleLeft(_visibleContent, visibleProp.boolValue);
            reverseProp.boolValue = EditorGUILayout.ToggleLeft(_reverseContent, reverseProp.boolValue);
        }

        private void InitStyles()
        {
            if (_toolbarStyle != null) return;
            _toolbarStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 25 };
            _typeStyle = new GUIStyle(EditorStyles.radioButton) { alignment = TextAnchor.MiddleCenter };
        }

        // PropertyDrawerでGUILayoutを使う場合、この高さ計算が「0」でも
        // 自動レイアウト側が描画してくれることがありますが、
        // 本来は中身に応じた高さを返す必要があります。
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f; // GUILayoutを使う場合は0を返して隙間を詰めさせることが多いです
        }
    }
}