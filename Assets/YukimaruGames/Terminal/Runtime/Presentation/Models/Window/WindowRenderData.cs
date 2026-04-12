using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Models.Window
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
