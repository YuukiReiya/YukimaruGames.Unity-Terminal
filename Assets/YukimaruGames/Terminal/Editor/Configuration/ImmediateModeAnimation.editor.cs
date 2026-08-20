using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// <see cref="ImmediateModeAnimation"/>のInspector表示用PropertyDrawer.
    /// </summary>
    /// <remarks>
    /// 描画は<see cref="DrawerLayout"/>による矩形ベース。<c>EditorGUILayout</c>を使うと
    /// Drawerが確保した位置ではなくInspectorの末尾へ流れてしまう(#147).
    /// </remarks>
    [CustomPropertyDrawer(typeof(ImmediateModeAnimation))]
    public sealed class ImmediateModeAnimationDrawer : PropertyDrawer
    {
        private const float SectionSpace = 6f;
        private const float GroupInnerSpace = 2f;

        /// <summary>開閉アニメーションの尺(秒)として指定できる範囲.</summary>
        private const float DurationMin = 0f;
        private const float DurationMax = 3f;

        /// <summary>Compactスタイル時のウィンドウ比率として指定できる範囲.</summary>
        private const float CompactScaleMin = 0.1f;
        private const float CompactScaleMax = 1f;

        private static readonly Lazy<GUIStyle> PopupStyle = new(() => new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        });

        private static readonly GUIContent[] BootupStateOptions = ToOptions(typeof(WindowState));
        private static readonly GUIContent[] AnchorOptions = ToOptions(typeof(WindowAnchor));
        private static readonly GUIContent[] WindowStyleOptions = ToOptions(typeof(WindowStyle));

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return;

            label = EditorGUI.BeginProperty(position, label, property);

            Build(new DrawerLayout(position, true), property, label);

            EditorGUI.EndProperty();
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject.targetObject == null) return 0f;

            var layout = new DrawerLayout(new Rect(0f, 0f, EditorGUIUtility.currentViewWidth, 0f), false);
            Build(layout, property, label);

            return layout.Height;
        }

        /// <summary>
        /// 描画と高さ計算で共有する組み立て処理.
        /// </summary>
        private static void Build(DrawerLayout layout, SerializedProperty property, GUIContent label)
        {
            layout.Label(label, EditorStyles.boldLabel);
            layout.BoxedGroup(box =>
            {
                box.Space(GroupInnerSpace);
                BuildWindowStyle(box, property);

                box.Space(SectionSpace);
                BuildParameters(box, property);
            });
        }

        private static void BuildWindowStyle(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Window Style", EditorStyles.boldLabel);
            layout.BoxedGroup(box =>
            {
                box.EnumPopup(
                    property.FindPropertyRelative("_bootupWindowState"),
                    new GUIContent("Bootup State"), BootupStateOptions, PopupStyle.Value);
                box.EnumPopup(
                    property.FindPropertyRelative("_anchor"),
                    new GUIContent("Anchor"), AnchorOptions, PopupStyle.Value);
                box.EnumPopup(
                    property.FindPropertyRelative("_windowStyle"),
                    new GUIContent("Style"), WindowStyleOptions, PopupStyle.Value);
            });
        }

        private static void BuildParameters(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Parameters", EditorStyles.boldLabel);
            layout.BoxedGroup(box =>
            {
                box.Slider(property.FindPropertyRelative("_duration"), DurationMin, DurationMax);
                box.Slider(property.FindPropertyRelative("_compactScale"), CompactScaleMin, CompactScaleMax);
            });
        }

        private static GUIContent[] ToOptions(Type enumType) =>
            Array.ConvertAll(Enum.GetNames(enumType), name => new GUIContent(name));
    }
}
