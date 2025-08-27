using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalWindowRenderData
    {
        public int Id { get; }
        public Rect Rect { get; }

        public TerminalWindowRenderData(int id, Rect rect)
        {
            Id = id;
            Rect = rect;
        }
    }
}
