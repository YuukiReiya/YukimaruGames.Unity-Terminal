#if UNITY_EDITOR
using UnityEditor;
using YukimaruGames.Terminal.Runtime;

namespace YukimaruGames.Terminal.Editor
{
    [CustomEditor(typeof(TerminalBootstrapper))]
    public sealed class TerminalBootstrapperEditor : UnityEditor.Editor
    {
        private SerializedProperty _installerProp;

        private void OnEnable()
        {
            _installerProp = serializedObject.FindProperty("_installer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            //using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_installerProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif