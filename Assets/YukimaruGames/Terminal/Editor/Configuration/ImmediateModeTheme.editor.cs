using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// <see cref="ImmediateModeTheme"/>のInspector表示用PropertyDrawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(ImmediateModeTheme))]
    public sealed class ImmediateModeThemeDrawer : PropertyDrawer
    {
        private const string ZeroReferenceHeightMessage =
            "Reference Resolution の高さが0のため、拡縮は行われません。";

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

        /// <summary>
        /// 拡縮を有効にしたとき、現在のGame Viewで実際に描画されるサイズを併記する.
        /// </summary>
        /// <remarks>
        /// 拡縮が有効な間、Sizeの値は「基準解像度での大きさ」であって実際の描画サイズではない。
        /// 入力した値と見えている大きさが食い違って見えるため、実効値をその場に出す.
        /// </remarks>
        private static void RenderEffectiveFontSize(int fontSize, int referenceHeight)
        {
            if (referenceHeight <= 0)
            {
                EditorGUILayout.HelpBox(ZeroReferenceHeightMessage, MessageType.Warning);
                return;
            }

            PlayModeWindow.GetRenderingResolution(out var width, out var height);
            if (height == 0) return;

            // 実行時とまったく同じ計算を使う。ここで計算を再実装すると、
            // 一方だけ変更されたときにInspectorの表示と実際の描画サイズがずれる.
            var effective = ThemeBinder.ResolveFontSize(
                fontSize, scaleFontWithScreen: true, referenceHeight, (int)height);

            EditorGUILayout.LabelField(
                " ",
                $"実効 {effective}px（Game View {width}x{height}）",
                EditorStyles.miniLabel);
        }

        private void RenderViewCategory(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Font", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("_font"));

                var fontSize = property.FindPropertyRelative("_fontSize");
                EditorGUILayout.PropertyField(fontSize, new GUIContent("Size"));

                var scaleWithScreen = property.FindPropertyRelative("_scaleFontWithScreen");
                EditorGUILayout.PropertyField(scaleWithScreen, new GUIContent("Scale With Screen"));

                if (scaleWithScreen.boolValue)
                {
                    var reference = property.FindPropertyRelative("_referenceResolution");
                    EditorGUILayout.PropertyField(reference, new GUIContent("Reference Resolution"));
                    RenderEffectiveFontSize(fontSize.intValue, reference.vector2IntValue.y);
                }
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