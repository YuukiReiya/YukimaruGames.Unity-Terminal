using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// <see cref="ImmediateModeOptions"/>のInspector表示用PropertyDrawer.
    /// </summary>
    /// <remarks>
    /// 描画は<see cref="DrawerLayout"/>による矩形ベース。<c>EditorGUILayout</c>を使うと
    /// Drawerが確保した位置ではなくInspectorの末尾へ流れてしまう(#147).
    /// </remarks>
    [CustomPropertyDrawer(typeof(ImmediateModeOptions))]
    public sealed class ImmediateModeOptionsDrawer : PropertyDrawer
    {
        private enum Tab { Input, System } // 分割された責務に合わせる

        private const float ToolbarHeight = 25f;
        private const float PostToolbarSpace = 5f;
        private const float SectionSpace = 5f;

        private static readonly string[] TabNames = Enum.GetNames(typeof(Tab));
        private static readonly GUIContent VisibleContent = new("Visible");
        private static readonly GUIContent ReverseContent = new("Reverse");
        private static readonly GUIContent InputContent = new("Input");
        private static readonly GUIContent LoadingIndicatorContent =
            new("Show Loading Indicator", "コマンド実行中、プロンプトの代わりにローディング表現を表示します.");
        private static readonly GUIContent LoadingIndicatorFramesContent =
            new("Frames", "ローディング表現として順番に表示するフレーム文字列群.");

        private static GUIStyle _toolbarStyle;

        private Tab _tab = Tab.Input;

        /// <summary>直前の描画で受け取った幅(px). 高さの計算に使う.</summary>
        private float _lastWidth;

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;

            InitStyles();

            _lastWidth = position.width;

            label = EditorGUI.BeginProperty(position, label, property);

            Build(new DrawerLayout(position, true), property, label);

            EditorGUI.EndProperty();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 描画と同じ組み立て処理を「計算のみ」で通すため、タブやトグルの開閉状態による
        /// 高さの違いも自動的に反映される(以前は高さの積算を別に持っていた).
        /// </remarks>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return 0f;

            // InitStylesはGUI.skinへ触れるため計測時には呼ばない(ツールバーの高さは定数で持つ).
            var layout = new DrawerLayout(DrawerLayout.MeasureRect(_lastWidth), false);
            Build(layout, property, label);

            return layout.Height;
        }

        /// <summary>
        /// 描画と高さ計算で共有する組み立て処理.
        /// </summary>
        private void Build(DrawerLayout layout, SerializedProperty property, GUIContent label)
        {
            layout.Label(label, EditorStyles.boldLabel);
            layout.BoxedGroup(box =>
            {
                var selected = (Tab)box.Toolbar((int)_tab, TabNames, _toolbarStyle, ToolbarHeight);
                if (selected != _tab)
                {
                    _tab = selected;

                    // 確保済みの高さは切り替え前のタブで計算されている。この描画を打ち切り、
                    // 次のLayoutイベントで測り直させる(でないと1フレームだけ内容が矩形からはみ出す).
                    GUIUtility.ExitGUI();
                }

                box.Space(PostToolbarSpace);

                switch (_tab)
                {
                    case Tab.Input:
                        BuildInputCategory(box, property);
                        break;
                    case Tab.System:
                        BuildSystemCategory(box, property);
                        break;
                }
            });
        }

        /// <summary>
        /// アクション別のキー設定は<see cref="ImmediateModeInputDrawer"/>へ委譲する.
        /// </summary>
        /// <remarks>
        /// Key/Modifiers/Trigger Timing/Priorityのグルーピング表示は、
        /// <c>_input</c>フィールドの<c>[SerializeInterface]</c>経由で自動的に使われる.
        /// </remarks>
        private static void BuildInputCategory(DrawerLayout layout, SerializedProperty property)
        {
            var inputProp = property.FindPropertyRelative("_input");
            if (inputProp == null) return;

            layout.PropertyField(inputProp, InputContent);
        }

        private static void BuildSystemCategory(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Buffer", EditorStyles.boldLabel);
            layout.PropertyField(property.FindPropertyRelative("_bufferSize"));

            layout.Space(SectionSpace);

            layout.Label("Command", EditorStyles.boldLabel);
            layout.PropertyField(property.FindPropertyRelative("_prompt"));
            layout.PropertyField(property.FindPropertyRelative("_bootupCommand"));

            layout.Space(SectionSpace);

            layout.Label("UI Controls", EditorStyles.boldLabel);
            layout.ToggleLeft(property.FindPropertyRelative("_buttonVisible"), VisibleContent);
            layout.ToggleLeft(property.FindPropertyRelative("_buttonReverse"), ReverseContent);

            layout.Space(SectionSpace);

            layout.Label("Execution", EditorStyles.boldLabel);

            var loadingIndicatorProp = property.FindPropertyRelative("_showLoadingIndicator");
            var showLoadingIndicator = loadingIndicatorProp is { boolValue: true };
            layout.ToggleLeft(loadingIndicatorProp, LoadingIndicatorContent);

            // ONにするとフレーム設定の行が増える(=高さが変わる)ため、変わったらこの描画は打ち切る.
            if (loadingIndicatorProp is { boolValue: var current } && current != showLoadingIndicator)
            {
                GUIUtility.ExitGUI();
            }

            if (loadingIndicatorProp is not { boolValue: true }) return;

            layout.PropertyField(
                property.FindPropertyRelative("_loadingIndicatorFrames"), LoadingIndicatorFramesContent);
        }

        private static void InitStyles()
        {
            _toolbarStyle ??= new GUIStyle(GUI.skin.button) { fixedHeight = ToolbarHeight };
        }
    }
}
