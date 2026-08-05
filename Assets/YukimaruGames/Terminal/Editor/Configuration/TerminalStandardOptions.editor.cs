using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(TerminalStandardOptions))]
    public sealed class TerminalStandardOptionsDrawer : PropertyDrawer
    {
        private enum Tab { Input, System } // 分割された責務に合わせる
        private Tab _tab = Tab.Input;

        // スタイル類（PropertyDrawerは静的に持つのが一般的）
        private static GUIStyle _toolbarStyle;
        private static readonly GUIContent _visibleContent = new GUIContent("Visible");
        private static readonly GUIContent _reverseContent = new GUIContent("Reverse");
        private static readonly GUIContent _loadingIndicatorContent = new GUIContent("Show Loading Indicator", "コマンド実行中にプロンプト横へスピナーを表示します.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;
            
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
            // Key/Modifiers/Trigger Timing/Priorityのアクション別グルーピング表示は
            // TerminalStandardInputDrawer(_inputフィールドの[SerializeInterface]経由で自動的に使用される)に委譲する.
            var inputProp = property.FindPropertyRelative("_input");
            if (inputProp == null) return;
            EditorGUILayout.PropertyField(inputProp, new GUIContent("Input"), true);
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

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Execution", EditorStyles.boldLabel);
            var loadingIndicatorProp = property.FindPropertyRelative("_showLoadingIndicator");
            loadingIndicatorProp.boolValue = EditorGUILayout.ToggleLeft(_loadingIndicatorContent, loadingIndicatorProp.boolValue);
        }

        private void InitStyles()
        {
            if (_toolbarStyle != null) return;
            _toolbarStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 25 };
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