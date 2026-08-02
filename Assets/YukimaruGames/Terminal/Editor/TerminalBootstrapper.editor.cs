#if UNITY_EDITOR
using UnityEditor;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Editor
{
    [CustomEditor(typeof(TerminalBootstrapper))]
    public sealed class BootstrapperEditor : UnityEditor.Editor
    {
        private SerializedProperty _installerProp;

        private void OnEnable()
        {
            _installerProp = serializedObject.FindProperty("_installer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_installerProp);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif