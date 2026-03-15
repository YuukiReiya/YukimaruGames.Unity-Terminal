using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public readonly struct WindowRenderData
    {
        public int Id { get; }
        public Rect Rect { get; }

        public WindowRenderData(int id, Rect rect)
        {
            Id = id;
            Rect = rect;
        }
    }
}
