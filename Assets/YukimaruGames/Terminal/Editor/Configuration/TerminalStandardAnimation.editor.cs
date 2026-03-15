using System;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Runtime;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(TerminalStandardAnimation))]
    public sealed class TerminalStandardAnimationDrawer : PropertyDrawer
    {
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
                EditorGUILayout.Space(4f);
                RenderWindowStyle(property);

                EditorGUILayout.Space(6f);
                RenderParameters(property);
                EditorGUILayout.Space(4f);
            }

            EditorGUI.EndProperty();
        }

        private void RenderWindowStyle(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Window Style", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(2f);
                DrawPopup(property.FindPropertyRelative("_bootupWindowState"), typeof(WindowState), "Bootup State");
                DrawPopup(property.FindPropertyRelative("_anchor"), typeof(WindowAnchor), "Anchor");
                DrawPopup(property.FindPropertyRelative("_windowStyle"), typeof(WindowStyle), "Style");
                EditorGUILayout.Space(2f);
            }
        }

        private void RenderParameters(SerializedProperty property)
        {
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
            return 0f;
        }
    }
}
