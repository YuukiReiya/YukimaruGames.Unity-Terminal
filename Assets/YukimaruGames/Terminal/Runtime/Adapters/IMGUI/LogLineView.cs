using UnityEngine;

namespace YukimaruGames.Terminal.Adapters.IMGUI
{
    /// <summary>
    /// ログ1行分の描画状態を保持するView.
    /// <para>
    /// <see cref="GUIContent"/>を使い回すことで、<c>GUILayout.Label(string, ...)</c>が
    /// 呼び出しの都度内部で生成する一時<see cref="GUIContent"/>のGC Allocを回避する。
    /// </para>
    /// </summary>
    public sealed class LogLineView
    {
        private readonly GUIContent _content = new();

        /// <summary>表示するメッセージを設定する.</summary>
        public void SetMessage(string message)
        {
            _content.text = message;
        }

        /// <summary>保持しているメッセージを描画する.</summary>
        public void Render(GUIStyle style)
        {
            UnityEngine.GUILayout.Label(_content, style);
        }

        /// <summary>プールへ返却する前に内部状態をクリアする.</summary>
        public void Reset()
        {
            _content.text = string.Empty;
        }
    }
}
