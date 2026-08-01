using YukimaruGames.Terminal.Presentation.Models.Input;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IInputRenderer
    {
        /// <summary>
        /// 描画.
        /// </summary>
        void Render(InputRenderData data);
    }
}
