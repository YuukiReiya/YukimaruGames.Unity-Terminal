using YukimaruGames.Terminal.Presentation.Models.Logs;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface ILogRenderer
    {
        void Render(LogRenderData data);
    }
}
