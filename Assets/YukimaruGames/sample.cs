using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared;

public class sample : MonoBehaviour
{
    [SerializeReference, SerializeInterface]
    public ITest _test;

    [SerializeReference, SerializeInterface]
    public ITest _test2;
    
    [SerializeReference] public ISample _sample;

    private void Start()
    {
        _test.Exec();
    }
}

#if false
[CustomEditor(typeof(sample))]
class sampleEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("hogehogheohgeo");
    }
}
#endif