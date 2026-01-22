using System;
using UnityEngine;
using YukimaruGames.Terminal.Domain.Attribute;
using YukimaruGames.Terminal.Runtime.Shared;

[Serializable]
public class Test : ITest
{
    [SerializeField] private int param1;
    [SerializeField] private Color _color;
    [SerializeField, Multiline] private string _text;
    public void Exec()
    {
        Debug.Log($"{JsonUtility.ToJson(this)}");
    }

    [TerminalCommand("echo", 0, 0)]
    private static void Echo()
    {
        Debug.Log($"echo {nameof(Test)}");
    }

    [TerminalCommand("hoge",0, 0)]
    private void Hoge()
    {
        Debug.Log($"hoge {nameof(Test)}");
    }
}
