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
        private static readonly GUIContent _loadingIndicatorContent = new GUIContent("Show Loading Indicator", "コマンド実行中、プロンプトの代わりにローディング表現を表示します.");
        private static readonly GUIContent _loadingIndicatorFramesContent = new GUIContent("Frames", "ローディング表現として順番に表示するフレーム文字列群.");

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

            using (new EditorGUI.DisabledScope(!loadingIndicatorProp.boolValue))
            {
                var loadingIndicatorFramesProp = property.FindPropertyRelative("_loadingIndicatorFrames");
                EditorGUILayout.PropertyField(loadingIndicatorFramesProp, _loadingIndicatorFramesContent, true);
            }
        }

        private void InitStyles()
        {
            if (_toolbarStyle != null) return;
            _toolbarStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 25 };
        }

        /// <summary>
        /// OnGUIの実際の描画内容（タブ・トグルの開閉状態を含む）に応じた高さを返す.
        /// </summary>
        /// <remarks>
        /// GUILayoutで描画される中身をここでも辿って積算するため、OnGUI側の描画順を変更した場合は
        /// あわせて本メソッドも更新すること.
        /// </remarks>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null) return 0f;

            const float boxPadding = 8f; // EditorStyles.helpBoxの上下パディング概算
            const float toolbarHeight = 25f;
            const float postToolbarSpace = 5f;

            var height = EditorGUIUtility.singleLineHeight; // 見出しラベル
            height += boxPadding;
            height += toolbarHeight + postToolbarSpace;
            height += _tab switch
            {
                Tab.Input => CalcInputTabHeight(property),
                Tab.System => CalcSystemTabHeight(property),
                _ => 0f,
            };

            return height;
        }

        private static float CalcInputTabHeight(SerializedProperty property)
        {
            var inputProp = property.FindPropertyRelative("_input");
            return inputProp != null ? EditorGUI.GetPropertyHeight(inputProp, true) : 0f;
        }

        private float CalcSystemTabHeight(SerializedProperty property)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            const float sectionSpace = 5f;

            var height = 0f;

            // Buffer
            height += lineHeight + spacing; // "Buffer" 見出し
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_bufferSize")) + spacing;
            height += sectionSpace;

            // Command
            height += lineHeight + spacing; // "Command" 見出し
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_prompt")) + spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_bootupCommand")) + spacing;
            height += sectionSpace;

            // UI Controls
            height += lineHeight + spacing; // "UI Controls" 見出し
            height += lineHeight + spacing; // Visible トグル
            height += lineHeight + spacing; // Reverse トグル
            height += sectionSpace;

            // Execution
            height += lineHeight + spacing; // "Execution" 見出し
            height += lineHeight + spacing; // Show Loading Indicator トグル

            var showLoadingIndicatorProp = property.FindPropertyRelative("_showLoadingIndicator");
            if (showLoadingIndicatorProp is { boolValue: true })
            {
                var framesProp = property.FindPropertyRelative("_loadingIndicatorFrames");
                if (framesProp != null)
                {
                    height += EditorGUI.GetPropertyHeight(framesProp, _loadingIndicatorFramesContent, true) + spacing;
                }
            }

            return height;
        }
    }
}