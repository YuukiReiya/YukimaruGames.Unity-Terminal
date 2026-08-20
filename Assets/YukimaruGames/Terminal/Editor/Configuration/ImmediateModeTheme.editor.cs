using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// <see cref="ImmediateModeTheme"/>のInspector表示用PropertyDrawer.
    /// </summary>
    /// <remarks>
    /// 描画は<see cref="DrawerLayout"/>による矩形ベース。<c>EditorGUILayout</c>を使うと
    /// Drawerが確保した位置ではなくInspectorの末尾へ流れてしまう(#147).
    /// </remarks>
    [CustomPropertyDrawer(typeof(ImmediateModeTheme))]
    public sealed class ImmediateModeThemeDrawer : PropertyDrawer
    {
        private const string ZeroReferenceHeightMessage =
            "Reference Resolution の高さが0のため、拡縮は行われません。";

        private const float SectionSpace = 6f;
        private const float GroupInnerSpace = 4f;
        private const float ResetButtonWidth = 60f;
        private const string ResetButtonText = "RESET";

        /// <summary>キャレットの点滅速度として指定できる範囲.</summary>
        private const float CursorFlashSpeedMin = 0f;
        private const float CursorFlashSpeedMax = 3f;

        /// <summary>RESETボタンで戻す点滅速度(<see cref="ImmediateModeTheme"/>の既定値).</summary>
        private const float DefaultCursorFlashSpeed = 1.886792f;

        private static readonly GUIContent SizeLabel = new("Size");
        private static readonly GUIContent ScaleWithScreenLabel = new("Scale With Screen");
        private static readonly GUIContent ReferenceResolutionLabel = new("Reference Resolution");
        private static readonly GUIContent CursorFlashSpeedLabel = new("Cursor Flash Speed");

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
                BuildFont(box, property);

                box.Space(SectionSpace);
                BuildColors(box, property);

                box.Space(SectionSpace);
                BuildLogColors(box, property);

                box.Space(SectionSpace);
                BuildButtonColors(box, property);
            });
        }

        private static void BuildFont(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Font", EditorStyles.boldLabel);
            layout.BoxedGroup(box =>
            {
                box.PropertyField(property.FindPropertyRelative("_font"));

                var fontSize = property.FindPropertyRelative("_fontSize");
                box.PropertyField(fontSize, SizeLabel);

                var scaleWithScreen = property.FindPropertyRelative("_scaleFontWithScreen");
                box.PropertyField(scaleWithScreen, ScaleWithScreenLabel);

                if (!scaleWithScreen.boolValue) return;

                var reference = property.FindPropertyRelative("_referenceResolution");
                box.PropertyField(reference, ReferenceResolutionLabel);
                BuildEffectiveFontSize(box, fontSize.intValue, reference.vector2IntValue.y);
            });
        }

        /// <summary>
        /// 拡縮を有効にしたとき、現在のGame Viewで実際に描画されるサイズを併記する.
        /// </summary>
        /// <remarks>
        /// 拡縮が有効な間、Sizeの値は「基準解像度での大きさ」であって実際の描画サイズではない。
        /// 入力した値と見えている大きさが食い違って見えるため、実効値をその場に出す.
        /// </remarks>
        private static void BuildEffectiveFontSize(DrawerLayout layout, int fontSize, int referenceHeight)
        {
            if (referenceHeight <= 0)
            {
                layout.HelpBox(ZeroReferenceHeightMessage, MessageType.Warning);
                return;
            }

            PlayModeWindow.GetRenderingResolution(out var width, out var height);
            if (height == 0) return;

            // 実行時とまったく同じ計算を使う。ここで計算を再実装すると、
            // 一方だけ変更されたときにInspectorの表示と実際の描画サイズがずれる.
            var effective = ThemeBinder.ResolveFontSize(
                fontSize, scaleFontWithScreen: true, referenceHeight, (int)height);

            layout.Label($"実効 {effective}px（Game View {width}x{height}）", EditorStyles.miniLabel);
        }

        private static void BuildColors(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Colors", EditorStyles.boldLabel);

            layout.PropertyField(property.FindPropertyRelative("_backgroundColor"));
            layout.PropertyField(property.FindPropertyRelative("_promptColor"));
            layout.PropertyField(property.FindPropertyRelative("_inputColor"));
            layout.PropertyField(property.FindPropertyRelative("_caretColor"));
            layout.PropertyField(property.FindPropertyRelative("_selectionColor"));

            var flashSpeed = property.FindPropertyRelative("_cursorFlashSpeed");
            if (layout.SliderWithButton(
                    flashSpeed, CursorFlashSpeedMin, CursorFlashSpeedMax,
                    CursorFlashSpeedLabel, ResetButtonText, ResetButtonWidth))
            {
                flashSpeed.floatValue = DefaultCursorFlashSpeed;
            }
        }

        private static void BuildLogColors(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Log Colors", EditorStyles.miniBoldLabel);

            layout.PropertyField(property.FindPropertyRelative("_messageColor"));
            layout.PropertyField(property.FindPropertyRelative("_entryColor"));
            layout.PropertyField(property.FindPropertyRelative("_warningColor"));
            layout.PropertyField(property.FindPropertyRelative("_errorColor"));
            layout.PropertyField(property.FindPropertyRelative("_assertColor"));
            layout.PropertyField(property.FindPropertyRelative("_exceptionColor"));
            layout.PropertyField(property.FindPropertyRelative("_systemColor"));
        }

        private static void BuildButtonColors(DrawerLayout layout, SerializedProperty property)
        {
            layout.Label("Buttons", EditorStyles.miniBoldLabel);

            layout.PropertyField(property.FindPropertyRelative("_executeButtonColor"));
            layout.PropertyField(property.FindPropertyRelative("_buttonColor"));
            layout.PropertyField(property.FindPropertyRelative("_copyButtonColor"));
        }
    }
}
